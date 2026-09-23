using Attendance.Application.Abstractions;
using Attendance.Application.Common;
using Attendance.Domain;
using Attendance.Domain.Absenteeism;

namespace Attendance.Application.Records;

public sealed class AttendanceWriteService : IAttendanceWriteService
{
    private readonly ISchoolRepository _schools;
    private readonly IStudentRepository _students;
    private readonly IAttendanceCodeRepository _codes;
    private readonly IAttendanceRepository _attendance;
    private readonly IStudentAlertRepository _alerts;
    private readonly IAttendanceSubmissionRepository _submissions;
    private readonly IAbsenteeismPolicy _policy;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly ICurrentUser _currentUser;
    private readonly AttendanceOptions _options;

    public AttendanceWriteService(
        ISchoolRepository schools,
        IStudentRepository students,
        IAttendanceCodeRepository codes,
        IAttendanceRepository attendance,
        IStudentAlertRepository alerts,
        IAttendanceSubmissionRepository submissions,
        IAbsenteeismPolicy policy,
        IUnitOfWork unitOfWork,
        IClock clock,
        ICurrentUser currentUser,
        AttendanceOptions options)
    {
        _schools = schools;
        _students = students;
        _codes = codes;
        _attendance = attendance;
        _alerts = alerts;
        _submissions = submissions;
        _policy = policy;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _currentUser = currentUser;
        _options = options;
    }

    public async Task<Result<SubmitDailyAttendanceResponse>> SubmitAsync(
        SubmitDailyAttendanceRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Entries.Count == 0)
        {
            return Fail(ErrorCodes.BatchEmpty, "The submission contains no entries.");
        }

        if (request.Entries.Count > _options.MaxBatchSize)
        {
            return Fail(ErrorCodes.BatchTooLarge, $"A submission may not exceed {_options.MaxBatchSize} entries.");
        }

        if (!_options.AllowFutureDates && request.Date > _clock.Today)
        {
            return Fail(ErrorCodes.FutureDate, "Attendance cannot be recorded for a future date.", "date");
        }

        var school = await _schools.FindAsync(request.SchoolId, cancellationToken);
        if (school is null || !school.Active)
        {
            return Fail(ErrorCodes.SchoolNotFound, "The school does not exist or is not active.", "schoolId");
        }

        var studentIds = request.Entries.Select(e => e.StudentId).Distinct().ToList();
        var roster = (await _students.GetBySchoolAsync(request.SchoolId, studentIds, cancellationToken))
            .ToDictionary(s => s.Id);
        var codes = (await _codes.GetAllAsync(cancellationToken))
            .ToDictionary(c => c.Value, StringComparer.OrdinalIgnoreCase);

        var validated = SubmissionValidator.Validate(
            request, roster, codes, _options.DefaultPresentCode, out var errors);
        if (errors.Count > 0)
        {
            return Result<SubmitDailyAttendanceResponse>.Failure(errors);
        }

        var existing = (await _attendance.GetForDateAsync(request.SchoolId, request.Date, studentIds, cancellationToken))
            .ToDictionary(r => r.StudentId);
        var now = _clock.UtcNow;
        var actor = _currentUser.Name;
        var created = 0;
        var amended = 0;

        foreach (var (entry, code) in validated)
        {
            if (existing.TryGetValue(entry.StudentId, out var record))
            {
                record.Amend(code, entry.MinutesLate, entry.Notes, now, actor);
                amended++;
            }
            else
            {
                _attendance.Add(AttendanceRecord.Record(
                    entry.StudentId, request.SchoolId, request.Date, code, entry.MinutesLate, entry.Notes, now, actor));
                created++;
            }
        }

        var schoolYear = SchoolYear.ForDate(request.Date);
        await RaiseAlerts(schoolYear, school, validated, studentIds, request.Date, now, cancellationToken);

        _submissions.Add(AttendanceSubmission.Log(request.SchoolId, request.Date, request.Entries.Count, now, actor));
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<SubmitDailyAttendanceResponse>.Success(
            new SubmitDailyAttendanceResponse(request.Date, schoolYear.Label, created, amended));
    }

    private async Task RaiseAlerts(
        SchoolYear schoolYear,
        School school,
        List<ValidatedEntry> validated,
        List<int> studentIds,
        DateOnly date,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        // Prior absences come from the indexed half-open range with the day being written excluded,
        // so the tally is always derived from the records and never from a stored counter.
        var prior = await _attendance.CountAbsencesAsync(
            studentIds, schoolYear.Start, schoolYear.EndExclusive, date, cancellationToken);
        var open = (await _alerts.GetOpenAsync(schoolYear.Label, studentIds, cancellationToken))
            .Select(a => a.StudentId)
            .ToHashSet();

        foreach (var (entry, code) in validated)
        {
            var total = (prior.TryGetValue(entry.StudentId, out var count) ? count : 0) + (code.IsAbsent ? 1 : 0);
            var status = _policy.Evaluate(schoolYear, total, school.AbsenceAlertThreshold);

            if (status.IsChronic && open.Add(entry.StudentId))
            {
                _alerts.Add(StudentAlert.RaiseChronicAbsence(entry.StudentId, school.Id, schoolYear, total, now));
            }
        }
    }

    private static Result<SubmitDailyAttendanceResponse> Fail(string code, string message, string? field = null) =>
        Result<SubmitDailyAttendanceResponse>.Failure(new ValidationError(code, message, field));
}
