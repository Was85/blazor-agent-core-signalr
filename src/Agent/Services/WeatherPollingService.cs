using System.Collections.Concurrent;
using Contracts;
using Microsoft.AspNetCore.SignalR.Client;

namespace Agent.Services;

public class WeatherPollingService(ILogger<WeatherPollingService> logger, IConfiguration config, AgentStatus status) : BackgroundService
{
    private readonly string _hubUrl = config["CoreHubUrl"] ?? "http://localhost:5100/hubs/resources";
    private readonly string _agentId = config["Agent:AgentId"] ?? $"agent-{Guid.NewGuid():N}";
    private readonly string _agentName = config["Agent:Name"] ?? "Weather Agent";
    private HubConnection? _connection;

    private readonly List<(string Id, string Name)> _cities =
    [
        ("nyc", "New York"),
        ("lon", "London"),
        ("tky", "Tokyo")
    ];

    private readonly ConcurrentDictionary<string, double> _lastTemps = new();
    private readonly Random _random = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await StartConnection(stoppingToken);

        // Register agent and resources on start and whenever reconnected
        async Task RegisterAll()
        {
            if (_connection is null) return;
            var now = DateTimeOffset.UtcNow;
            await _connection.InvokeAsync("RegisterAgent", new AgentInfo(_agentId, _agentName, now), cancellationToken: stoppingToken);
            await _connection.InvokeAsync("RegisterResources",
                _cities.Select(c => new CityRegistration(c.Id, c.Name)),
                _agentId,
                cancellationToken: stoppingToken);
        }

        _connection!.Reconnected += async _ =>
        {
            status.SetState("Reconnected", "Re-registering...");
            await RegisterAll();
        };

        await RegisterAll();

        // Initialize temps
        foreach (var city in _cities)
        {
            _lastTemps.TryAdd(city.Id, BaseTempFor(city.Id));
        }

        var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            foreach (var city in _cities)
            {
                var newTemp = Drift(_lastTemps[city.Id]);
                if (Math.Abs(newTemp - _lastTemps[city.Id]) >= 0.1)
                {
                    _lastTemps[city.Id] = newTemp;
                    await PushUpdate(city.Id, newTemp, stoppingToken);
                }
            }
        }
    }

    private async Task StartConnection(CancellationToken ct)
    {
        _connection = new HubConnectionBuilder()
            .WithUrl(_hubUrl)
            .WithAutomaticReconnect()
            .Build();

        _connection.Closed += async (ex) =>
        {
            status.SetState("Disconnected", ex?.Message);
            await Task.Delay(TimeSpan.FromSeconds(3), CancellationToken.None);
            try
            {
                await _connection.StartAsync(CancellationToken.None);
                status.SetState("Connected", "Reconnected after close");
            }
            catch (Exception startEx)
            {
                status.SetState("Disconnected", $"Reconnect failed: {startEx.Message}");
            }
        };

        await _connection.StartAsync(ct);
        status.SetState("Connected", $"Connected to {_hubUrl}");
    }

    private async Task PushUpdate(string cityId, double tempC, CancellationToken ct)
    {
        if (_connection is null) return;
        var update = new WeatherUpdate(cityId, _agentId, tempC, DateTimeOffset.UtcNow);
        try
        {
            await _connection.InvokeAsync("PushWeatherUpdate", update, ct);
            status.SetState("Connected", $"Pushed {cityId}:{tempC:0.0}°C");
        }
        catch (Exception ex)
        {
            status.SetState("Error", ex.Message);
        }
    }

    private double BaseTempFor(string cityId) => cityId switch
    {
        "nyc" => 22,
        "lon" => 18,
        "tky" => 25,
        _ => 20
    };

    private double Drift(double val)
    {
        // small random drift
        var delta = (_random.NextDouble() - 0.5) * 0.8; // -0.4..+0.4
        return Math.Round(val + delta, 1);
    }
}