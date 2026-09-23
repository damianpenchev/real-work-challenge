namespace Attendance.Domain;

public sealed class School
{
    private School(string name, int? absenceAlertThreshold, bool active)
    {
        Name = name;
        AbsenceAlertThreshold = absenceAlertThreshold;
        Active = active;
    }

    private School() => Name = null!;

    public int Id { get; private set; }

    public string Name { get; private set; }

    public int? AbsenceAlertThreshold { get; private set; }

    public bool Active { get; private set; }

    public static School Create(string name, int? absenceAlertThreshold, bool active = true)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("School name is required.", nameof(name));
        }

        if (absenceAlertThreshold is <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(absenceAlertThreshold), absenceAlertThreshold, "Threshold must be positive when set.");
        }

        return new School(name.Trim(), absenceAlertThreshold, active);
    }
}
