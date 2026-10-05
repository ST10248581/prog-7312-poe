using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;

namespace SmartX.Api.Logic;

/// <summary>
/// Emulated ESP32 nodes, smart plugs and gateways. Every two seconds it asks the
/// engine what each device would report, then sends it the way a device does:
/// an HTTP <c>POST /api/commands/packets</c> per device, carrying JSON.
/// <para>
/// The engine never sees these packets until they arrive through that endpoint,
/// so the intake is exercised end to end — model binding, the registry lookup by
/// MAC address, lane classification, the critical lane draining inside the
/// request — exactly as it would be for real hardware.
/// </para>
/// </summary>
public class DeviceTelemetrySimulator : BackgroundService
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(2);
    private const string IntakePath = "api/commands/packets";

    /// <summary>Matches the API's own wire format: camelCase, enums by name.</summary>
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly ISmartXCommandEngine _engine;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IServer _server;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DeviceTelemetrySimulator> _logger;

    public DeviceTelemetrySimulator(
        ISmartXCommandEngine engine,
        IHttpClientFactory httpClientFactory,
        IServer server,
        IHostApplicationLifetime lifetime,
        IConfiguration configuration,
        ILogger<DeviceTelemetrySimulator> logger)
    {
        _engine = engine;
        _httpClientFactory = httpClientFactory;
        _server = server;
        _lifetime = lifetime;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_configuration.GetValue("DeviceSimulator:Enabled", true))
        {
            _logger.LogInformation("Device simulator disabled by configuration.");
            return;
        }

        // The server's addresses are only known once it is listening.
        if (!await WaitForStartAsync(stoppingToken))
        {
            return;
        }

        var baseAddress = ResolveBaseAddress();
        if (baseAddress is null)
        {
            _logger.LogWarning("Device simulator could not work out the API address; set DeviceSimulator:BaseUrl.");
            return;
        }

        var client = _httpClientFactory.CreateClient(nameof(DeviceTelemetrySimulator));
        client.BaseAddress = baseAddress;
        client.Timeout = TimeSpan.FromSeconds(5);

        _logger.LogInformation(
            "Device simulator posting to {Url} every {Seconds}s.",
            new Uri(baseAddress, IntakePath), TickInterval.TotalSeconds);

        using var timer = new PeriodicTimer(TickInterval);
        var failing = false;

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                var transmissions = _engine.ComposeDeviceTransmissions();

                // Devices transmit independently, so their requests go out together.
                await Task.WhenAll(transmissions.Select(transmission =>
                    SendAsync(client, transmission.Sender, transmission.Packets, stoppingToken)));

                if (failing)
                {
                    _logger.LogInformation("Device simulator reached the intake again.");
                    failing = false;
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                // Logged once per outage rather than every two seconds.
                if (!failing)
                {
                    _logger.LogWarning(ex, "Device simulator could not post telemetry; retrying each tick.");
                    failing = true;
                }
            }
        }
    }

    private static async Task SendAsync(
        HttpClient client,
        string sender,
        List<Models.Requests.StreamPacketRequest> packets,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, IntakePath)
        {
            Content = JsonContent.Create(packets, options: JsonOptions)
        };
        request.Headers.Add("X-Device-Id", sender);

        using var response = await client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private async Task<bool> WaitForStartAsync(CancellationToken stoppingToken)
    {
        var started = new TaskCompletionSource();
        using var onStarted = _lifetime.ApplicationStarted.Register(() => started.TrySetResult());
        using var onStopping = stoppingToken.Register(() => started.TrySetCanceled());

        try
        {
            await started.Task;
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    /// <summary>
    /// The configured base URL if there is one, otherwise the address the server
    /// is listening on, with any wildcard host turned into localhost. HTTP is
    /// preferred so a development certificate never gets in the way.
    /// </summary>
    private Uri? ResolveBaseAddress()
    {
        var configured = _configuration["DeviceSimulator:BaseUrl"];
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return new Uri(configured.TrimEnd('/') + "/");
        }

        var addresses = _server.Features.Get<IServerAddressesFeature>()?.Addresses ?? [];
        var address = addresses.FirstOrDefault(a => a.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
                      ?? addresses.FirstOrDefault();

        if (address is null)
        {
            return null;
        }

        var normalised = address
            .Replace("://*", "://localhost")
            .Replace("://+", "://localhost")
            .Replace("://[::]", "://localhost")
            .Replace("://0.0.0.0", "://localhost");

        return new Uri(normalised.TrimEnd('/') + "/");
    }
}
