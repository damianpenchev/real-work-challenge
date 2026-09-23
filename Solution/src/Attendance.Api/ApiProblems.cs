using Attendance.Application.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Attendance.Api;

public sealed record ApiError(string Code, string Message, string? Field, int? Row);

internal static class ApiProblems
{
    public const string ContentType = "application/problem+json";

    public static ProblemDetails Build(int status, string title, IEnumerable<ApiError> errors)
    {
        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Type = $"https://httpstatuses.io/{status}"
        };

        problem.Extensions["errors"] = errors.ToArray();
        return problem;
    }

    public static ObjectResult Result(int status, string title, IEnumerable<ApiError> errors) =>
        new(Build(status, title, errors)) { StatusCode = status, ContentTypes = { ContentType } };

    public static ObjectResult FromValidation(IReadOnlyList<ValidationError> errors)
    {
        var notFound = errors.Any(e =>
            e.Code is ErrorCodes.StudentNotFound or ErrorCodes.SchoolNotFound);
        var status = notFound ? StatusCodes.Status404NotFound : StatusCodes.Status400BadRequest;
        var title = notFound ? "The requested resource was not found." : "The submission was rejected.";

        return Result(status, title, errors.Select(e => new ApiError(e.Code, e.Message, e.Field, e.Row)));
    }

    // One error contract for the whole API: model-state failures are reshaped into the
    // same payload a rejected roster returns.
    public static IActionResult FromModelState(ActionContext context)
    {
        var errors = context.ModelState
            .Where(kvp => kvp.Value?.Errors.Count > 0)
            .SelectMany(kvp => kvp.Value!.Errors.Select(e => new ApiError(
                "INVALID_REQUEST",
                string.IsNullOrWhiteSpace(e.ErrorMessage) ? "The value is not valid." : e.ErrorMessage,
                kvp.Key.Length == 0 ? null : kvp.Key,
                RowOf(kvp.Key))))
            .ToArray();

        return Result(StatusCodes.Status400BadRequest, "The request body was not valid.", errors);
    }

    private static int? RowOf(string key)
    {
        var open = key.IndexOf('[');
        var close = key.IndexOf(']');
        return open >= 0 && close > open && int.TryParse(key[(open + 1)..close], out var row) ? row : null;
    }
}
