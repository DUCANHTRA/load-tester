namespace LoadTester;

public static class ArgumentParser
{
    public static ParseResult Parse(string[] args)
    {
        if (args.Length == 0)
            return Fail("Missing URL.");

        if (!Uri.TryCreate(args[0], UriKind.Absolute, out var url)
            || (url.Scheme != Uri.UriSchemeHttp && url.Scheme != Uri.UriSchemeHttps))
            return Fail($"'{args[0]}' is not a valid http or https URL.");

        var requests = 100;
        var concurrency = 10;
        var timeoutSeconds = 10;

        for (var i = 1; i < args.Length; i += 2)
        {
            var flag = args[i];

            if (i + 1 >= args.Length)
                return Fail($"Missing value after {flag}.");

            if (!int.TryParse(args[i + 1], out var value) || value < 1)
                return Fail($"{flag} needs a whole number of 1 or more, but got '{args[i + 1]}'.");

            switch (flag)
            {
                case "-n": requests = value; break;
                case "-c": concurrency = value; break;
                case "-t": timeoutSeconds = value; break;
                default: return Fail($"Unknown option '{flag}'.");
            }
        }

        if (concurrency > requests)
            concurrency = requests;

        return new ParseResult(new Options(url, requests, concurrency, timeoutSeconds), null);
    }

    private static ParseResult Fail(string error) => new(null, error);
}