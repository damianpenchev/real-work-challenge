using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Attendance.Tests.Api;

public class AttendanceEndpointTests
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow.Date);

    private static object Roster(DateOnly date, params object[] entries) =>
        new { date = date.ToString("yyyy-MM-dd"), entries };

    private static object Entry(int studentId, string code, int minutesLate = 0, string? notes = null) =>
        new { studentId, code, minutesLate, notes };

    private static async Task<JsonElement> Body(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

    [Fact]
    public async Task PostingARosterRecordsTheDayAndTheHistoryReturnsIt()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var posted = await client.PostAsJsonAsync("/api/schools/1/attendance",
            Roster(Today, Entry(1, "P"), Entry(2, "A"), Entry(3, "AE", 0, "flu")));

        Assert.Equal(HttpStatusCode.OK, posted.StatusCode);
        var summary = await Body(posted);
        Assert.Equal(3, summary.GetProperty("created").GetInt32());
        Assert.Equal(0, summary.GetProperty("amended").GetInt32());

        var history = await Body(await client.GetAsync("/api/students/3/attendance"));
        var item = Assert.Single(history.GetProperty("items").EnumerateArray().ToList());
        Assert.Equal("AE", item.GetProperty("code").GetString());
        Assert.Equal("Absent excused", item.GetProperty("codeDescription").GetString());
        Assert.Equal(1, history.GetProperty("totalAbsences").GetInt32());
    }

    [Fact]
    public async Task AnUnknownCodeIsRejectedAsAProblemKeyedToTheOffendingRow()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/schools/1/attendance",
            Roster(Today, Entry(1, "P"), Entry(2, "ZZZ")));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var error = Assert.Single((await Body(response)).GetProperty("errors").EnumerateArray().ToList());
        Assert.Equal("UNKNOWN_ATTENDANCE_CODE", error.GetProperty("code").GetString());
        Assert.Equal("Code", error.GetProperty("field").GetString());
        Assert.Equal(1, error.GetProperty("row").GetInt32());
    }

    [Fact]
    public async Task ModelStateFailuresUseTheSameErrorContract()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/schools/1/attendance", Roster(Today));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var errors = (await Body(response)).GetProperty("errors").EnumerateArray().ToList();
        Assert.NotEmpty(errors);
        Assert.All(errors, e => Assert.Equal("INVALID_REQUEST", e.GetProperty("code").GetString()));
    }

    [Fact]
    public async Task AnUnknownStudentReturnsANotFoundProblem()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/students/9999/attendance");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var error = Assert.Single((await Body(response)).GetProperty("errors").EnumerateArray().ToList());
        Assert.Equal("STUDENT_NOT_FOUND", error.GetProperty("code").GetString());
    }

    [Fact]
    public async Task ChronicAbsenteeismFlipsOnceTheSchoolThresholdIsCrossed()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var before = await Body(await client.GetAsync("/api/students/1/chronic-absenteeism"));
        Assert.False(before.GetProperty("isChronicallyAbsent").GetBoolean());
        Assert.Equal(3, before.GetProperty("threshold").GetInt32());

        foreach (var offset in new[] { 0, -1, -2 })
        {
            var response = await client.PostAsJsonAsync("/api/schools/1/attendance",
                Roster(Today.AddDays(offset), Entry(1, "A")));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        var after = await Body(await client.GetAsync("/api/students/1/chronic-absenteeism"));
        Assert.Equal(3, after.GetProperty("totalAbsences").GetInt32());
        Assert.True(after.GetProperty("isChronicallyAbsent").GetBoolean());
    }

    [Fact]
    public async Task ResubmittingTheSameDayAmendsRatherThanDuplicating()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();

        await client.PostAsJsonAsync("/api/schools/1/attendance", Roster(Today, Entry(2, "A")));
        var second = await client.PostAsJsonAsync("/api/schools/1/attendance",
            Roster(Today, Entry(2, "P", 0, "keyed in error")));

        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        var summary = await Body(second);
        Assert.Equal(0, summary.GetProperty("created").GetInt32());
        Assert.Equal(1, summary.GetProperty("amended").GetInt32());

        var history = await Body(await client.GetAsync("/api/students/2/attendance"));
        Assert.Single(history.GetProperty("items").EnumerateArray().ToList());
        Assert.Equal(0, history.GetProperty("totalAbsences").GetInt32());
    }
}
