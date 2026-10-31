namespace LoadTester;

public record RequestResult(double LatencyMs, int? StatusCode, string? Error)
{
    public bool IsSuccess => Error is null && StatusCode is >= 200 and < 300;
}