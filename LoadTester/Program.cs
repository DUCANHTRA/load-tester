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

using var cts = new CancellationTokenSource();

Console.CancelKeyPress += (_, e) =>
{
    if (cts.IsCancellationRequested)
    {
        return;
    }

    e.Cancel = true;
    cts.Cancel();

    Console.Error.WriteLine();
    Console.Error.WriteLine("Stopping... (press Ctrl+C again to quit now)");
};

using var client = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
var runner = new LoadRunner(client);

Console.Error.WriteLine("Running... (Ctrl+C to stop)");
var run = await runner.RunAsync(options, cts.Token);

var report = Report.Create(options, run);

if (options.Json)
{
    JsonReportWriter.Write(Console.Out, report);
}
else
{
    ReportPrinter.Print(Console.Out, report);
}

return 0;