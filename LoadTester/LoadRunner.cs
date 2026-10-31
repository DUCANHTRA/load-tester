using System.Diagnostics;

namespace LoadTester;

public class LoadRunner
{
    private readonly HttpClient _client;

    public LoadRunner(HttpClient client)
    {
        _client = client;
    }

    public async Task<RequestResult> SendOneAsync(Uri url)
    {
        var start = Stopwatch.GetTimestamp();

        using var response = await _client.GetAsync(url, HttpCompletionOption.ResponseContentRead);

        var elapsed = Stopwatch.GetElapsedTime(start);

        return new RequestResult(elapsed.TotalMilliseconds, (int)response.StatusCode, null);
    }

    public async Task<RunResult> RunAsync(Options options)
    {
        var results = new List<RequestResult>();

        var start = Stopwatch.GetTimestamp();

        for (var i = 0; i < options.Requests; i++)
        {
            var result = await SendOneAsync(options.Url);
            results.Add(result);
        }

        var totalTime = Stopwatch.GetElapsedTime(start);

        return new RunResult(results, totalTime);
    }

    public async Task<RunResult> RunWhenAllAsync(Options options)
    {
        var start = Stopwatch.GetTimestamp();

        var tasks = Enumerable.Range(0, options.Requests)
            .Select(_ => SendOneAsync(options.Url))
            .ToList();

        var results = await Task.WhenAll(tasks);

        var totalTime = Stopwatch.GetElapsedTime(start);

        return new RunResult(results, totalTime);
    }
}