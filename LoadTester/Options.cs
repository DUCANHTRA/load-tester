namespace LoadTester;

public record Options(
    Uri Url, // type for web address Uri
    int Requests = 100, // total number of requests
    int Concurrency = 10, // how many run at the same time
    int TimeoutSeconds = 10); // how long waiting for 1 req before give up