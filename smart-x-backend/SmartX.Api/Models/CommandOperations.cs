namespace SmartX.Api.Models;

/// <summary>
/// The single place that says which operation category a command type belongs
/// to. Both the filter and the category a command reports read it from here, so
/// a new command type cannot end up categorised one way in the query and
/// another way on the record.
/// </summary>
public static class CommandOperations
{
    private static readonly Dictionary<CommandType, OperationCategory> Categories = new()
    {
        // Changes what the node is configured to do.
        [CommandType.SetThreshold] = OperationCategory.Configuration,
        [CommandType.FirmwarePush] = OperationCategory.Configuration,

        // Restores a node that is drifting or unresponsive.
        [CommandType.Recalibrate] = OperationCategory.Maintenance,
        [CommandType.RestartNode] = OperationCategory.Maintenance,

        // Acts on the physical world through the node.
        [CommandType.ToggleActuator] = OperationCategory.Control,

        // Asks the node for something without changing it.
        [CommandType.RequestSample] = OperationCategory.Diagnostics
    };

    /// <summary>
    /// The category a command type belongs to. Anything unmapped is treated as
    /// a diagnostic rather than silently disappearing from every category
    /// filter — an uncategorised command is still one an operator must see.
    /// </summary>
    public static OperationCategory CategoryOf(CommandType commandType)
    {
        return Categories.TryGetValue(commandType, out var category)
            ? category
            : OperationCategory.Diagnostics;
    }

    /// <summary>The command types that make up a category, for the filter UI.</summary>
    public static List<CommandType> TypesIn(OperationCategory category)
    {
        return Categories
            .Where(entry => entry.Value == category)
            .Select(entry => entry.Key)
            .ToList();
    }
}
