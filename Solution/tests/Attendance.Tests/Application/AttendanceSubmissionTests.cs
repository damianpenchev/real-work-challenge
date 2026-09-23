using Attendance.Application.Abstractions;
using Attendance.Application.Common;
using Attendance.Application.Records;
using Attendance.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Attendance.Tests.Application;

public class AttendanceSubmissionTests
{
    private static readonly DateOnly Day = new(2025, 3, 4);

    [Fact]
    public async Task EveryErrorComesBackAtOnce_KeyedToItsRow()
    {
        using var h = new Harness();
        var school = await h.AddSchool();
        await h.AddCodes();
        var able = await h.AddStudent(school, "Able");
        var baker = await h.AddStudent(school, "Baker");

        var result = await h.Submit(new SubmitDailyAttendanceRequest(school, Day, new[]
        {
            new AttendanceEntry(able, "ZZZ", 0, null),
            new AttendanceEntry(999999, "P", 0, null),
            new AttendanceEntry(baker, "P", -5, null)
        }));

        Assert.False(result.IsSuccess);
        Assert.Equal(3, result.Errors.Count);
        Assert.Equal(new int?[] { 0, 1, 2 }, result.Errors.Select(e => e.Row).OrderBy(r => r).ToArray());
        Assert.Contains(result.Errors, e => e.Code == ErrorCodes.UnknownCode);
        Assert.Contains(result.Errors, e => e.Code == ErrorCodes.StudentNotInSchool);
        Assert.Contains(result.Errors, e => e.Code == ErrorCodes.MinutesLateOutOfRange);
    }

    [Fact]
    public async Task ResubmittingADay_AmendsInPlaceAndRecordsWhoChangedIt()
    {
        using var h = new Harness();
        var school = await h.AddSchool();
        await h.AddCodes();
        var student = await h.AddStudent(school);

        h.User.Name = "clerk";
        await h.Submit(new SubmitDailyAttendanceRequest(
            school, Day, new[] { new AttendanceEntry(student, "P", 0, null) }));

        h.User.Name = "principal";
        h.Clock.UtcNow = h.Clock.UtcNow.AddHours(3);
        var amended = await h.Submit(new SubmitDailyAttendanceRequest(
            school, Day, new[] { new AttendanceEntry(student, "AE", 20, "dentist") }));

        Assert.True(amended.IsSuccess);
        Assert.Equal(1, amended.Value!.Amended);
        var row = Assert.Single(await h.InContext(db => db.AttendanceRecords.AsNoTracking().ToListAsync()));
        Assert.Equal("AE", row.Code);
        Assert.True(row.IsExcused);
        Assert.Equal("clerk", row.CreatedBy);
        Assert.Equal("principal", row.ModifiedBy);
        Assert.NotNull(row.ModifiedAt);
    }

