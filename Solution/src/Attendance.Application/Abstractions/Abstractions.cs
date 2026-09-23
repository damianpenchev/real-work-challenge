using Attendance.Domain;

namespace Attendance.Application.Abstractions;

public interface IClock
{
    DateTimeOffset UtcNow { get; }

    DateOnly Today { get; }
}

public interface ICurrentUser
{
    string Name { get; }
}

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}

public interface ISchoolRepository
{
    Task<School?> FindAsync(int schoolId, CancellationToken cancellationToken);
}

public interface IStudentRepository
{
    Task<IReadOnlyList<Student>> GetBySchoolAsync(
        int schoolId, IReadOnlyCollection<int> studentIds, CancellationToken cancellationToken);

    Task<Student?> FindAsync(int studentId, CancellationToken cancellationToken);
}

public interface IAttendanceCodeRepository
{
    Task<IReadOnlyList<AttendanceCode>> GetAllAsync(CancellationToken cancellationToken);
}

public interface IAttendanceRepository
{
    Task<IReadOnlyList<AttendanceRecord>> GetForDateAsync(
        int schoolId, DateOnly date, IReadOnlyCollection<int> studentIds, CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<int, int>> CountAbsencesAsync(
        IReadOnlyCollection<int> studentIds,
        DateOnly fromInclusive,
        DateOnly toExclusive,
        DateOnly? excluding,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<AttendanceRecord>> GetHistoryAsync(
        int studentId, DateOnly fromInclusive, DateOnly toExclusive, CancellationToken cancellationToken);

    void Add(AttendanceRecord record);
}

public interface IStudentAlertRepository
{
    Task<IReadOnlyList<StudentAlert>> GetOpenAsync(
        string schoolYear, IReadOnlyCollection<int> studentIds, CancellationToken cancellationToken);

    void Add(StudentAlert alert);
}

public interface IAttendanceSubmissionRepository
{
    void Add(AttendanceSubmission submission);
}
