using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LoadTester.Tests;

public sealed class LocalTestServer : IAsyncDisposable
{
    private readonly WebApplication _app;
    private readonly object _lock = new();
    private int _inFlight;
    private int _maxInFlight;
    private int _totalRequests;

    public Uri BaseUrl { get; private set; } = null!;

    public int TotalRequests
    {
        get { lock (_lock) { return _totalRequests; } }
    }

    public int MaxInFlight
    {
        get { lock (_lock) { return _maxInFlight; } }
    }

    private LocalTestServer()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();

        _app = builder.Build();

        _app.Use(async (context, next) =>
        {
            EnterRequest();

            try
            {
                await next();
            }
            finally
            {
                ExitRequest();
            }
        });
    }

    public static async Task<LocalTestServer> StartAsync()
    {
        var server = new LocalTestServer();

        await server._app.StartAsync();

        server.BaseUrl = new Uri(server._app.Urls.First());

        return server;
    }

    public Uri Url(string pathAndQuery)
    {
        return new Uri(BaseUrl, pathAndQuery);
    }

    public async ValueTask DisposeAsync()
    {
        await _app.StopAsync();
        await _app.DisposeAsync();
    }

    private void EnterRequest()
    {
        lock (_lock)
        {
            _totalRequests++;
            _inFlight++;

            if (_inFlight > _maxInFlight)
            {
                _maxInFlight = _inFlight;
            }
        }
    }

    private void ExitRequest()
    {
        lock (_lock)
        {
            _inFlight--;
        }
    }
}