namespace Attendance.Domain;

public sealed class AttendanceRecord
{
    public const int MaxMinutesLate = 1440;
    public const int MaxNotesLength = 500;

    private AttendanceRecord(
        int studentId,
        int schoolId,
        DateOnly date,
        AttendanceCode code,
        int minutesLate,
        string? notes,
        DateTimeOffset at,
        string by)
    {
        StudentId = studentId;
        SchoolId = schoolId;
        Date = date;
        Code = code.Value;
        IsAbsent = code.IsAbsent;
        IsExcused = code.IsExcused;
        MinutesLate = minutesLate;
        Notes = notes;
        CreatedAt = at;
        CreatedBy = by;
    }

    private AttendanceRecord()
    {
        Code = null!;
        CreatedBy = null!;
    }

    public int Id { get; private set; }

    public int StudentId { get; private set; }

    public int SchoolId { get; private set; }

    public DateOnly Date { get; private set; }

    public string Code { get; private set; }

    public bool IsAbsent { get; private set; }

    public bool IsExcused { get; private set; }

    public int MinutesLate { get; private set; }

    public string? Notes { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public string CreatedBy { get; private set; }

    public DateTimeOffset? ModifiedAt { get; private set; }

    public string? ModifiedBy { get; private set; }

    public static AttendanceRecord Record(
        int studentId,
        int schoolId,
        DateOnly date,
        AttendanceCode code,
        int minutesLate,
        string? notes,
        DateTimeOffset at,
        string by)
    {
        ArgumentNullException.ThrowIfNull(code);
        Guard(studentId, schoolId, minutesLate, notes, by);
        return new AttendanceRecord(studentId, schoolId, date, code, minutesLate, Normalise(notes), at, by);
    }

    public void Amend(AttendanceCode code, int minutesLate, string? notes, DateTimeOffset at, string by)
    {
        ArgumentNullException.ThrowIfNull(code);
        GuardMinutes(minutesLate);
        GuardNotes(notes);
        GuardActor(by);

        Code = code.Value;
        IsAbsent = code.IsAbsent;
        IsExcused = code.IsExcused;
        MinutesLate = minutesLate;
        Notes = Normalise(notes);
        ModifiedAt = at;
        ModifiedBy = by;
    }

    private static void Guard(int studentId, int schoolId, int minutesLate, string? notes, string by)
    {
        if (studentId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(studentId), studentId, "Student id must be positive.");
        }

        if (schoolId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(schoolId), schoolId, "School id must be positive.");
        }

        GuardMinutes(minutesLate);
        GuardNotes(notes);
        GuardActor(by);
    }

    private static void GuardMinutes(int minutesLate)
    {
        if (minutesLate is < 0 or > MaxMinutesLate)
        {
            throw new ArgumentOutOfRangeException(
                nameof(minutesLate), minutesLate, $"Minutes late must be between 0 and {MaxMinutesLate}.");
        }
    }

    private static void GuardNotes(string? notes)
    {
        if (notes is not null && notes.Trim().Length > MaxNotesLength)
        {
            throw new ArgumentException($"Notes may not exceed {MaxNotesLength} characters.", nameof(notes));
        }
    }

    private static void GuardActor(string by)
    {
        if (string.IsNullOrWhiteSpace(by))
        {
            throw new ArgumentException("The acting user is required.", nameof(by));
        }
    }

    private static string? Normalise(string? notes) =>
        string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
}
