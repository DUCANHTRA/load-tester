namespace LoadTester;

public record RequestResult(double LatencyMs, int? StatusCode, string? Error);