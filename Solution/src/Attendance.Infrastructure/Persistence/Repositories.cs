using Attendance.Application.Abstractions;
using Attendance.Domain;
using Microsoft.EntityFrameworkCore;

namespace Attendance.Infrastructure.Persistence;

internal sealed class SchoolRepository : ISchoolRepository
{
    private readonly AttendanceDbContext _db;

    public SchoolRepository(AttendanceDbContext db) => _db = db;

    public async Task<School?> FindAsync(int schoolId, CancellationToken cancellationToken) =>
        await _db.Schools.AsNoTracking().FirstOrDefaultAsync(s => s.Id == schoolId, cancellationToken);
}

internal sealed class StudentRepository : IStudentRepository
{
    private readonly AttendanceDbContext _db;

    public StudentRepository(AttendanceDbContext db) => _db = db;

    public async Task<IReadOnlyList<Student>> GetBySchoolAsync(
        int schoolId, IReadOnlyCollection<int> studentIds, CancellationToken cancellationToken) =>
        await _db.Students
            .AsNoTracking()
            .Where(s => s.SchoolId == schoolId && studentIds.Contains(s.Id))
            .ToListAsync(cancellationToken);

    public async Task<Student?> FindAsync(int studentId, CancellationToken cancellationToken) =>
        await _db.Students.AsNoTracking().FirstOrDefaultAsync(s => s.Id == studentId, cancellationToken);
}

internal sealed class AttendanceCodeRepository : IAttendanceCodeRepository
{
    private readonly AttendanceDbContext _db;

    public AttendanceCodeRepository(AttendanceDbContext db) => _db = db;

    public async Task<IReadOnlyList<AttendanceCode>> GetAllAsync(CancellationToken cancellationToken) =>
        await _db.AttendanceCodes.AsNoTracking().ToListAsync(cancellationToken);
}

internal sealed class AttendanceRepository : IAttendanceRepository
{
    private readonly AttendanceDbContext _db;

    public AttendanceRepository(AttendanceDbContext db) => _db = db;

    public async Task<IReadOnlyList<AttendanceRecord>> GetForDateAsync(
        int schoolId, DateOnly date, IReadOnlyCollection<int> studentIds, CancellationToken cancellationToken) =>
        await _db.AttendanceRecords
            .Where(r => r.SchoolId == schoolId && r.Date == date && studentIds.Contains(r.StudentId))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyDictionary<int, int>> CountAbsencesAsync(
        IReadOnlyCollection<int> studentIds,
        DateOnly fromInclusive,
        DateOnly toExclusive,
        DateOnly? excluding,
        CancellationToken cancellationToken)
    {
        var rows = await _db.AttendanceRecords
            .AsNoTracking()
            .Where(r => studentIds.Contains(r.StudentId)
                        && r.Date >= fromInclusive
                        && r.Date < toExclusive
                        && r.IsAbsent
                        && (excluding == null || r.Date != excluding))
            .GroupBy(r => r.StudentId)
            .Select(g => new { StudentId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(r => r.StudentId, r => r.Count);
    }

    public async Task<IReadOnlyList<AttendanceRecord>> GetHistoryAsync(
        int studentId, DateOnly fromInclusive, DateOnly toExclusive, CancellationToken cancellationToken) =>
        await _db.AttendanceRecords
            .AsNoTracking()
            .Where(r => r.StudentId == studentId && r.Date >= fromInclusive && r.Date < toExclusive)
            .OrderByDescending(r => r.Date)
            .ToListAsync(cancellationToken);

    public void Add(AttendanceRecord record) => _db.AttendanceRecords.Add(record);
}

internal sealed class StudentAlertRepository : IStudentAlertRepository
{
    private readonly AttendanceDbContext _db;

    public StudentAlertRepository(AttendanceDbContext db) => _db = db;

    public async Task<IReadOnlyList<StudentAlert>> GetOpenAsync(
        string schoolYear, IReadOnlyCollection<int> studentIds, CancellationToken cancellationToken) =>
        await _db.StudentAlerts
            .AsNoTracking()
            .Where(a => a.SchoolYear == schoolYear && a.ResolvedAt == null && studentIds.Contains(a.StudentId))
            .ToListAsync(cancellationToken);

    public void Add(StudentAlert alert) => _db.StudentAlerts.Add(alert);
}

internal sealed class AttendanceSubmissionRepository : IAttendanceSubmissionRepository
{
    private readonly AttendanceDbContext _db;

    public AttendanceSubmissionRepository(AttendanceDbContext db) => _db = db;

    public void Add(AttendanceSubmission submission) => _db.AttendanceSubmissions.Add(submission);
}
