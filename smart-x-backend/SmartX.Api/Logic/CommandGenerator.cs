using SmartX.Api.Models;

namespace SmartX.Api.Logic;

/// <summary>
/// Builds command records and moves them through their lifecycle.
/// <para>
/// Shared by the seeder, which back-fills history and settles it immediately,
/// and by the dispatch simulator, which issues commands live and advances them
/// one tick at a time. Keeping both on the same rules is what makes the seeded
/// history and the live tail indistinguishable in the stream.
/// </para>
/// </summary>
public static class CommandGenerator
{
    /// <summary>Automation rule ids, so an automated command names what triggered it.</summary>
    private static readonly string[] AutomationSources =
    [
        "anomaly-rule-7", "drift-rule-2", "threshold-rule-4", "health-monitor", "mesh-balancer"
    ];

    private static readonly string[] ScheduleSources =
    [
        "nightly-calibration", "rollout-14", "hourly-sweep", "maintenance-window"
    ];

    private static readonly string[] Operators =
    [
        "operator", "t.kruger", "m.naidoo", "s.botha", "night-shift"
    ];

    /// <summary>
    /// Which commands make sense for which hardware. A motion sensor is never
    /// asked to recalibrate a power offset, so the stream reads as real traffic
    /// rather than a shuffle of every enum value.
    /// </summary>
    private static readonly Dictionary<SensorCategory, CommandType[]> CommandsByCategory = new()
    {
        [SensorCategory.Environmental] =
            [CommandType.SetThreshold, CommandType.Recalibrate, CommandType.RequestSample, CommandType.FirmwarePush],
        [SensorCategory.PowerConsumption] =
            [CommandType.SetThreshold, CommandType.Recalibrate, CommandType.RequestSample, CommandType.RestartNode],
        [SensorCategory.Actuator] =
            [CommandType.ToggleActuator, CommandType.RestartNode, CommandType.FirmwarePush],
        [SensorCategory.Motion] =
            [CommandType.RequestSample, CommandType.SetThreshold, CommandType.RestartNode],
        [SensorCategory.Connectivity] =
            [CommandType.RestartNode, CommandType.FirmwarePush, CommandType.RequestSample]
    };

    /// <summary>A newly issued command, still queued and not yet dispatched.</summary>
    public static DeviceCommand Compose(Random random, SensorProfile sensor, DateTime issuedUtc)
    {
        var origin = PickOrigin(random);
        var commandType = PickCommandType(random, sensor.Category);

        return new DeviceCommand
        {
            Id = Guid.NewGuid(),
            SensorProfileId = sensor.Id,
            NodeId = sensor.NodeId,
            SensorName = sensor.Name,
            Zone = sensor.Zone,
            CommandType = commandType,
            Parameters = BuildParameters(random, commandType, sensor),
            Origin = origin,
            Priority = PickPriority(random, origin),
            Status = CommandStatus.Queued,
            IssuedUtc = issuedUtc,
            IssuedBy = PickIssuer(random, origin),
            Retries = 0,
            IsDryRun = false
        };
    }

    /// <summary>
    /// Runs a command all the way to a terminal state in one step. Used for
    /// back-filled history, where nothing should still be sitting in flight.
    /// </summary>
    public static void Settle(Random random, DeviceCommand command, DateTime now)
    {
        command.DispatchedUtc = command.IssuedUtc.AddMilliseconds(random.Next(40, 900));

        // An offline node is where commands go to expire; everything else is
        // overwhelmingly acknowledged, with the odd failure.
        var roll = random.NextDouble();

        if (roll < 0.045)
        {
            command.Status = CommandStatus.Failed;
            command.Retries = random.Next(1, 4);
            return;
        }

        if (roll < 0.065)
        {
            command.Status = CommandStatus.Expired;
            command.Retries = 3;
            return;
        }

        command.Status = CommandStatus.Acknowledged;
        command.Retries = random.NextDouble() < 0.12 ? 1 : 0;
        command.RoundTripMs = RoundTrip(random, command);
        command.AcknowledgedUtc = command.DispatchedUtc!.Value.AddMilliseconds(command.RoundTripMs!.Value);

        // A command that takes an age to come back is exactly the kind of thing
        // an operator should be able to see settling in the stream.
        if (now < command.AcknowledgedUtc)
        {
            command.Status = CommandStatus.Sent;
            command.AcknowledgedUtc = null;
            command.RoundTripMs = null;
        }
    }

