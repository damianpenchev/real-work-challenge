namespace Attendance.Application;

public sealed class AttendanceOptions
{
    public const string SectionName = "Attendance";

    public string DefaultPresentCode { get; set; } = "P";

    public int MaxBatchSize { get; set; } = 500;

    public bool AllowFutureDates { get; set; }

    public int DefaultAbsenceThreshold { get; set; } = 10;

    public string AbsenteeismPolicy { get; set; } = "AbsenceCount";
}
