using Attendance.Application.Common;
using Attendance.Domain;

namespace Attendance.Application.Records;

internal sealed record ValidatedEntry(AttendanceEntry Entry, AttendanceCode Code);

internal static class SubmissionValidator
{
    public static List<ValidatedEntry> Validate(
        SubmitDailyAttendanceRequest request,
        IReadOnlyDictionary<int, Student> roster,
        IReadOnlyDictionary<string, AttendanceCode> codes,
        string defaultPresentCode,
        out List<ValidationError> errors)
    {
        errors = new List<ValidationError>();
        var validated = new List<ValidatedEntry>(request.Entries.Count);
        var seen = new HashSet<int>();

        for (var row = 0; row < request.Entries.Count; row++)
        {
            var entry = request.Entries[row];

            if (!seen.Add(entry.StudentId))
            {
                errors.Add(Error(ErrorCodes.DuplicateStudent,
                    "The same student appears twice in this submission.", nameof(entry.StudentId), row));
                continue;
            }

            if (!roster.TryGetValue(entry.StudentId, out var student))
            {
                errors.Add(Error(ErrorCodes.StudentNotInSchool,
                    "The student is not enrolled at this school.", nameof(entry.StudentId), row));
                continue;
            }

            if (!student.Active)
            {
                errors.Add(Error(ErrorCodes.StudentInactive,
                    "The student is no longer active.", nameof(entry.StudentId), row));
                continue;
            }

            var value = string.IsNullOrWhiteSpace(entry.Code) ? defaultPresentCode : entry.Code.Trim();
            if (!codes.TryGetValue(value, out var code) || !code.IsActive)
            {
                errors.Add(Error(ErrorCodes.UnknownCode,
                    $"'{value}' is not an active attendance code.", nameof(entry.Code), row));
                continue;
            }

            if (entry.MinutesLate is < 0 or > AttendanceRecord.MaxMinutesLate)
            {
                errors.Add(Error(ErrorCodes.MinutesLateOutOfRange,
                    $"Minutes late must be between 0 and {AttendanceRecord.MaxMinutesLate}.",
                    nameof(entry.MinutesLate), row));
                continue;
            }

            if (entry.Notes is not null && entry.Notes.Trim().Length > AttendanceRecord.MaxNotesLength)
            {
                errors.Add(Error(ErrorCodes.NotesTooLong,
                    $"Notes may not exceed {AttendanceRecord.MaxNotesLength} characters.", nameof(entry.Notes), row));
                continue;
            }

            validated.Add(new ValidatedEntry(entry, code));
        }

        return validated;
    }

    private static ValidationError Error(string code, string message, string field, int row) =>
        new(code, message, field, row);
}
