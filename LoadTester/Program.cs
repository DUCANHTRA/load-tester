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

using var client = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
var runner = new LoadRunner(client);

Console.WriteLine("Running...");
var run = await runner.RunAsync(options);

var stats = Statistics.Calculate(run.Results, run.TotalTime);

Console.WriteLine();
Console.WriteLine($"Requests:     {stats.Total}   Succeeded: {stats.Succeeded}   Failed: {stats.Failed}");
Console.WriteLine($"Total time:   {stats.TotalSeconds:F2} s   Throughput: {stats.Throughput:F1} req/s");
Console.WriteLine($"Latency (ms): min {stats.MinMs:F0}  mean {stats.MeanMs:F0}  p50 {stats.P50Ms:F0}  p90 {stats.P90Ms:F0}  p95 {stats.P95Ms:F0}  p99 {stats.P99Ms:F0}  max {stats.MaxMs:F0}");

var failures = Statistics.CountFailures(run.Results);
if (failures.Count > 0)
{
    Console.WriteLine("Failures");
    foreach (var (reason, count) in failures)
    {
        Console.WriteLine($"  {reason}: {count}");
    }
}
return 0;