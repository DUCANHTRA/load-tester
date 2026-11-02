namespace LoadTester;
public static class Statistics
{
    private static readonly double[] BucketEdges = [5, 10, 20, 50, 100, 200, 500, 1000, 2000];
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

    public static IReadOnlyList<HistogramBucket> Histogram(IReadOnlyList<RequestResult> results)
    {
        var latencies = results
            .Where(r => r.IsSuccess)
            .Select(r => r.LatencyMs)
            .ToList();

        if (latencies.Count ==0)
        {
            return [];
        }

        var counts = new int[BucketEdges.Length + 1];

        foreach (var ms in latencies)
        {
            var index = 0;
            
            while (index < BucketEdges.Length && ms >= BucketEdges[index])
            { 
                index++;
            }
            counts[index]++;
        }

        var first = Array.FindIndex(counts, count => count > 0);
        var last = Array.FindLastIndex(counts, count => count > 0);

        var buckets = new List<HistogramBucket>();

        for (var i = first; i <= last; i++)
        {
            buckets.Add(new HistogramBucket(BucketLabel(i), counts[i]));
        }

        return buckets;
    }

    private static string BucketLabel(int index)
    {
        if (index == 0)
        {
            return $"under {BucketEdges[0]} ms";
        }
        if (index == BucketEdges.Length)
        {
            return $"{BucketEdges[^1]} ms and over";
        }

        return $"{BucketEdges[index - 1]}-{BucketEdges[index]} ms";
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