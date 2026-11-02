namespace LoadTester;

public static class ReportPrinter
{
    public static void Print(TextWriter writer, Report report)
    {
        var options = report.Options;
        var stats = report.Stats;

        // Header
        writer.WriteLine($"Target:      {options.Url}");
        writer.WriteLine($"Settings:    {options.Requests} requests, {options.Concurrency} at a time, {options.TimeoutSeconds} s timeout");
        writer.WriteLine();

        if (report.WasCancelled)
        {
            writer.WriteLine("Stopped early (Ctrl+C). These numbers are for the requests that finished.");
            writer.WriteLine();
        }

        // Totals
        var failedPercent = stats.Total == 0 ? 0 : 100.0 * stats.Failed / stats.Total;

        writer.WriteLine($"Requests:    {stats.Total}   Succeeded: {stats.Succeeded}   Failed: {stats.Failed} ({failedPercent:F1}%)");
        writer.WriteLine($"Total time:  {stats.TotalSeconds:F2} s   Throughput: {stats.Throughput:F1} req/s");
        writer.WriteLine();

        // Latency
        if (stats.Succeeded == 0)
        {
            writer.WriteLine("Latency:     no successful requests");
        }
        else
        {
            writer.WriteLine(
                $"Latency (ms) min {Ms(stats.MinMs)}   mean {Ms(stats.MeanMs)}   p50 {Ms(stats.P50Ms)}   " +
                $"p90 {Ms(stats.P90Ms)}   p95 {Ms(stats.P95Ms)}   p99 {Ms(stats.P99Ms)}   max {Ms(stats.MaxMs)}");
        }

        writer.WriteLine();

        // Status codes
        if (report.StatusCodes.Count > 0)
        {
            writer.WriteLine("Status codes");

            foreach (var (code, count) in report.StatusCodes.OrderBy(pair => pair.Key))
            {
                writer.WriteLine($"  {code}  x {count}");
            }

            writer.WriteLine();
        }

        // Errors
        if (report.Errors.Count > 0)
        {
            writer.WriteLine("Errors");

            foreach (var (error, count) in report.Errors.OrderByDescending(pair => pair.Value))
            {
                writer.WriteLine($"  {error}  x {count}");
            }

            writer.WriteLine();
        }

        // Histogram
        if (report.Histogram.Count > 0)
        {
            var biggest = report.Histogram.Max(b => b.Count);

            writer.WriteLine("Latency spread");

            foreach (var bucket in report.Histogram)
            {
                var length = bucket.Count == 0
                    ? 0
                    : Math.Max(1, (int)Math.Round(bucket.Count * 30.0 / biggest));

                writer.WriteLine($"  {bucket.Label,-18} {new string('#', length),-30} {bucket.Count}");
            }

            writer.WriteLine();
        }
    }

    private static string Ms(double value)
    {
        return value < 10 ? value.ToString("F1") : value.ToString("F0");
    }
}