namespace LoadTester;

public record RunStatistics(
    int Total,
    int Succeeded,
    int Failed,
    double TotalSeconds,
    double Throughput,
    double MinMs,
    double MeanMs,
    double P50Ms,
    double P90Ms,
    double P95Ms,
    double P99Ms,
    double MaxMs);