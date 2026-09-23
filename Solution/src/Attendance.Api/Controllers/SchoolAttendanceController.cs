using Attendance.Application.Records;
using Microsoft.AspNetCore.Mvc;

namespace Attendance.Api.Controllers;

[ApiController]
[Route("api/schools/{schoolId:int}/attendance")]
public sealed class SchoolAttendanceController : ControllerBase
{
    private readonly IAttendanceWriteService _write;

    public SchoolAttendanceController(IAttendanceWriteService write) => _write = write;

    [HttpPost]
    [ProducesResponseType(typeof(SubmitDailyAttendanceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SubmitDailyAttendanceResponse>> Submit(
        int schoolId, [FromBody] SubmitAttendanceBody body, CancellationToken cancellationToken)
    {
        var request = new SubmitDailyAttendanceRequest(
            schoolId,
            body.Date,
            body.Entries.Select(e => new AttendanceEntry(e.StudentId, e.Code, e.MinutesLate, e.Notes)).ToList());

        var result = await _write.SubmitAsync(request, cancellationToken);
        return result.ToActionResult(this);
    }
}
