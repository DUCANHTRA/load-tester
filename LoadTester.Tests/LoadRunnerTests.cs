using System.Net;

namespace LoadTester.Tests;

public class LoadRunnerTests
{
    private static readonly Uri Url = new("https://test.local/");

    // small class that stands in for the network. 
    // Every request goes to that function.
    private class FakeHandler : HttpMessageHandler
    {
        private readonly Func<CancellationToken, Task<HttpResponseMessage>> _onSend;

        public FakeHandler(Func<CancellationToken, Task<HttpResponseMessage>> onSend)
        {
            _onSend = onSend;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return _onSend(cancellationToken);
        }
    }

    // a LoadRunner that uses a fake handler
    private static LoadRunner RunnerWith(Func<CancellationToken, Task<HttpResponseMessage>> onSend)
    {
        var client = new HttpClient(new FakeHandler(onSend));
        return new LoadRunner(client);
    }

    // fake answer with the status code we choose.
    private static Task<HttpResponseMessage> Respond(HttpStatusCode code)
    {
        return Task.FromResult(new HttpResponseMessage(code));
    }

    private static async Task<HttpResponseMessage> HangUntilCancelled(CancellationToken token)
    {
        await Task.Delay(Timeout.Infinite, token);
        return new HttpResponseMessage(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Success_gives_status_code_and_no_error()
    {
        var runner = RunnerWith(_ => Respond(HttpStatusCode.OK));

        var result = await runner.SendOneAsync(Url, TimeSpan.FromSeconds(5), CancellationToken.None);

        Assert.Equal(200, result.StatusCode);
        Assert.Null(result.Error);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Server_error_is_a_failed_result_not_an_exception()
    {
        var runner = RunnerWith(_ => Respond(HttpStatusCode.InternalServerError));

        var result = await runner.SendOneAsync(Url, TimeSpan.FromSeconds(5), CancellationToken.None);

        Assert.Equal(500, result.StatusCode);
        Assert.Null(result.Error);
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Network_error_is_recorded_with_its_category()
    {
        var runner = RunnerWith(_ =>
            throw new HttpRequestException(HttpRequestError.ConnectionError, "refused"));

        var result = await runner.SendOneAsync(Url, TimeSpan.FromSeconds(5), CancellationToken.None);

        Assert.Null(result.StatusCode);
        Assert.Equal("ConnectionError", result.Error);
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Slow_server_gives_a_timeout()
    {
        var runner = RunnerWith(HangUntilCancelled);

        var result = await runner.SendOneAsync(Url, TimeSpan.FromMilliseconds(100), CancellationToken.None);

        Assert.Null(result.StatusCode);
        Assert.Equal("Timeout", result.Error);
    }

    // ---- A whole run ----

    [Fact]
    public async Task Normal_run_returns_every_result()
    {
        var runner = RunnerWith(_ => Respond(HttpStatusCode.OK));
        var options = new Options(Url, Requests: 20, Concurrency: 5, TimeoutSeconds: 5);

        var run = await runner.RunAsync(options, CancellationToken.None);

        Assert.Equal(20, run.Results.Count);
        Assert.All(run.Results, r => Assert.True(r.IsSuccess));
        Assert.False(run.WasCancelled);
    }

    [Fact]
    public async Task Already_cancelled_run_sends_nothing()
    {
        var runner = RunnerWith(_ => Respond(HttpStatusCode.OK));
        var options = new Options(Url, Requests: 20, Concurrency: 5, TimeoutSeconds: 5);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var run = await runner.RunAsync(options, cts.Token);

        Assert.True(run.WasCancelled);
        Assert.Empty(run.Results);
    }

    [Fact]
    public async Task Cancelling_during_a_run_stops_it_and_does_not_count_timeouts()
    {
        var runner = RunnerWith(HangUntilCancelled);
        var options = new Options(Url, Requests: 10, Concurrency: 2, TimeoutSeconds: 30);
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(100));

        var run = await runner.RunAsync(options, cts.Token);

        Assert.True(run.WasCancelled);
        Assert.Empty(run.Results);
    }
}
