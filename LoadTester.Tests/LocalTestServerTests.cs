using System.Diagnostics;
using System.Net;

namespace LoadTester.Tests;

public class LocalTestServerTests
{
    [Fact]
    public async Task Ok_answers_200_and_fail_answers_500()
    {
        await using var server = await LocalTestServer.StartAsync();

        using var client = new HttpClient();

        using var ok = await client.GetAsync(server.Url("/ok"));
        using var fail = await client.GetAsync(server.Url("/fail"));

        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        Assert.Equal(HttpStatusCode.InternalServerError, fail.StatusCode);
    }

    [Fact]
    public async Task Slow_waits_for_the_time_we_ask()
    {
        await using var server = await LocalTestServer.StartAsync();
        using var client = new HttpClient();

        var start = Stopwatch.GetTimestamp();
        using var response = await client.GetAsync(server.Url("/slow?ms=300"));
        var elapsed = Stopwatch.GetElapsedTime(start);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(elapsed.TotalMilliseconds >= 250, $"Only waited {elapsed.TotalMilliseconds} ms");
    }

    [Fact]
    public async Task Server_counts_the_requests_it_gets()
    {
        await using var server = await LocalTestServer.StartAsync();
        using var client = new HttpClient();

        for (var i = 0; i < 3; i++)
        {
            using var response = await client.GetAsync(server.Url("/ok"));
        }

        Assert.Equal(3, server.TotalRequests);
        Assert.Equal(1, server.MaxInFlight);
    }
}
