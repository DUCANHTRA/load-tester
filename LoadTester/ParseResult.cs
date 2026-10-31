namespace LoadTester;

public record ParseResult(Options? Options, string? Error)
{
    public bool IsSuccess => Options is not null;
}