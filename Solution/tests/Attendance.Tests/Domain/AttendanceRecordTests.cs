using Attendance.Domain;

namespace Attendance.Tests.Domain;

public class AttendanceRecordTests
{
    private static readonly DateTimeOffset At = new(2025, 3, 4, 8, 30, 0, TimeSpan.Zero);
    private static readonly DateOnly Date = new(2025, 3, 4);

    private static AttendanceCode Present() => AttendanceCode.Create("P", "Present", false, false, true);

    private static AttendanceCode ExcusedAbsence() => AttendanceCode.Create("AE", "Absent excused", true, true, true);

    [Fact]
    public void FlagsAlwaysComeFromTheCode_LegacyStaleIsAbsentDefect()
    {
        var record = AttendanceRecord.Record(1, 7, Date, ExcusedAbsence(), 0, null, At, "teacher");
        Assert.True(record.IsAbsent);
        Assert.True(record.IsExcused);

        record.Amend(Present(), 0, null, At, "teacher");

        Assert.False(record.IsAbsent);
        Assert.False(record.IsExcused);
        Assert.Equal("P", record.Code);
    }

    [Fact]
    public void Create_RejectsMinutesLateOutOfRange()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => AttendanceRecord.Record(1, 7, Date, Present(), -1, null, At, "teacher"));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => AttendanceRecord.Record(1, 7, Date, Present(), 1441, null, At, "teacher"));
    }

    [Fact]
    public void Create_RejectsNotesBeyondTheColumnLength()
    {
        var notes = new string('x', AttendanceRecord.MaxNotesLength + 1);

        Assert.Throws<ArgumentException>(
            () => AttendanceRecord.Record(1, 7, Date, Present(), 0, notes, At, "teacher"));
    }

    [Fact]
    public void Amend_StampsWhoAndWhen()
    {
        var record = AttendanceRecord.Record(1, 7, Date, Present(), 0, null, At, "creator");
        var later = At.AddHours(2);

        record.Amend(ExcusedAbsence(), 15, " sick ", later, "amender");

        Assert.Equal("creator", record.CreatedBy);
        Assert.Equal(At, record.CreatedAt);
        Assert.Equal("amender", record.ModifiedBy);
        Assert.Equal(later, record.ModifiedAt);
        Assert.Equal("sick", record.Notes);
    }

    [Fact]
    public void ExcusedCodeMustAlsoBeAnAbsence()
    {
        Assert.Throws<ArgumentException>(() => AttendanceCode.Create("X", "Excused but present", false, true, true));
    }

    [Fact]
    public void CodeValueIsRequiredAndBounded()
    {
        Assert.Throws<ArgumentException>(() => AttendanceCode.Create("  ", "Blank", false, false, true));
        Assert.Throws<ArgumentException>(() => AttendanceCode.Create("TOOLONG", "Too long", false, false, true));
    }
}
