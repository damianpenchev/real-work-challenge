using Attendance.Application.Abstractions;
using Attendance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Attendance.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, Action<DbContextOptionsBuilder> configureDb)
    {
        services.AddDbContext<AttendanceDbContext>(configureDb);
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<ISchoolRepository, SchoolRepository>();
        services.AddScoped<IStudentRepository, StudentRepository>();
        services.AddScoped<IAttendanceCodeRepository, AttendanceCodeRepository>();
        services.AddScoped<IAttendanceRepository, AttendanceRepository>();
        services.AddScoped<IStudentAlertRepository, StudentAlertRepository>();
        services.AddScoped<IAttendanceSubmissionRepository, AttendanceSubmissionRepository>();
        services.TryAddClock();
        return services;
    }

    private static void TryAddClock(this IServiceCollection services)
    {
        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<ICurrentUser, UnauthenticatedCurrentUser>();
    }
}
