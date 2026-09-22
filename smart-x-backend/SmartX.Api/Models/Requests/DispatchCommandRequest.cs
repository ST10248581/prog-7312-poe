using SmartX.Api.Models;

namespace SmartX.Api.Models.Requests;

/// <summary>Manual override submitted from the command console.</summary>
public class DispatchCommandRequest
{
    public string NodeId { get; set; } = string.Empty;
    public CommandType CommandType { get; set; }
    public string Parameters { get; set; } = string.Empty;
    public CommandPriority Priority { get; set; } = CommandPriority.Normal;

    /// <summary>Validate and log the command without dispatching it to the node.</summary>
    public bool DryRun { get; set; }

    /// <summary>Operator id the override is logged against.</summary>
    public string IssuedBy { get; set; } = "operator";
}
