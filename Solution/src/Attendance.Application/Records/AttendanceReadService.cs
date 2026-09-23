using Attendance.Application.Abstractions;
using Attendance.Application.Common;
using Attendance.Domain;
using Attendance.Domain.Absenteeism;

namespace Attendance.Application.Records;

public sealed class AttendanceReadService : IAttendanceReadService
{
    private readonly IStudentRepository _students;
    private readonly ISchoolRepository _schools;
    private readonly IAttendanceCodeRepository _codes;
    private readonly IAttendanceRepository _attendance;
    private readonly IAbsenteeismPolicy _policy;
    private readonly IClock _clock;

    public AttendanceReadService(
        IStudentRepository students,
        ISchoolRepository schools,
        IAttendanceCodeRepository codes,
        IAttendanceRepository attendance,
        IAbsenteeismPolicy policy,
        IClock clock)
    {
        _students = students;
        _schools = schools;
        _codes = codes;
        _attendance = attendance;
        _policy = policy;
        _clock = clock;
    }

    public async Task<Result<StudentAttendanceHistory>> GetHistoryAsync(
        int studentId, string? schoolYear, CancellationToken cancellationToken)
    {
        SchoolYear year;
        if (schoolYear is null)
        {
            year = SchoolYear.ForDate(_clock.Today);
        }
        else if (!SchoolYear.TryParse(schoolYear, out year))
        {
            return Result<StudentAttendanceHistory>.Failure(new ValidationError(
                ErrorCodes.UnknownSchoolYear, "The school year must look like 2024-2025.", "schoolYear"));
        }

        var student = await _students.FindAsync(studentId, cancellationToken);
        if (student is null)
        {
            return Result<StudentAttendanceHistory>.Failure(new ValidationError(
                ErrorCodes.StudentNotFound, "The student does not exist.", "studentId"));
        }

        var school = await _schools.FindAsync(student.SchoolId, cancellationToken);
        var records = await _attendance.GetHistoryAsync(studentId, year.Start, year.EndExclusive, cancellationToken);
        var codes = (await _codes.GetAllAsync(cancellationToken))
            .ToDictionary(c => c.Value, StringComparer.OrdinalIgnoreCase);

        // Descriptions are looked up, not joined: a retired code must not make history disappear.
        var items = records
            .Select(r => new AttendanceHistoryItem(
                r.Date,
                r.Code,
                codes.TryGetValue(r.Code, out var code) ? code.Description : r.Code,
                r.IsAbsent,
                r.IsExcused,
                r.MinutesLate,
                r.Notes))
            .ToList();

        var status = _policy.Evaluate(year, records.Count(r => r.IsAbsent), school?.AbsenceAlertThreshold);

        return Result<StudentAttendanceHistory>.Success(new StudentAttendanceHistory(
            studentId, year.Label, items, status.Absences, status.Threshold, status.IsChronic));
    }
}
