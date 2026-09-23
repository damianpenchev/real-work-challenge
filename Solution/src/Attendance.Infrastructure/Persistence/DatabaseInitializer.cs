using Attendance.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Attendance.Infrastructure;

public static class DatabaseInitializer
{
    public static async Task InitialiseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Persistence.AttendanceDbContext>();
        await db.Database.EnsureCreatedAsync(cancellationToken);

        if (await db.Schools.AnyAsync(cancellationToken))
        {
            return;
        }

        db.AddRange(
            AttendanceCode.Create("P", "Present", false, false, true),
            AttendanceCode.Create("L", "Late", false, false, true),
            AttendanceCode.Create("A", "Absent unexcused", true, false, true),
            AttendanceCode.Create("AE", "Absent excused", true, true, true));

        var school = School.Create("Riverside Elementary", 3);
        db.Add(school);
        await db.SaveChangesAsync(cancellationToken);

        db.AddRange(
            Student.Enrol(school.Id, "Ada", "Lovelace", "5"),
            Student.Enrol(school.Id, "Grace", "Hopper", "5"),
            Student.Enrol(school.Id, "Alan", "Turing", "5"));
        await db.SaveChangesAsync(cancellationToken);
    }
}
