namespace Attendance.Domain;

public sealed record SchoolYear
{
    public const int RolloverMonth = 9;
    public const int RolloverDay = 1;

    private SchoolYear(int startYear) => StartYear = startYear;

    public int StartYear { get; }

    public DateOnly Start => new(StartYear, RolloverMonth, RolloverDay);

    public DateOnly EndExclusive => new(StartYear + 1, RolloverMonth, RolloverDay);

    public string Label => $"{StartYear}-{StartYear + 1}";

    // Rollover day is the 1st, so the month alone decides the side of the boundary.
    public static SchoolYear ForDate(DateOnly date) =>
        new(date.Month >= RolloverMonth ? date.Year : date.Year - 1);

    public static SchoolYear StartingIn(int startYear)
    {
        if (startYear is < 1900 or > 9998)
        {
            throw new ArgumentOutOfRangeException(nameof(startYear), startYear, "School year is out of range.");
        }

        return new SchoolYear(startYear);
    }

    public static SchoolYear Parse(string label)
    {
        if (!TryParse(label, out var year))
        {
            throw new ArgumentException($"'{label}' is not a school year label of the form 2024-2025.", nameof(label));
        }

        return year;
    }

    public static bool TryParse(string? label, out SchoolYear year)
    {
        year = null!;
        if (label is null)
        {
            return false;
        }

        var parts = label.Split('-');
        if (parts.Length != 2
            || !int.TryParse(parts[0], out var first)
            || !int.TryParse(parts[1], out var second)
            || parts[0].Length != 4
            || parts[1].Length != 4
            || second != first + 1
            || first is < 1900 or > 9998)
        {
            return false;
        }

        year = new SchoolYear(first);
        return true;
    }

    public bool Contains(DateOnly date) => date >= Start && date < EndExclusive;

    public override string ToString() => Label;
}