    /// <summary>
    /// One tick of the lifecycle for a command that is still in flight: queued
    /// commands get dispatched, dispatched commands come back, retry or expire.
    /// </summary>
    public static void Advance(Random random, DeviceCommand command, DateTime now)
    {
        if (command.Status == CommandStatus.Queued)
        {
            // Priority decides how long a command waits behind the queue.
            var wait = command.Priority switch
            {
                CommandPriority.Immediate => 0.5,
                CommandPriority.High => 1.5,
                _ => 4.0
            };

            if ((now - command.IssuedUtc).TotalSeconds < wait)
            {
                return;
            }

            // A dry run is validated and logged but never leaves the gateway, so
            // it settles as acknowledged without a node ever seeing it.
            if (command.IsDryRun)
            {
                command.DispatchedUtc = now;
                command.AcknowledgedUtc = now;
                command.RoundTripMs = 0;
                command.Status = CommandStatus.Acknowledged;
                return;
            }

            command.DispatchedUtc = now;
            command.Status = CommandStatus.Sent;
            return;
        }

        if (command.Status != CommandStatus.Sent || command.DispatchedUtc is null)
        {
            return;
        }

        var inFlightMs = (now - command.DispatchedUtc.Value).TotalMilliseconds;
        var budget = RoundTrip(random, command);

        if (inFlightMs < budget)
        {
            return;
        }

        var roll = random.NextDouble();

        if (roll < 0.06)
        {
            // Three attempts and the gateway stops trying.
            command.Retries++;
            if (command.Retries >= 3)
            {
                command.Status = CommandStatus.Expired;
                return;
            }

            command.Status = CommandStatus.Queued;
            command.DispatchedUtc = null;
            return;
        }

        if (roll < 0.09)
        {
            command.Status = CommandStatus.Failed;
            return;
        }

        command.Status = CommandStatus.Acknowledged;
        command.AcknowledgedUtc = now;
        command.RoundTripMs = (int)Math.Round(inFlightMs);
    }

    /// <summary>Round trip a node of this kind would plausibly take, in milliseconds.</summary>
    private static int RoundTrip(Random random, DeviceCommand command)
    {
        // A firmware push is a transfer, not a ping, so it is an order of
        // magnitude slower than everything else.
        var baseline = command.CommandType switch
        {
            CommandType.FirmwarePush => random.Next(1_800, 4_500),
            CommandType.RestartNode => random.Next(600, 1_800),
            CommandType.Recalibrate => random.Next(300, 900),
            _ => random.Next(80, 420)
        };

        return command.Priority == CommandPriority.Immediate
            ? (int)(baseline * 0.7)
            : baseline;
    }

    private static CommandOrigin PickOrigin(Random random)
    {
        var roll = random.NextDouble();
        if (roll < 0.62) return CommandOrigin.Automation;
        if (roll < 0.85) return CommandOrigin.Schedule;
        return CommandOrigin.Manual;
    }

    private static CommandPriority PickPriority(Random random, CommandOrigin origin)
    {
        var roll = random.NextDouble();

        // A person at the console is usually there because something needs doing now.
        if (origin == CommandOrigin.Manual)
        {
            return roll < 0.45 ? CommandPriority.High : CommandPriority.Normal;
        }

        if (roll < 0.08) return CommandPriority.Immediate;
        if (roll < 0.25) return CommandPriority.High;
        return CommandPriority.Normal;
    }

    private static CommandType PickCommandType(Random random, SensorCategory category)
    {
        var candidates = CommandsByCategory.TryGetValue(category, out var types)
            ? types
            : Enum.GetValues<CommandType>();

        return candidates[random.Next(candidates.Length)];
    }

    private static string PickIssuer(Random random, CommandOrigin origin) => origin switch
    {
        CommandOrigin.Automation => AutomationSources[random.Next(AutomationSources.Length)],
        CommandOrigin.Schedule => ScheduleSources[random.Next(ScheduleSources.Length)],
        _ => Operators[random.Next(Operators.Length)]
    };

    /// <summary>
    /// Parameter strings are rendered verbatim in the stream, so they are built
    /// to look like what a gateway would actually carry.
    /// </summary>
    private static string BuildParameters(Random random, CommandType commandType, SensorProfile sensor)
    {
        return commandType switch
        {
            CommandType.SetThreshold => sensor.Category switch
            {
                SensorCategory.Environmental => $"temp.max={random.Next(22, 34)}.{random.Next(0, 10)}",
                SensorCategory.PowerConsumption => $"power.max={random.Next(2, 9)}.{random.Next(0, 10)}kW",
                SensorCategory.Motion => $"motion.sensitivity={random.Next(3, 9)}",
                _ => $"limit.max={random.Next(10, 90)}"
            },
            CommandType.Recalibrate => random.NextDouble() < 0.5
                ? "offset=auto"
                : $"offset={(random.NextDouble() < 0.5 ? "-" : "+")}{random.Next(0, 3)}.{random.Next(1, 10)}",
            CommandType.ToggleActuator => $"relay={random.Next(1, 4)},state={(random.NextDouble() < 0.5 ? "on" : "off")}",
            CommandType.RestartNode => random.NextDouble() < 0.7 ? "mode=soft" : "mode=hard",
            CommandType.FirmwarePush => $"v{random.Next(2, 4)}.{random.Next(0, 6)}.{random.Next(0, 9)}",
            CommandType.RequestSample => $"count={random.Next(1, 11)}",
            _ => string.Empty
        };
    }
}
