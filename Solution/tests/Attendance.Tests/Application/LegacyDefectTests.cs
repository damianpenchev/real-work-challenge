using Attendance.Application.Common;
using Attendance.Application.Records;
using Microsoft.EntityFrameworkCore;

namespace Attendance.Tests.Application;

public class LegacyDefectTests
{
    private static readonly DateOnly Day = new(2025, 3, 4);

    [Fact]
    public async Task StudentWithNoRecord_DoesNotInheritThePreviousStudentsRecord_StaleExistingIdDefect()
    {
        using var h = new Harness();
        var school = await h.AddSchool();
        await h.AddCodes();
        var able = await h.AddStudent(school, "Able");
        var baker = await h.AddStudent(school, "Baker");
        await h.AddRecord(able, school, Day, "P");

        var result = await h.Submit(new SubmitDailyAttendanceRequest(school, Day, new[]
        {
            new AttendanceEntry(able, "A", 0, null),
            new AttendanceEntry(baker, "P", 0, null)
        }));

        Assert.True(result.IsSuccess);
        var rows = await h.InContext(db => db.AttendanceRecords.AsNoTracking().ToListAsync());
        Assert.Equal(2, rows.Count);
        var ableRow = Assert.Single(rows, r => r.StudentId == able);
        var bakerRow = Assert.Single(rows, r => r.StudentId == baker);
        Assert.Equal("A", ableRow.Code);
        Assert.True(ableRow.IsAbsent);
        Assert.Equal("P", bakerRow.Code);
        Assert.False(bakerRow.IsAbsent);
        Assert.NotEqual(ableRow.Id, bakerRow.Id);
    }

    [Fact]
    public async Task AbsenceTallyIsBoundedToOneSchoolYear_BrokenSchoolYearPredicateDefect()
    {
        using var h = new Harness();
        var school = await h.AddSchool();
        await h.AddCodes();
        var student = await h.AddStudent(school);
        await h.AddRecord(student, school, new DateOnly(2024, 5, 6), "A");   // 2023-2024
        await h.AddRecord(student, school, new DateOnly(2024, 9, 2), "A");   // 2024-2025
        await h.AddRecord(student, school, new DateOnly(2025, 1, 10), "A");  // 2024-2025

        var result = await h.Submit(new SubmitDailyAttendanceRequest(
            school, Day, new[] { new AttendanceEntry(student, "A", 0, null) }));
        Assert.True(result.IsSuccess);

        var history = await h.History(student);

        Assert.True(history.IsSuccess);
        Assert.Equal("2024-2025", history.Value!.SchoolYear);
        Assert.Equal(3, history.Value.TotalAbsences);
        Assert.Equal(3, history.Value.Items.Count);
    }

    [Fact]
    public async Task UnknownOrRetiredCode_RefusesTheWholeSubmission_StaleCodeLookupDefect()
    {
        using var h = new Harness();
        var school = await h.AddSchool();
        await h.AddCodes();
        var able = await h.AddStudent(school, "Able");
        var baker = await h.AddStudent(school, "Baker");

        var unknown = await h.Submit(new SubmitDailyAttendanceRequest(school, Day, new[]
        {
            new AttendanceEntry(able, "A", 0, null),
            new AttendanceEntry(baker, "ZZZ", 0, null)
        }));

        Assert.False(unknown.IsSuccess);
        var error = Assert.Single(unknown.Errors);
        Assert.Equal(ErrorCodes.UnknownCode, error.Code);
        Assert.Equal(1, error.Row);
        Assert.Empty(await h.InContext(db => db.AttendanceRecords.AsNoTracking().ToListAsync()));

        var retired = await h.Submit(new SubmitDailyAttendanceRequest(
            school, Day, new[] { new AttendanceEntry(able, "OLD", 0, null) }));

        Assert.False(retired.IsSuccess);
        Assert.Equal(ErrorCodes.UnknownCode, Assert.Single(retired.Errors).Code);
    }

    [Fact]
    public async Task OneInvalidEntryWritesNothingAtAll_PartialSaveDefect()
    {
        using var h = new Harness();
        var school = await h.AddSchool();
        await h.AddCodes();
        var able = await h.AddStudent(school, "Able");
        var baker = await h.AddStudent(school, "Baker");

        var result = await h.Submit(new SubmitDailyAttendanceRequest(school, Day, new[]
        {
            new AttendanceEntry(able, "P", 0, null),
            new AttendanceEntry(baker, "A", 0, null),
            new AttendanceEntry(999999, "P", 0, null)
        }));

        Assert.False(result.IsSuccess);
        Assert.Empty(await h.InContext(db => db.AttendanceRecords.AsNoTracking().ToListAsync()));
        Assert.Empty(await h.InContext(db => db.AttendanceSubmissions.AsNoTracking().ToListAsync()));
    }
}
