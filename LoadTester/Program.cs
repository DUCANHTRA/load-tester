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

using var client = new HttpClient();
var runner = new LoadRunner(client);

// Run many request
Console.WriteLine("Running...");
var run = await runner.RunSemaphoreAsync(options);

var latencies = run.Results.Select(r => r.LatencyMs).ToList();

Console.WriteLine();
Console.WriteLine($"Requests sent:       {run.Results.Count}");
Console.WriteLine($"Total time:          {run.TotalTime.TotalSeconds:F2} s");
Console.WriteLine($"Average latency:     {latencies.Average():F0} ms");
Console.WriteLine($"Sum of latencies:    {latencies.Sum() / 1000:F2} s");

return 0;