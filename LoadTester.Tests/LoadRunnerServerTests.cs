using Xunit.Sdk;

namespace LoadTester.Tests;

[Trait("Category", "Integration")]
public class LoadRunnerServerTests
{
    private static async Task<RunResult> RunAsync(
        Uri url,
        int requests,
        int concurrency,
        int timeoutSeconds = 10,
        CancellationToken cancellationToken = default)
    {
        using var client = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
        var runner = new LoadRunner(client);
        var options = new Options(url, requests, concurrency, timeoutSeconds);

        return await runner.RunAsync(options, cancellationToken);
    }


    [Fact]
    public async Task Ok_endpoint_gives_all_successes()
    {
        await using var server = await LocalTestServer.StartAsync();

        var run = await RunAsync(server.Url("/ok"), requests: 50, concurrency: 10);

        Assert.Equal(50, run.Results.Count);
        Assert.All(run.Results, r => Assert.Equal(200, r.StatusCode));
        Assert.All(run.Results, r => Assert.True(r.IsSuccess));
        Assert.False(run.WasCancelled);
        Assert.Equal(50, server.TotalRequests);
    }

    [Fact]
    public async Task Fai_endpoint_gives_all_failures()
    {
        await using var server = await LocalTestServer.StartAsync();

        var run = await RunAsync(server.Url("/fail"), requests: 50, concurrency: 10);
        var stats = Statistics.Calculate(run.Results, run.TotalTime);

        Assert.Equal(50, run.Results.Count);
        Assert.All(run.Results, r => Assert.Equal(500, r.StatusCode));
        Assert.All(run.Results, r => Assert.Null(r.Error));
        Assert.Equal(0, stats.Succeeded);
        Assert.Equal(50, stats.Failed);
        Assert.Equal(50, Statistics.CountStatusCodes(run.Results)[500]);
        Assert.Empty(Statistics.CountErrors(run.Results));
    }

    [Fact]
    public async Task Slow_endpoint_shows_up_in_the_latency()
    {
        await using var server = await LocalTestServer.StartAsync();

        var run = await RunAsync(server.Url("/slow?ms=200"), requests: 10, concurrency:10);
        var stats = Statistics.Calculate(run.Results, run.TotalTime);

        Assert.All(run.Results, r => Assert.True(r.IsSuccess));
        Assert.All(run.Results, r => Assert.True(r.LatencyMs >= 150, $"Latency was only {r.LatencyMs} ms"));
        Assert.True(stats.MinMs >= 150);
        Assert.True(stats.P50Ms >= 150);
    }

    [Fact]
    public async Task Short_timeout_gives_timeouts()
    {
        await using var server = await LocalTestServer.StartAsync();

        var run = await RunAsync(
            server.Url("slow?ms=3000"), requests: 3, concurrency: 3, timeoutSeconds: 1);

        Assert.Equal(3, run.Results.Count);
        Assert.All(run.Results, r => Assert.Equal("Timeout", r.Error));
        Assert.All(run.Results, r => Assert.Null(r.StatusCode));
        Assert.All(run.Results, r => Assert.InRange(r.LatencyMs, 900, 2500));
        Assert.True(run.TotalTime < TimeSpan.FromSeconds(2.5), "The run waited for the slow server");
    }

    [Fact]
    public async Task One_at_a_time_never_overlaps()
    {
        await using var server = await LocalTestServer.StartAsync();

        var run = await RunAsync(server.Url("/slow?ms=50"), requests: 10, concurrency: 1);

        Assert.Equal(10, server.TotalRequests);
        Assert.Equal(1, server.MaxInFlight);
    }

    [Fact]
    public async Task Concurrency_limit_is_respected()
    {
        await using var server = await LocalTestServer.StartAsync();

        var run = await RunAsync(server.Url("/slow?ms=200"), requests: 40, concurrency: 5);

        Assert.Equal(40, server.TotalRequests);
        Assert.InRange(server.MaxInFlight, 2, 5);
    }

    [Fact]
    public async Task Concurrency_makes_the_run_faster()
    {
        await using var server = await LocalTestServer.StartAsync();
        var url = server.Url("/slow?ms=100");

        var oneByOne = await RunAsync(url, requests: 10, concurrency: 1);
        var tenAtOnce = await RunAsync(url, requests: 10, concurrency: 10);

        Assert.True(
            tenAtOnce.TotalTime < oneByOne.TotalTime / 2,
            $"c=1 took {oneByOne.TotalTime.TotalMilliseconds} ms, c=10 took {tenAtOnce.TotalTime.TotalMilliseconds} ms"
        );
    }

    [Fact]
    public async Task Cancelling_during_a_run_stops_quickly()
    {
        await using var server = await LocalTestServer.StartAsync();
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(300));

        var run = await RunAsync(
            server.Url("/slow?ms=3000"), requests: 10, concurrency: 2, timeoutSeconds: 30,
            cancellationToken: cts.Token);

        Assert.True(run.WasCancelled);
        Assert.Empty(run.Results);
        Assert.True(run.TotalTime < TimeSpan.FromSeconds(2), "The run did not stop quickly");
    }
}