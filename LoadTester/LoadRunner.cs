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
}