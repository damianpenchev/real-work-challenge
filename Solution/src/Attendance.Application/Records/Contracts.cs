using Attendance.Application.Common;

namespace Attendance.Application.Records;

public sealed record AttendanceEntry(int StudentId, string? Code, int MinutesLate, string? Notes);

public sealed record SubmitDailyAttendanceRequest(int SchoolId, DateOnly Date, IReadOnlyList<AttendanceEntry> Entries);

public sealed record SubmitDailyAttendanceResponse(DateOnly Date, string SchoolYear, int Created, int Amended);

public sealed record AttendanceHistoryItem(
    DateOnly Date,
    string Code,
    string CodeDescription,
    bool IsAbsent,
    bool IsExcused,
    int MinutesLate,
    string? Notes);

public sealed record StudentAttendanceHistory(
    int StudentId,
    string SchoolYear,
    IReadOnlyList<AttendanceHistoryItem> Items,
    int TotalAbsences,
    int Threshold,
    bool IsChronicallyAbsent);

public interface IAttendanceWriteService
{
    Task<Result<SubmitDailyAttendanceResponse>> SubmitAsync(
        SubmitDailyAttendanceRequest request, CancellationToken cancellationToken);
}

public interface IAttendanceReadService
{
    Task<Result<StudentAttendanceHistory>> GetHistoryAsync(
        int studentId, string? schoolYear, CancellationToken cancellationToken);
}
