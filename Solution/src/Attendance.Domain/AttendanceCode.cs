namespace Attendance.Domain;

public sealed class AttendanceCode
{
    public const int MaxValueLength = 5;

    private AttendanceCode(string value, string description, bool isAbsent, bool isExcused, bool isActive)
    {
        Value = value;
        Description = description;
        IsAbsent = isAbsent;
        IsExcused = isExcused;
        IsActive = isActive;
    }

    private AttendanceCode()
    {
        Value = null!;
        Description = null!;
    }

    public string Value { get; private set; }

    public string Description { get; private set; }

    public bool IsAbsent { get; private set; }

    public bool IsExcused { get; private set; }

    public bool IsActive { get; private set; }

    public static AttendanceCode Create(string value, string description, bool isAbsent, bool isExcused, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > MaxValueLength)
        {
            throw new ArgumentException($"Attendance code must be 1 to {MaxValueLength} characters.", nameof(value));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Attendance code description is required.", nameof(description));
        }

        if (isExcused && !isAbsent)
        {
            throw new ArgumentException("An excused code must also be an absence.", nameof(isExcused));
        }

        return new AttendanceCode(value.Trim().ToUpperInvariant(), description.Trim(), isAbsent, isExcused, isActive);
    }
}
