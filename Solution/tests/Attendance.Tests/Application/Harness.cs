using Attendance.Application;
using Attendance.Application.Abstractions;
using Attendance.Application.Common;
using Attendance.Application.Records;
using Attendance.Domain;
using Attendance.Infrastructure;
using Attendance.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Attendance.Tests.Application;

internal sealed class TestClock : IClock
{
    public DateTimeOffset UtcNow { get; set; } = new(2025, 3, 4, 8, 0, 0, TimeSpan.Zero);

    public DateOnly Today => DateOnly.FromDateTime(UtcNow.Date);
}

internal sealed class TestUser : ICurrentUser
{
    public string Name { get; set; } = "teacher";
}

internal sealed class Harness : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ServiceProvider _provider;

    public Harness(Action<AttendanceOptions>? configure = null)
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var services = new ServiceCollection();
        
        services.AddApplication(configure);
        services.AddInfrastructure(db => db.UseSqlite(_connection));
        services.AddSingleton<IClock>(Clock);
        services.AddScoped<ICurrentUser>(_ => User);
        _provider = services.BuildServiceProvider();

        using var scope = _provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<AttendanceDbContext>().Database.EnsureCreated();
    }

    public TestClock Clock { get; } = new();

    public TestUser User { get; } = new();

    public async Task<T> InContext<T>(Func<AttendanceDbContext, Task<T>> work)
    {
        using var scope = _provider.CreateScope();
        return await work(scope.ServiceProvider.GetRequiredService<AttendanceDbContext>());
    }

    public async Task<T> InScope<T>(Func<IServiceProvider, Task<T>> work)
    {
        using var scope = _provider.CreateScope();
        return await work(scope.ServiceProvider);
    }

    public async Task<Result<SubmitDailyAttendanceResponse>> Submit(SubmitDailyAttendanceRequest request)
    {
        using var scope = _provider.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<IAttendanceWriteService>()
            .SubmitAsync(request, CancellationToken.None);
    }

    public async Task<Result<StudentAttendanceHistory>> History(int studentId, string? schoolYear = null)
    {
        using var scope = _provider.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<IAttendanceReadService>()
            .GetHistoryAsync(studentId, schoolYear, CancellationToken.None);
    }

    public Task<int> AddSchool(int? threshold = null) => InContext(async db =>
    {
        var school = School.Create("Riverside", threshold);
        db.Add(school);
        await db.SaveChangesAsync();
        return school.Id;
    });

    public Task<int> AddStudent(int schoolId, string lastName = "Smith", bool active = true) => InContext(async db =>
    {
        var student = Student.Enrol(schoolId, "Ada", lastName, "5", active);
        db.Add(student);
        await db.SaveChangesAsync();
        return student.Id;
    });

    public Task<int> AddCodes() => InContext(async db =>
    {
        db.AddRange(
            AttendanceCode.Create("P", "Present", false, false, true),
            AttendanceCode.Create("A", "Absent unexcused", true, false, true),
            AttendanceCode.Create("AE", "Absent excused", true, true, true),
            AttendanceCode.Create("OLD", "Retired code", true, false, false));
        return await db.SaveChangesAsync();
    });

    public Task<int> AddRecord(int studentId, int schoolId, DateOnly date, string code) => InContext(async db =>
    {
        var codes = await db.AttendanceCodes.AsNoTracking().ToListAsync();
        db.Add(AttendanceRecord.Record(
            studentId, schoolId, date, codes.Single(c => c.Value == code), 0, null, Clock.UtcNow, "seed"));
        return await db.SaveChangesAsync();
    });

    public void Dispose()
    {
        _provider.Dispose();
        _connection.Dispose();
    }
}