    [Fact]
    public async Task TheSameStudentTwiceInOneBatch_IsRejected()
    {
        using var h = new Harness();
        var school = await h.AddSchool();
        await h.AddCodes();
        var student = await h.AddStudent(school);

        var result = await h.Submit(new SubmitDailyAttendanceRequest(school, Day, new[]
        {
            new AttendanceEntry(student, "P", 0, null),
            new AttendanceEntry(student, "A", 0, null)
        }));

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Code == ErrorCodes.DuplicateStudent);
    }

    [Fact]
    public async Task AFutureDateIsRejectedUnlessThePolicyAllowsIt()
    {
        using var h = new Harness();
        var school = await h.AddSchool();
        await h.AddCodes();
        var student = await h.AddStudent(school);
        var tomorrow = h.Clock.Today.AddDays(1);

        var rejected = await h.Submit(new SubmitDailyAttendanceRequest(
            school, tomorrow, new[] { new AttendanceEntry(student, "P", 0, null) }));
        Assert.False(rejected.IsSuccess);
        Assert.Equal(ErrorCodes.FutureDate, Assert.Single(rejected.Errors).Code);

        using var permissive = new Harness(o => o.AllowFutureDates = true);
        var school2 = await permissive.AddSchool();
        await permissive.AddCodes();
        var student2 = await permissive.AddStudent(school2);
        var accepted = await permissive.Submit(new SubmitDailyAttendanceRequest(
            school2, permissive.Clock.Today.AddDays(1), new[] { new AttendanceEntry(student2, "P", 0, null) }));
        Assert.True(accepted.IsSuccess);
    }

    [Fact]
    public async Task AnInactiveStudentOrOneFromAnotherSchoolIsRejected()
    {
        using var h = new Harness();
        var school = await h.AddSchool();
        var other = await h.AddSchool();
        await h.AddCodes();
        var withdrawn = await h.AddStudent(school, "Gone", active: false);
        var elsewhere = await h.AddStudent(other, "Elsewhere");

        var result = await h.Submit(new SubmitDailyAttendanceRequest(school, Day, new[]
        {
            new AttendanceEntry(withdrawn, "P", 0, null),
            new AttendanceEntry(elsewhere, "P", 0, null)
        }));

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Code == ErrorCodes.StudentInactive && e.Row == 0);
        Assert.Contains(result.Errors, e => e.Code == ErrorCodes.StudentNotInSchool && e.Row == 1);
    }

    [Fact]
    public async Task TheUniqueIndexOnStudentAndDateSurfacesAsADomainException()
    {
        using var h = new Harness();
        var school = await h.AddSchool();
        await h.AddCodes();
        var student = await h.AddStudent(school);
        await h.AddRecord(student, school, Day, "P");

        await Assert.ThrowsAsync<UniqueInvariantViolationException>(() => h.InScope(async sp =>
        {
            var codes = await sp.GetRequiredService<IAttendanceCodeRepository>().GetAllAsync(default);
            sp.GetRequiredService<IAttendanceRepository>().Add(AttendanceRecord.Record(
                student, school, Day, codes.First(c => c.Value == "A"), 0, null, h.Clock.UtcNow, "racer"));
            return await sp.GetRequiredService<IUnitOfWork>().SaveChangesAsync(default);
        }));
    }

    [Fact]
    public async Task OnlyOneOpenAlertPerStudentPerSchoolYearCanExist()
    {
        using var h = new Harness();
        var school = await h.AddSchool();
        var student = await h.AddStudent(school);
        var year = SchoolYear.StartingIn(2024);

        await h.InContext(async db =>
        {
            var resolved = StudentAlert.RaiseChronicAbsence(student, school, year, 10, h.Clock.UtcNow);
            resolved.Resolve(h.Clock.UtcNow, "counsellor");
            db.Add(resolved);
            db.Add(StudentAlert.RaiseChronicAbsence(student, school, year, 10, h.Clock.UtcNow));
            return await db.SaveChangesAsync();
        });

        await Assert.ThrowsAsync<UniqueInvariantViolationException>(() => h.InScope(async sp =>
        {
            sp.GetRequiredService<IStudentAlertRepository>()
                .Add(StudentAlert.RaiseChronicAbsence(student, school, year, 11, h.Clock.UtcNow));
            return await sp.GetRequiredService<IUnitOfWork>().SaveChangesAsync(default);
        }));
    }

    [Fact]
    public async Task ChronicStatusUsesTheSchoolThresholdAndRaisesTheAlertOnce()
    {
        using var h = new Harness();
        var school = await h.AddSchool(threshold: 2);
        await h.AddCodes();
        var student = await h.AddStudent(school);
        await h.AddRecord(student, school, new DateOnly(2025, 1, 13), "A");

        await h.Submit(new SubmitDailyAttendanceRequest(
            school, Day, new[] { new AttendanceEntry(student, "A", 0, null) }));
        await h.Submit(new SubmitDailyAttendanceRequest(
            school, Day, new[] { new AttendanceEntry(student, "A", 0, null) }));

        var history = await h.History(student);
        Assert.Equal(2, history.Value!.Threshold);
        Assert.Equal(2, history.Value.TotalAbsences);
        Assert.True(history.Value.IsChronicallyAbsent);
        Assert.Single(await h.InContext(db => db.StudentAlerts.AsNoTracking().ToListAsync()));
    }

    [Fact]
    public async Task TotalsAreDerivedFromRecordsSoACorrectionMovesTheTallyDown()
    {
        using var h = new Harness();
        var school = await h.AddSchool(threshold: 2);
        await h.AddCodes();
        var student = await h.AddStudent(school);
        await h.AddRecord(student, school, new DateOnly(2025, 1, 13), "A");

        await h.Submit(new SubmitDailyAttendanceRequest(
            school, Day, new[] { new AttendanceEntry(student, "A", 0, null) }));
        Assert.True((await h.History(student)).Value!.IsChronicallyAbsent);

        await h.Submit(new SubmitDailyAttendanceRequest(
            school, Day, new[] { new AttendanceEntry(student, "P", 0, "keyed in error") }));

        var corrected = await h.History(student);
        Assert.Equal(1, corrected.Value!.TotalAbsences);
        Assert.False(corrected.Value.IsChronicallyAbsent);
    }

    [Fact]
    public async Task HistoryKeepsRowsWhoseCodeWasLaterRetired()
    {
        using var h = new Harness();
        var school = await h.AddSchool();
        await h.AddCodes();
        var student = await h.AddStudent(school);
        await h.AddRecord(student, school, new DateOnly(2025, 2, 3), "OLD");

        var history = await h.History(student);

        var item = Assert.Single(history.Value!.Items);
        Assert.Equal("OLD", item.Code);
        Assert.Equal("Retired code", item.CodeDescription);
    }
}
