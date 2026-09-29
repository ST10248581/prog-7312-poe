namespace SmartX.Api.Logic;

/// <summary>
/// The clock for the command stream. Every two seconds it asks the
/// <see cref="ISmartXCommandEngine"/> to run one dispatch cycle — advance
/// in-flight commands, issue automated traffic, take in gateway packets and
/// drain the intake queues.
/// <para>
/// This is what makes the page genuinely real-time rather than a polled
/// snapshot of a frozen list. The logic itself lives in the engine; this class
/// only owns the timer.
/// </para>
/// </summary>
public class CommandDispatchSimulator : BackgroundService
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(2);

    private readonly ISmartXCommandEngine _engine;
    private readonly ILogger<CommandDispatchSimulator> _logger;

    public CommandDispatchSimulator(ISmartXCommandEngine engine, ILogger<CommandDispatchSimulator> logger)
    {
        _engine = engine;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Command dispatch simulator running; ticking every {Seconds}s.",
            TickInterval.TotalSeconds);

        using var timer = new PeriodicTimer(TickInterval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                _engine.RunDispatchCycle();
            }
            catch (Exception ex)
            {
                // A bad tick must not take the host down — the next one retries.
                _logger.LogError(ex, "Command dispatch tick failed.");
            }
        }
    }
}
