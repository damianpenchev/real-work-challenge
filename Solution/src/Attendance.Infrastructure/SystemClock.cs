using Attendance.Application.Abstractions;

namespace Attendance.Infrastructure;

internal sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

    public DateOnly Today => DateOnly.FromDateTime(DateTimeOffset.UtcNow.Date);
}

// Full authentication is out of scope; this is the single seam an authenticated
// principal will be read through when it is wired up.
internal sealed class UnauthenticatedCurrentUser : ICurrentUser
{
    public string Name => "system";
}
