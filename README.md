# loadtest

A command-line load tester for HTTP APIs. It sends N requests to a URL, a few at a time, and reports latency percentiles, throughput, status codes and errors.

I built it in C# and .NET to learn `async`/`await`, `Task`, `HttpClient`, cancellation, and testing with a real local server.

## Example

~~~bash
loadtest https://example.com -n 500 -c 20
~~~

~~~text
Running... (Ctrl+C to stop)
Target:      https://example.com/
Settings:    500 requests, 20 at a time, 10 s timeout

Requests:    500   Succeeded: 500   Failed: 0 (0.0%)
Total time:  1.27 s   Throughput: 392.6 req/s

Latency (ms) min 15   mean 47   p50 23   p90 95   p95 283   p99 305   max 310

Status codes
  200  x 500

Latency spread
  10-20 ms           #########                      96
  20-50 ms           ############################## 325
  50-100 ms          ####                           38
  100-200 ms         #                              13
  200-500 ms         ###                            28
~~~

## Usage

~~~text
loadtest <url> [-n requests] [-c concurrency] [-t timeout-seconds] [--json]
~~~

| Option | Meaning | Default |
| --- | --- | --- |
| `-n` | Total requests to send | 100 |
| `-c` | Requests in flight at the same time | 10 |
| `-t` | Timeout for one request, in seconds | 10 |
| `--json` | Print the report as JSON. Progress messages go to stderr, so stdout is clean JSON. | off |

Press Ctrl+C to stop early. You still get a report for the requests that finished.

## Build from source

You need the .NET 10 SDK.

~~~bash
git clone https://github.com/YOUR-USERNAME/load-tester.git
cd load-tester
dotnet run --project LoadTester -- https://localhost:5116/api/health/database -n 20
~~~

## Run the tests

~~~bash
dotnet test
~~~

Some tests start a small web server on a random local port and send real requests to it.

## Responsible use

Only test servers that you own, or that you have permission to test.
