namespace Attendance.Application.Common;

public sealed record ValidationError(string Code, string Message, string? Field = null, int? Row = null);

public abstract class ResultBase
{
    protected ResultBase(IReadOnlyList<ValidationError> errors) => Errors = errors;

    public IReadOnlyList<ValidationError> Errors { get; }

    public bool IsSuccess => Errors.Count == 0;
}

public sealed class Result : ResultBase
{
    private Result(IReadOnlyList<ValidationError> errors) : base(errors)
    {
    }

    public static Result Success() => new(Array.Empty<ValidationError>());

    public static Result Failure(IEnumerable<ValidationError> errors) => new(errors.ToList());

    public static Result Failure(ValidationError error) => new(new[] { error });
}

public sealed class Result<T> : ResultBase
{
    private Result(T? value, IReadOnlyList<ValidationError> errors) : base(errors) => Value = value;

    public T? Value { get; }

    public static Result<T> Success(T value) => new(value, Array.Empty<ValidationError>());

    public static Result<T> Failure(IEnumerable<ValidationError> errors) => new(default, errors.ToList());

    public static Result<T> Failure(ValidationError error) => new(default, new[] { error });
}
