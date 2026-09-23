using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.Sqlite;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Attendance.Tests.Api;

internal sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"attendance-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.UseSetting("ConnectionStrings:Default", $"Data Source={_dbPath}");

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
        {
            return;
        }

        // The pooled SQLite connection keeps the file handle open past host shutdown.
        SqliteConnection.ClearAllPools();

        try
        {
            File.Delete(_dbPath);
        }
        catch (IOException)
        {
        }
    }
}
