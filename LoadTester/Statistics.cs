namespace LoadTester;
public static class Statistics
{
   public static RunStatistics Calculate(IReadOnlyList<RequestResult> results, TimeSpan totalTime)
    {
        var sorted = results
            .Where(r => r.IsSuccess)
            .Select(r => r.LatencyMs)
            .OrderBy(ms => ms)
            .ToList();

        var totalSeconds = totalTime.TotalSeconds;
        var throughput = totalSeconds > 0 ? sorted.Count / totalSeconds : 0;

        if (sorted.Count == 0)
        {
            return new RunStatistics(
                Total: results.Count,
                Succeeded: 0,
                Failed: results.Count,
                TotalSeconds: totalSeconds,
                Throughput: 0,
                MinMs: 0, MeanMs: 0, P50Ms: 0, P90Ms: 0, P95Ms: 0, P99Ms: 0, MaxMs: 0);
        }

        return new RunStatistics(
            Total: results.Count,
            Succeeded: sorted.Count,
            Failed: results.Count - sorted.Count,
            TotalSeconds: totalSeconds,
            Throughput: throughput,
            MinMs: sorted[0],
            MeanMs: sorted.Average(),
            P50Ms: Percentile(sorted, 50),
            P90Ms: Percentile(sorted, 90),
            P95Ms: Percentile(sorted, 95),
            P99Ms: Percentile(sorted, 99),
            MaxMs: sorted[^1]);
    }   
    public static double Percentile(IReadOnlyList<double> sortedValues, double p)
    {
        if (sortedValues.Count == 0)
            return 0;

        var rank = (int)Math.Ceiling(p*sortedValues.Count / 100.0);
        var index = Math.Clamp(rank -1, 0, sortedValues.Count -1);

        return sortedValues[index];
    }

    public static IReadOnlyDictionary<string, int> CountErrors(IReadOnlyList<RequestResult> results)
    {
        return results
            .Where(r => r.Error is not null)
            .GroupBy(r => r.Error!)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    public static IReadOnlyDictionary<int, int> CountStatusCodes(IReadOnlyList<RequestResult> results)
    {
        return results
            .Where(r => r.StatusCode is not null)
            .GroupBy(r => r.StatusCode!.Value)
            .ToDictionary(g => g.Key, g => g.Count());
    }
}