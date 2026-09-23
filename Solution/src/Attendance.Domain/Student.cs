namespace Attendance.Domain;

public sealed class Student
{
    private Student(int schoolId, string firstName, string lastName, string? grade, bool active)
    {
        SchoolId = schoolId;
        FirstName = firstName;
        LastName = lastName;
        Grade = grade;
        Active = active;
    }

    private Student()
    {
        FirstName = null!;
        LastName = null!;
    }

    public int Id { get; private set; }

    public int SchoolId { get; private set; }

    public string FirstName { get; private set; }

    public string LastName { get; private set; }

    public string? Grade { get; private set; }

    public bool Active { get; private set; }

    public static Student Enrol(int schoolId, string firstName, string lastName, string? grade, bool active = true)
    {
        if (schoolId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(schoolId), schoolId, "School id must be positive.");
        }

        if (string.IsNullOrWhiteSpace(firstName))
        {
            throw new ArgumentException("First name is required.", nameof(firstName));
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            throw new ArgumentException("Last name is required.", nameof(lastName));
        }

        return new Student(schoolId, firstName.Trim(), lastName.Trim(), grade?.Trim(), active);
    }
}
