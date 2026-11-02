namespace LoadTester.Tests;

public class StatisticsTests
{
    private static readonly double[] Five = [10, 20, 30, 40, 50];
    private static readonly double[] Ten = [10, 20, 30, 40, 50, 60, 70, 80, 90, 100];

    private static RequestResult Ok(double ms) => new(ms, 200, null);
    private static RequestResult Fail(double ms) => new(ms, 500, null);

    // ---- Percentile ----

    [Theory]
    [InlineData(50, 30)]
    [InlineData(90, 50)]
    [InlineData(20, 10)]
    [InlineData(100, 50)]
    [InlineData(0, 10)]
    public void Percentile_of_five_values(double p, double expected)
    {
        Assert.Equal(expected, Statistics.Percentile(Five, p));
    }

    [Theory]
    [InlineData(50, 50)]
    [InlineData(90, 90)]
    [InlineData(95, 100)]
    public void Percentile_of_ten_values(double p, double expected)
    {
        Assert.Equal(expected, Statistics.Percentile(Ten, p));
    }

    [Fact]
    public void Percentile_of_empty_list_is_zero()
    {
        Assert.Equal(0, Statistics.Percentile([], 95));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(50)]
    [InlineData(99)]
    [InlineData(100)]
    public void Percentile_of_one_value_is_that_value(double p)
    {
        Assert.Equal(42, Statistics.Percentile([42], p));
    }

    [Fact]
    public void Percentile_of_equal_values_is_that_value()
    {
        Assert.Equal(7, Statistics.Percentile([7, 7, 7, 7], 90));
    }

    // ---- Calculate ----

    [Fact]
    public void Calculate_gives_min_mean_max_and_throughput()
    {
        var results = new[] { Ok(10), Ok(20), Ok(30), Ok(40), Ok(50) };

        var stats = Statistics.Calculate(results, TimeSpan.FromSeconds(2));

        Assert.Equal(5, stats.Total);
        Assert.Equal(5, stats.Succeeded);
        Assert.Equal(0, stats.Failed);
        Assert.Equal(10, stats.MinMs);
        Assert.Equal(30, stats.MeanMs);
        Assert.Equal(50, stats.MaxMs);
        Assert.Equal(2.5, stats.Throughput, 3);
    }

    [Fact]
    public void Calculate_sorts_unsorted_input()
    {
        var results = new[] { Ok(50), Ok(10), Ok(30) };

        var stats = Statistics.Calculate(results, TimeSpan.FromSeconds(1));

        Assert.Equal(10, stats.MinMs);
        Assert.Equal(30, stats.P50Ms);
        Assert.Equal(50, stats.MaxMs);
    }

    [Fact]
    public void Calculate_leaves_failed_requests_out_of_latency()
    {
        var results = new[] { Ok(10), Ok(20), Fail(1000) };

        var stats = Statistics.Calculate(results, TimeSpan.FromSeconds(1));

        Assert.Equal(3, stats.Total);
        Assert.Equal(2, stats.Succeeded);
        Assert.Equal(1, stats.Failed);
        Assert.Equal(20, stats.MaxMs);
    }

    [Fact]
    public void Calculate_with_no_successes_does_not_crash()
    {
        var results = new[] { Fail(5), Fail(6) };

        var stats = Statistics.Calculate(results, TimeSpan.FromSeconds(1));

        Assert.Equal(0, stats.Succeeded);
        Assert.Equal(2, stats.Failed);
        Assert.Equal(0, stats.MaxMs);
        Assert.Equal(0, stats.Throughput);
    }

    [Fact]
    public void Calculate_with_empty_results_does_not_crash()
    {
        var stats = Statistics.Calculate([], TimeSpan.FromSeconds(1));

        Assert.Equal(0, stats.Total);
        Assert.Equal(0, stats.MaxMs);
    }

    [Fact]
    public void Calculate_with_zero_total_time_has_zero_throughput()
    {
        var results = new[] { Ok(10) };

        var stats = Statistics.Calculate(results, TimeSpan.Zero);

        Assert.Equal(0, stats.Throughput);
    }

    [Fact]
    public void CountFailures_groups_by_reason()
    {
        var results = new[]
        {
            Ok(10),
            new RequestResult(5, 500, null),
            new RequestResult(5, 500, null),
            new RequestResult(5, 404, null),
            new RequestResult(5, null, "Timeout")
        };

        var failures = Statistics.CountFailures(results);

        Assert.Equal(3, failures.Count);
        Assert.Equal(2, failures["HTTP 500"]);
        Assert.Equal(1, failures["HTTP 404"]);
        Assert.Equal(1, failures["Timeout"]);
    }

    [Fact]
    public void CountFailures_is_empty_when_everything_succeeds()
    {
        var failures = Statistics.CountFailures([Ok(1), Ok(2)]);

        Assert.Empty(failures);
    }
}