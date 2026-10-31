namespace LoadTester;

public static class Usage
{
    public const string Text = """
        Usage:
          loadtest <url> [-n requests] [-c concurrency] [-t timeout-seconds]

        Options:
          -n   total requests to send            (default 100)
          -c   requests at the same time         (default 10)
          -t   timeout for one request, seconds  (default 10)

        Example:
          loadtest https://localhost:5116/api/health/database -n 500 -c 20
        """;
}