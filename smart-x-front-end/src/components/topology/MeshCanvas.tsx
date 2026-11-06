import { useMemo } from "react";
import type { MeshLink, MeshNode } from "../../services/apiService";

export type ViewMode = "mesh" | "mst" | "route" | "scan";

interface MeshCanvasProps {
  nodes: MeshNode[];
  links: MeshLink[];
  view: ViewMode;
  /**
   * Links to emphasise on top of the view: the traced route ("route"), the
   * traced node's power-efficient path along the MST ("mst"), or the BFS/DFS
   * traversal tree ("scan").
   */
  routeLinkIds: string[];
  /** Nodes on that highlight; others are dimmed in the "route" and "scan" views. */
  routeNodeIds: string[];
  selectedId: string | null;
  /** Lower-cased zone / node / MAC text; non-matching nodes are dimmed. */
  filter: string;
  zoom: number;
  onSelect: (nodeId: string) => void;
}

/**
 * Draws the mesh exactly as the API laid it out: positions, link weights and
 * which links belong to the spanning tree or the traced route all come from
 * `/api/topology`. Nothing is computed here beyond what to emphasise.
 */
function MeshCanvas({ nodes, links, view, routeLinkIds, routeNodeIds, selectedId, filter, zoom, onSelect }: MeshCanvasProps) {
  const byId = useMemo(() => new Map(nodes.map((node) => [node.id, node])), [nodes]);
  const routeLinks = useMemo(() => new Set(routeLinkIds), [routeLinkIds]);
  const routeNodes = useMemo(() => new Set(routeNodeIds), [routeNodeIds]);

  const matches = (node: MeshNode) =>
    !filter ||
    node.id.toLowerCase().includes(filter) ||
    node.zone.toLowerCase().includes(filter) ||
    node.name.toLowerCase().includes(filter) ||
    node.macAddress.toLowerCase().includes(filter);

  // Zoom towards the selected node, or the centre when nothing is selected.
  const focus = (selectedId && byId.get(selectedId)) || { x: 50, y: 50 };
  const size = 100 / zoom;
  const minX = Math.min(Math.max(focus.x - size / 2, 0), 100 - size);
  const minY = Math.min(Math.max(focus.y - size / 2, 0), 100 - size);

  const linkClass = (link: MeshLink) => {
    const classes = ["topology-link"];
    if (!link.isActive) classes.push("topology-link-inactive");

    if (view === "mst") {
      classes.push(routeLinks.has(link.id) ? "topology-link-route" : link.inMst ? "topology-link-mst" : "topology-link-faded");
    } else if (view === "route" || view === "scan") {
      classes.push(routeLinks.has(link.id) ? "topology-link-route" : "topology-link-faded");
    } else if (link.inRoutingTree) {
      classes.push("topology-link-tree");
    }

    return classes.join(" ");
  };

  const nodeClass = (node: MeshNode) => {
    const classes = ["topology-node"];
    if (node.kind === "Gateway") classes.push("topology-node-gateway");
    if (node.state === "Offline") classes.push("topology-node-offline");
    if (node.state === "Degraded") classes.push("topology-node-degraded");
    if (node.state === "Isolated") classes.push("topology-node-isolated");
    if (node.isRerouted) classes.push("topology-node-rerouted");
    if (node.id === selectedId) classes.push("topology-node-selected");
    if (!matches(node) || ((view === "route" || view === "scan") && routeNodes.size > 0 && !routeNodes.has(node.id))) {
      classes.push("topology-node-dim");
    }
    return classes.join(" ");
  };

  // Emphasised links are drawn last so they sit on top of the faded ones.
  const ordered = [...links].sort((a, b) => emphasised(a) - emphasised(b));
  function emphasised(link: MeshLink) {
    if (routeLinks.has(link.id) && view !== "mesh") return 2;
    return view === "mst" ? Number(link.inMst) : view === "mesh" ? Number(link.inRoutingTree) : 0;
  }

  return (
    <svg
      viewBox={`${minX} ${minY} ${size} ${size}`}
      preserveAspectRatio="xMidYMid meet"
      role="img"
      aria-label={`Mesh graph: ${nodes.length} nodes, ${links.length} links`}
    >
      {ordered.map((link) => {
        const a = byId.get(link.from);
        const b = byId.get(link.to);
        if (!a || !b) return null;
        return (
          <line key={link.id} x1={a.x} y1={a.y} x2={b.x} y2={b.y} className={linkClass(link)}>
            <title>
              {`${link.from} ↔ ${link.to} · cost ${link.cost} (ETX) · ${link.rssiDbm} dBm · ${link.distanceMetres} m · ${link.latencyMs} ms${link.isActive ? "" : " · inactive"}`}
            </title>
          </line>
        );
      })}

      {nodes.map((node) => {
        const radius = node.kind === "Gateway" ? 3 : 1.7;
        return (
          <g
            key={node.id}
            className={nodeClass(node)}
            onClick={() => onSelect(node.id)}
            role="button"
            tabIndex={0}
            aria-label={`${node.id}, ${node.state}`}
            onKeyDown={(event) => {
              if (event.key === "Enter" || event.key === " ") {
                event.preventDefault();
                onSelect(node.id);
              }
            }}
          >
            <circle cx={node.x} cy={node.y} r={radius} />
            <text x={node.x} y={node.y + radius + 2.2}>{node.id}</text>
            <title>
              {`${node.name} (${node.id})\n${node.macAddress} · ${node.zone}\n${node.state}` +
                (node.hops !== null ? ` · ${node.hops} hop(s) to ${node.gatewayId} · cost ${node.pathCost}` : "") +
                (node.dependants > 0 ? `\nRelays for ${node.dependants} node(s)` : "") +
                (node.isRerouted ? "\nRe-routed around a failure" : "")}
            </title>
          </g>
        );
      })}
    </svg>
  );
}

export default MeshCanvas;
