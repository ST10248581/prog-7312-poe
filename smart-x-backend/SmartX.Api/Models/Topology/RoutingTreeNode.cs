// =============================================================================
// CODE ATTRIBUTION — Trees (Part 3)
//
// The routing tree — every node keeping a preferred parent towards a border
// router, so the routes together form a tree rooted at each gateway — and the
// recursive walk over it were written with reference to the sources below.
//
// Code Attribution [32]
// Author: Winter, T., Thubert, P., Brandt, A., Hui, J., Kelsey, R., Levis, P., Pister, K., Struik, R., Vasseur, J.P. and Alexander, R.
// Year: 2012
// Title: RPL: IPv6 Routing Protocol for Low-Power and Lossy Networks (RFC 6550)
// Type: [Source code]
// Available at: <https://www.rfc-editor.org/rfc/rfc6550>
// Accessed: [Accessed 2 November 2026]
// Modifications: No code copied. Followed the DODAG model: each node picks one
//   preferred parent with the lowest path cost ("rank") to a root, the parent
//   links form a tree per border router, and a node whose parent fails picks a
//   new one. Rank is the Dijkstra path cost here rather than RPL's objective
//   function.
// Reference: Winter, T. et al., 2012. RPL: IPv6 Routing Protocol for Low-Power and Lossy Networks (RFC 6550) [Source code] Available at: <https://www.rfc-editor.org/rfc/rfc6550> [Accessed 2 November 2026].
//
// Code Attribution [12]
// Author: Microsoft
// Year: 2021
// Title: Iterate Through All Nodes of TreeView Control – Windows Forms
// Type: [Source code]
// Available at: <https://learn.microsoft.com/en-us/dotnet/desktop/winforms/controls/how-to-iterate-through-all-nodes-of-a-windows-forms-treeview-control>
// Accessed: [Accessed 2 November 2026]
// Modifications: Reused the node-holding-child-nodes shape already adopted for
//   DeploymentNode. The recursive subtree walk with a depth guard lives in
//   SmartXTopologyEngine.CountDependants.
// Reference: Microsoft, 2021. Iterate Through All Nodes of TreeView Control – Windows Forms [Source code] Available at: <https://learn.microsoft.com/en-us/dotnet/desktop/winforms/controls/how-to-iterate-through-all-nodes-of-a-windows-forms-treeview-control> [Accessed 2 November 2026].
// =============================================================================

namespace SmartX.Api.Models.Topology;

/// <summary>
/// One node of the routing tree. A gateway is a root; every other node hangs
/// under the neighbour it forwards to, so a node's subtree is exactly the set of
/// nodes whose traffic passes through it.
/// </summary>
public class RoutingTreeNode
{
    public string NodeId { get; set; } = string.Empty;
    public MeshNodeKind Kind { get; set; }

    /// <summary>Links between this node and its root.</summary>
    public int Depth { get; set; }

    public double PathCost { get; set; }

    /// <summary>Nodes below this one — filled in by the recursive walk.</summary>
    public int SubtreeSize { get; set; }

    public List<RoutingTreeNode> Children { get; set; } = new();
}
