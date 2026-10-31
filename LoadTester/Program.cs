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

using var client = new HttpClient();
var runner = new LoadRunner(client);

Console.WriteLine("Running...");
var run = await runner.RunAsync(options);

var latencies = run.Results.Select(r => r.LatencyMs).ToList();
var sumSeconds = latencies.Sum() / 1000;
var totalSeconds = run.TotalTime.TotalSeconds;

Console.WriteLine();
Console.WriteLine($"Requests sent:          {run.Results.Count}");
Console.WriteLine($"Total time:             {totalSeconds:F2} s");
Console.WriteLine($"Average latency:        {latencies.Average():F0} ms");
Console.WriteLine($"Sum of latencies:       {sumSeconds:F2} s");
Console.WriteLine($"Average in flight:      {sumSeconds / totalSeconds:F1}");

return 0;