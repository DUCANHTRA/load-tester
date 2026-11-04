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
}