using System.Text.Json;

namespace LoadTester;

public static class JsonReportWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static void Write(TextWriter writer, Report report)
    {
        writer.WriteLine(JsonSerializer.Serialize(report, JsonOptions));
    }
}
