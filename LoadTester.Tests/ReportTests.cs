using System.Text.Json;

namespace LoadTester.Tests;

public class ReportTests
{
    private static Report ReportFor(bool cancelled, params RequestResult[] results)
    {
        var run = new RunResult(results, TimeSpan.FromSeconds(2), cancelled);
        var options = new Options(new Uri("https://test.local/"), Requests: results.Length, Concurrency: 1, TimeoutSeconds: 5);

        return Report.Create(options, run);
    }

    private static string TextOf(Report report)
    {
        var writer = new StringWriter();
        ReportPrinter.Print(writer, report);
        return writer.ToString();
    }

    [Fact]
    public void Text_report_shows_totals_and_status_codes()
    {
        var report = ReportFor(false,
            new RequestResult(10, 200, null),
            new RequestResult(20, 200, null),
            new RequestResult(30, 500, null));

        var text = TextOf(report);

        Assert.Contains("Succeeded: 2", text);
        Assert.Contains("Failed: 1", text);
        Assert.Contains("200  x 2", text);
        Assert.Contains("500  x 1", text);
        Assert.Contains("Latency (ms)", text);
    }

    [Fact]
    public void Text_report_lists_errors()
    {
        var report = ReportFor(false,
            new RequestResult(5, null, "Timeout"),
            new RequestResult(5, null, "Timeout"));

        var text = TextOf(report);

        Assert.Contains("Timeout  x 2", text);
    }

    [Fact]
    public void Text_report_without_successes_says_so_instead_of_printing_zeros()
    {
        var report = ReportFor(false, new RequestResult(5, null, "Timeout"));

        var text = TextOf(report);

        Assert.Contains("no successful requests", text);
        Assert.DoesNotContain("Latency (ms)", text);
    }

    [Fact]
    public void Text_report_says_when_the_run_was_stopped_early()
    {
        var report = ReportFor(true, new RequestResult(10, 200, null));

        Assert.Contains("Stopped early", TextOf(report));
    }

    [Fact]
    public void Json_report_is_valid_and_has_the_numbers()
    {
        var report = ReportFor(false,
            new RequestResult(10, 200, null),
            new RequestResult(20, 200, null),
            new RequestResult(30, 500, null));

        var writer = new StringWriter();
        JsonReportWriter.Write(writer, report);

        using var doc = JsonDocument.Parse(writer.ToString());
        var stats = doc.RootElement.GetProperty("stats");

        Assert.Equal(3, stats.GetProperty("total").GetInt32());
        Assert.Equal(2, stats.GetProperty("succeeded").GetInt32());
        Assert.Equal(1, doc.RootElement.GetProperty("statusCodes").GetProperty("500").GetInt32());
    }
}