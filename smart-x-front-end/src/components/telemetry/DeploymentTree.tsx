import type { DeploymentNode, DeploymentTier } from "../../services/apiService";

/** Short badge per tier, so the hierarchy reads at a glance in a dense tree. */
const TIER_BADGE: Record<DeploymentTier, string> = {
  Facility: "FAC",
  Zone: "ZONE",
  SubZone: "SUB",
  Node: "NODE",
};

interface TreeNodeProps {
  node: DeploymentNode;
  /** Fully qualified path, matching the paths the API reports issues against. */
  path: string;
  depth: number;
  overrides: Record<string, boolean>;
  issuePaths: Set<string>;
  onToggle: (path: string) => void;
  onSelectSensor: (sensorProfileId: string) => void;
}

/**
 * Renders one node and then calls itself for each child. The component mirrors
 * the shape of the data and of the recursive validator on the API side: the
 * depth is not known in advance, so the walk is self-similar rather than a
 * fixed ladder of loops.
 */
function TreeNode({
  node,
  path,
  depth,
  overrides,
  issuePaths,
  onToggle,
  onSelectSensor,
}: TreeNodeProps) {
  const isLeaf = node.children.length === 0;

  // Facility and zones open by default; sub-zones stay collapsed so a 40-node
  // mesh still fits on screen. An explicit click always wins.
  const expanded = overrides[path] ?? depth < 2;
  const flagged = issuePaths.has(path);

  const status = node.status ? node.status.toLowerCase() : "unknown";
  const name = node.name.trim() === "" ? "(unnamed)" : node.name;

  const rowContent = (
    <>
      <span className={`tree-badge tier-${node.tier.toLowerCase()}`}>
        {TIER_BADGE[node.tier]}
      </span>
      <span className="tree-name">{name}</span>
      {node.tier === "Node" ? (
        <span className={`tree-status status-${status}`}>{node.status ?? "—"}</span>
      ) : (
        <span className="tree-count">{node.children.length}</span>
      )}
      {flagged && <span className="tree-flag" title="Validation issue on this node">!</span>}
    </>
  );

  return (
    <li className="tree-node">
      <div className={`tree-row${flagged ? " flagged" : ""}`}>
        {isLeaf ? (
          <span className="tree-toggle spacer" />
        ) : (
          <button
            type="button"
            className="tree-toggle"
            aria-expanded={expanded}
            aria-label={expanded ? `Collapse ${name}` : `Expand ${name}`}
            onClick={() => onToggle(path)}
          >
            {expanded ? "▾" : "▸"}
          </button>
        )}

        {node.sensorProfileId ? (
          <button
            type="button"
            className="tree-label as-button"
            onClick={() => onSelectSensor(node.sensorProfileId!)}
            title="Open sensor details"
          >
            {rowContent}
          </button>
        ) : (
          <span className="tree-label">{rowContent}</span>
        )}
      </div>

      {!isLeaf && expanded && (
        <ul className="tree-children">
          {node.children.map((child, index) => (
            <TreeNode
              key={`${path}/${child.name}-${index}`}
              node={child}
              path={`${path} -> ${child.name.trim() === "" ? "(unnamed)" : child.name}`}
              depth={depth + 1}
              overrides={overrides}
              issuePaths={issuePaths}
              onToggle={onToggle}
              onSelectSensor={onSelectSensor}
            />
          ))}
        </ul>
      )}
    </li>
  );
}

interface DeploymentTreeProps {
  root: DeploymentNode;
  overrides: Record<string, boolean>;
  issuePaths: Set<string>;
  onToggle: (path: string) => void;
  onSelectSensor: (sensorProfileId: string) => void;
}

function DeploymentTree({
  root,
  overrides,
  issuePaths,
  onToggle,
  onSelectSensor,
}: DeploymentTreeProps) {
  return (
    <ul className="tree-root">
      <TreeNode
        node={root}
        path={root.name}
        depth={0}
        overrides={overrides}
        issuePaths={issuePaths}
        onToggle={onToggle}
        onSelectSensor={onSelectSensor}
      />
    </ul>
  );
}

export default DeploymentTree;
