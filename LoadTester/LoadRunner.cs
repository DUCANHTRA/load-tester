using System.Diagnostics;

namespace LoadTester;

public class LoadRunner
{
    private readonly HttpClient _client;

    public LoadRunner(HttpClient client)
    {
        _client = client;
    }

    public async Task<RunResult> RunAsync(Options options, CancellationToken cancellationToken = default)
    {
        var results = new RequestResult?[options.Requests];
        var timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);

        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = options.Concurrency,
            CancellationToken = cancellationToken
        };

        var wasCancellaed = false;
        var start = Stopwatch.GetTimestamp();

        try {
            await Parallel.ForEachAsync(
                Enumerable.Range(0, options.Requests),
                parallelOptions,
                async (i, token) =>
                {
                    results[i] = await SendOneAsync(options.Url, timeout, token);
                });
        }
        catch (OperationCanceledException)
        {
            wasCancellaed = true;
        }

        var totalTime = Stopwatch.GetElapsedTime(start);
        var finished = results.OfType<RequestResult>().ToList();

        return new RunResult(finished, totalTime, wasCancellaed);
    }

    public async Task<RequestResult> SendOneAsync(Uri url, TimeSpan timeout, CancellationToken cancellationToken)
    {
        var start = Stopwatch.GetTimestamp();

        // Timeout cancellation token source is linked to the 
        // provided cancellation token
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(timeout);

        try
        {
            using var response = await _client.GetAsync(url, HttpCompletionOption.ResponseContentRead, timeoutSource.Token);

            return new RequestResult(ElapsedMs(start), (int)response.StatusCode, null);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
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