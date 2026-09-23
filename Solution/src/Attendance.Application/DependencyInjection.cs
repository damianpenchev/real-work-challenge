using Attendance.Application.Records;
using Attendance.Domain.Absenteeism;
using Microsoft.Extensions.DependencyInjection;

namespace Attendance.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services, Action<AttendanceOptions>? configure = null)
    {
        var options = new AttendanceOptions();
        configure?.Invoke(options);
        services.AddSingleton(options);

        services.AddScoped<IAttendanceWriteService, AttendanceWriteService>();
        services.AddScoped<IAttendanceReadService, AttendanceReadService>();
        services.AddScoped<IAbsenteeismPolicy>(_ => options.AbsenteeismPolicy switch
        {
            "AbsenceCount" => new AbsenceCountPolicy(options.DefaultAbsenceThreshold),
            _ => throw new InvalidOperationException(
                $"Unknown absenteeism policy '{options.AbsenteeismPolicy}'.")
        });

        return services;
    }
}
