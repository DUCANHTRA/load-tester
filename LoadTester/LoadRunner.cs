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

            return new RequestResult(ElapsedMs(start), (int)response.StatusCode, null);
        }
        catch (OperationCanceledException)
        {
            return new RequestResult(ElapsedMs(start), null, "Timeout");
        }
        catch (HttpRequestException ex)
        {
            return new RequestResult(ElapsedMs(start), null, ex.HttpRequestError.ToString());
        }
    }

    private static double ElapsedMs(long start)
    {
        return Stopwatch.GetElapsedTime(start).TotalMilliseconds;
    }
}   