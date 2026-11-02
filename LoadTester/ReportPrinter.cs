using Microsoft.VisualBasic;

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

        var stats = Statistics.Calculate(run.Results, run.TotalTime);
        var failedPercent = stats.Total == 0 ? 0 : 100.0 * stats.Failed / stats.Total;

        writer.WriteLine($"Requests:    {stats.Total}   Succeeded: {stats.Succeeded}   Failed: {stats.Failed} ({failedPercent:F1}%)");
        writer.WriteLine($"Total time:  {stats.TotalSeconds:F2} s   Throughput: {stats.Throughput:F1} req/s");
        writer.WriteLine();

        if (stats.Succeeded == 0)
        {
            writer.WriteLine("Latency:      no successful requests");
        }
        else
        {
            writer.WriteLine(
                $"Latency (ms) min {Ms(stats.MinMs)}   mean {Ms(stats.MeanMs)}   p50 {Ms(stats.P50Ms)}   " +
                $"p90 {Ms(stats.P90Ms)}   p95 {Ms(stats.P95Ms)}   p99 {Ms(stats.P99Ms)}   max {Ms(stats.MaxMs)}");
        }
        writer.WriteLine();

        var statusCodes = Statistics.CountStatusCodes(run.Results);
        if (statusCodes.Count > 0)
        {
            writer.WriteLine("Status codes");

            foreach (var (code, count) in statusCodes.OrderBy(pair => pair.Key))
            {
                writer.WriteLine($"  {code}  x {count}");
            }
            
            writer.WriteLine();
        }

        var errors = Statistics.CountErrors(run.Results);
        if (errors.Count > 0)
        {
            writer.WriteLine("Errors");

            foreach (var (error, count) in errors.OrderByDescending(pair => pair.Value))
            {
                writer.WriteLine($"  {error}  x {count}");
            }

            writer.WriteLine();
        }

        var histogram = Statistics.Histogram(run.Results);
        if (histogram.Count > 0)
        {
            var biggest = histogram.Max(bucket => bucket.Count);
            writer.WriteLine("Latency spread");

            foreach (var bucket in histogram)
            {
                // Scale each bar so the largest bucket uses 30 # characters.
                var length = bucket.Count == 0
                    ? 0
                    // Keep non-empty buckets visible with at least one #.
                    : Math.Max(1, (int)Math.Round(bucket.Count * 30.0 / biggest));

                // Pad the label and bar so all counts line up in columns.
                writer.WriteLine(
                    $"  {bucket.Label,-18} {new string('#', length),-30} {bucket.Count}");
            }

            writer.WriteLine();
        }
    }

    private static string Ms(double value)
    {
        return value < 10 ? value.ToString("F1") : value.ToString("F0");
    }
}