namespace SmartX.Api.Logic;

/// <summary>
/// The clock for the mesh router. Every few seconds it asks the
/// <see cref="ISmartXTopologyEngine"/> to check whether any node has dropped off
/// or come back, and to recompute the routes if so.
/// <para>
/// Without it, a route change would only be noticed when someone next opened
/// the topology page, and the telemetry history would date it to then rather
/// than to when it happened. The logic itself lives in the engine; this class
/// only owns the timer.
/// </para>
/// </summary>
public class TopologyMonitor : BackgroundService
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(3);

    private readonly ISmartXTopologyEngine _engine;
    private readonly ILogger<TopologyMonitor> _logger;

    public TopologyMonitor(ISmartXTopologyEngine engine, ILogger<TopologyMonitor> logger)
    {
        _engine = engine;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Topology monitor running; checking mesh health every {Seconds}s.",
            TickInterval.TotalSeconds);

        using var timer = new PeriodicTimer(TickInterval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                _engine.Refresh();
            }
            catch (Exception ex)
            {
                // A bad tick must not take the host down — the next one retries.
                _logger.LogError(ex, "Topology refresh failed.");
            }
        }
    }
}
