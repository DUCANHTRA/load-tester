using LoadTester;

var result = ArgumentParser.Parse(args);

if (!result.IsSuccess)
{
    Console.Error.WriteLine($"Error: {result.Error}");
    Console.Error.WriteLine();
    Console.Error.WriteLine(Usage.Text);
    return 1;
}

var options = result.Options!;

Console.WriteLine($"Target:       {options.Url}");
Console.WriteLine($"Requests:     {options.Requests}");
Console.WriteLine($"Concurrency:  {options.Concurrency}");
Console.WriteLine($"Timeout:      {options.TimeoutSeconds}s");
Console.WriteLine();

using var cts = new CancellationTokenSource();

Console.CancelKeyPress += (_, e) =>
{
    if (cts.IsCancellationRequested)
    {
        return;
    }

    e.Cancel = true;
    cts.Cancel();

    Console.WriteLine();
    Console.WriteLine("Stopping... (press Ctrl+C again to quit now)");
};

using var client = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
var runner = new LoadRunner(client);

Console.WriteLine("Running... (Ctrl+C to stop)");
var run = await runner.RunAsync(options, cts.Token);

var stats = Statistics.Calculate(run.Results, run.TotalTime);
var failures = Statistics.CountFailures(run.Results);

Console.WriteLine();

if (run.WasCancelled)
{
    Console.WriteLine("Stopped early. These numbers are for the requests that finished.");
}

Console.WriteLine($"Requests:     {stats.Total}   Succeeded: {stats.Succeeded}   Failed: {stats.Failed}");
Console.WriteLine($"Total time:   {stats.TotalSeconds:F2} s   Throughput: {stats.Throughput:F1} req/s");
Console.WriteLine($"Latency (ms): min {stats.MinMs:F0}  mean {stats.MeanMs:F0}  p50 {stats.P50Ms:F0}  p90 {stats.P90Ms:F0}  p95 {stats.P95Ms:F0}  p99 {stats.P99Ms:F0}  max {stats.MaxMs:F0}");

if (failures.Count > 0)
{
    Console.WriteLine("Failures:");

    foreach (var (reason, count) in failures)
    {
        Console.WriteLine($"  {reason}: {count}");
    }
}

return 0;