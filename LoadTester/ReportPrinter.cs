namespace LoadTester;

public static class ReportPrinter
{
    public static void Print(TextWriter writer, Options options, RunResult run)
    {
        writer.WriteLine($"Target:      {options.Url}");
        writer.WriteLine($"Settings:    {options.Requests} requests, {options.Concurrency} at a time, {options.TimeoutSeconds} s timeout");
        writer.WriteLine();

        if (run.WasCancelled)
        {
            writer.WriteLine("Stopped early (Ctrl+C). These numbers are for the requests that finished.");
            writer.WriteLine();
        }
    }
}