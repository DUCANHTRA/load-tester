# loadtest

![CI](https://github.com/DUCANHTRA/load-tester/actions/workflows/ci.yml/badge.svg)

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

## How it works

~~~text
args  --->  ArgumentParser  --->  Options
                                     |
                                     v
                               LoadRunner  --->  N requests, C at a time
                                     |
                                     v
                            list of RequestResult
                                     |
                                     v
                     Statistics  --->  Report  --->  text or JSON
~~~

**Concurrency.** `LoadRunner` uses `Parallel.ForEachAsync` with `MaxDegreeOfParallelism` set to the `-c` value. At most C requests are in flight. When one finishes, the next one starts. All requests share one `HttpClient`.

**No shared list.** Each request writes its result into its own slot in an array (`results[i]`). Two tasks never write to the same place, so no lock is needed.

**Latency.** The time from sending the request until the whole response body has been read, measured with `Stopwatch`.

**Percentiles.** Nearest-rank method. Sort the successful latencies, then rank = ceil(p x count / 100), and take the value at that rank.

**What counts as failed.** A request fails if it times out, has a network error, or gets a status code outside 200-299. Failures are counted and listed, but left out of the latency numbers.

**Timeouts and Ctrl+C.** Each request has its own timeout. Ctrl+C cancels a second token that every request watches, so the run stops and still prints a report.

**Tests.** Unit tests cover the math and the argument parser. Integration tests start a small local server that counts the requests it sees. They check that the tool sends exactly N requests and never more than C at a time.

## Limits

- **Measured from the client.** The numbers include your network and your own machine, not only the server. Run it on a quiet machine, and compare runs made in the same way.
- **Latency** is the time until the whole response body is read.
- **No warm-up.** The first requests are often slower (DNS, connection setup). They are included.
- **Closed loop.** The tool waits for answers before it uses a free slot. If the server gets very slow, the tool sends fewer requests, which can hide how bad it is (known as coordinated omission). A fixed-rate mode would fix this.
- **GET only.** No POST bodies, custom headers or login yet.
- **Percentiles need enough requests.** With fewer than 100 requests, p99 is just the maximum.
- This is a learning project. For serious load testing, use a tool like k6, wrk or bombardier.
