using Attendance.Application.Records;
using Microsoft.AspNetCore.Mvc;

namespace Attendance.Api.Controllers;

[ApiController]
[Route("api/students/{studentId:int}")]
public sealed class StudentAttendanceController : ControllerBase
{
    private readonly IAttendanceReadService _read;

    public StudentAttendanceController(IAttendanceReadService read) => _read = read;

    [HttpGet("attendance")]
    [ProducesResponseType(typeof(StudentAttendanceHistory), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StudentAttendanceHistory>> GetHistory(
        int studentId, [FromQuery] string? schoolYear, CancellationToken cancellationToken)
    {
        var result = await _read.GetHistoryAsync(studentId, schoolYear, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpGet("chronic-absenteeism")]
    [ProducesResponseType(typeof(ChronicAbsenteeismResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChronicAbsenteeismResponse>> GetChronicAbsenteeism(
        int studentId, [FromQuery] string? schoolYear, CancellationToken cancellationToken)
    {
        var result = await _read.GetHistoryAsync(studentId, schoolYear, cancellationToken);
        if (!result.IsSuccess)
        {
            return ApiProblems.FromValidation(result.Errors);
        }

        var history = result.Value!;
        return Ok(new ChronicAbsenteeismResponse(
            history.StudentId,
            history.SchoolYear,
            history.TotalAbsences,
            history.Threshold,
            history.IsChronicallyAbsent));
    }
}
