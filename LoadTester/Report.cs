namespace LoadTester;

public record Report(
    Options Options,
    bool WasCancelled,
    RunStatistics Stats,
    IReadOnlyDictionary<int, int> StatusCodes,
    IReadOnlyDictionary<string, int> Errors,
    IReadOnlyList<HistogramBucket> Histogram)
{
    public static Report Create(Options options, RunResult run)
    {
        return new Report(
            options,
            run.WasCancelled,
            Statistics.Calculate(run.Results, run.TotalTime),
            Statistics.CountStatusCodes(run.Results),
            Statistics.CountErrors(run.Results),
            Statistics.Histogram(run.Results));
    }
}