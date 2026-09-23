using Attendance.Application.Abstractions;
using Attendance.Domain;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Attendance.Infrastructure.Persistence;

internal sealed class UnitOfWork : IUnitOfWork
{
    private readonly AttendanceDbContext _db;

    public UnitOfWork(AttendanceDbContext db) => _db = db;

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqliteException { SqliteErrorCode: 19 } sqlite)
        {
            throw new UniqueInvariantViolationException(Describe(sqlite.Message), ex);
        }
    }

    private static string Describe(string message) =>
        message.Contains("StudentAlerts", StringComparison.OrdinalIgnoreCase)
            ? UniqueInvariantViolationException.OneOpenAlertPerStudentPerYear
            : UniqueInvariantViolationException.OneRecordPerStudentPerDay;
}
