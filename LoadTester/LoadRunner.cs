using System.Diagnostics;

namespace LoadTester;

public class LoadRunner
{
    private readonly HttpClient _client;

    public LoadRunner(HttpClient client)
    {
        _client = client;
    }

    public async Task<RunResult> RunAsync(Options options)
    {
        var results = new RequestResult[options.Requests];

        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = options.Concurrency
        };

        var start = Stopwatch.GetTimestamp();

        await Parallel.ForEachAsync(
            Enumerable.Range(0, options.Requests),
            parallelOptions,
            async (i, _) =>
            {
                results[i] = await SendOneAsync(options.Url, TimeSpan.FromSeconds(options.TimeoutSeconds));
            });

        var totalTime = Stopwatch.GetElapsedTime(start);

        return new RunResult(results, totalTime);
    }

    public async Task<RequestResult> SendOneAsync(Uri url, TimeSpan timeout)
    {
        var start = Stopwatch.GetTimestamp();

        using var timeoutSource = new CancellationTokenSource(timeout);
    
        try
        {
            using var response = await _client.GetAsync(url, HttpCompletionOption.ResponseContentRead, timeoutSource.Token);

            var elapsed = Stopwatch.GetElapsedTime(start);

            return new RequestResult(elapsed.TotalMilliseconds, (int)response.StatusCode, null);
        }
        catch (OperationCanceledException)
        {
            var elapsed = Stopwatch.GetElapsedTime(start);
            
            return new RequestResult(elapsed.TotalMilliseconds, null, "Timeout");
        }
    }
}   