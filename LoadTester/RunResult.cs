namespace LoadTester;

public record RunResult(IReadOnlyList<RequestResult> Results, TimeSpan TotalTime);