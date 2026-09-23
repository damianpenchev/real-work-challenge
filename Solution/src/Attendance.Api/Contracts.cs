using System.ComponentModel.DataAnnotations;
using Attendance.Application.Common;
using Attendance.Application.Records;
using Microsoft.AspNetCore.Mvc;

namespace Attendance.Api;

public sealed record AttendanceEntryBody(
    [Required] int StudentId,
    string? Code,
    int MinutesLate,
    string? Notes);

public sealed record SubmitAttendanceBody(
    [Required] DateOnly Date,
    [Required][MinLength(1)] IReadOnlyList<AttendanceEntryBody> Entries);

public sealed record ChronicAbsenteeismResponse(
    int StudentId,
    string SchoolYear,
    int TotalAbsences,
    int Threshold,
    bool IsChronicallyAbsent);

internal static class ResultMapping
{
    public static ActionResult<T> ToActionResult<T>(this Result<T> result, ControllerBase controller) =>
        result.IsSuccess ? controller.Ok(result.Value!) : ApiProblems.FromValidation(result.Errors);
}
