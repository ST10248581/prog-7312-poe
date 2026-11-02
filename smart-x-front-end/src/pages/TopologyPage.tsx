import { useState } from "react";
import StatTile from "../components/telemetry/StatTile";
import "./TelemetryPage.css";
import "./TopologyPage.css";

/*
 * UI template for Module 3 — Network Topology and Mesh Routing.
 * Everything below is placeholder layout: no API calls, no real data.
 * Each section marks the feature it will host once wired up:
 *   1. Topology dashboard  — graph of the active mesh (nodes + links)
 *   2. Route tracker       — trace a node's path to the gateway by MAC
 *   3. Telemetry history   — ordered log for the selected sensor
 */

type ViewMode = "mesh" | "mst" | "route";

const VIEW_MODES: { id: ViewMode; label: string }[] = [
  { id: "mesh", label: "Full mesh" },
  { id: "mst", label: "Minimum spanning tree" },
  { id: "route", label: "Traced route" },
];

/** Placeholder layout so the canvas has something to show. */
const DEMO_NODES = [
  { id: "GW", x: 50, y: 50, kind: "gateway" },
  { id: "A", x: 20, y: 20, kind: "sensor" },
  { id: "B", x: 78, y: 18, kind: "sensor" },
  { id: "C", x: 15, y: 62, kind: "sensor" },
  { id: "D", x: 82, y: 70, kind: "sensor" },
  { id: "E", x: 40, y: 85, kind: "offline" },
  { id: "F", x: 62, y: 88, kind: "sensor" },
] as const;

const DEMO_LINKS: [string, string][] = [
  ["A", "GW"], ["B", "GW"], ["C", "GW"], ["D", "GW"],
  ["A", "B"], ["A", "C"], ["C", "E"], ["E", "F"], ["F", "D"], ["B", "D"],
];

const HOP_PLACEHOLDERS = ["Source node", "Relay hop", "Relay hop", "Gateway"];

function TopologyPage() {
  const [view, setView] = useState<ViewMode>("mesh");
  const [mac, setMac] = useState("");

  const nodeAt = (id: string) => DEMO_NODES.find((node) => node.id === id)!;

  return (
    <div className="topology-page">
      <header className="page-head">
        <div>
          <h1 className="page-title">Network Topology and Mesh Routing</h1>
          <p className="page-sub">
            Visualise the active IoT mesh, trace how any node&apos;s packets hop back to the
            gateway, and review a sensor&apos;s telemetry history.
          </p>
        </div>
        <div className="page-head-actions">
          <span className="page-refresh">Not connected</span>
          <button type="button" className="register-btn" disabled>
            Recompute routes
          </button>
        </div>
      </header>

      {/* Mesh overview */}
      <section className="stat-row" aria-label="Topology overview">
        <StatTile label="Active nodes" value="—" hint="Online of total registered" />
        <StatTile label="Mesh links" value="—" hint="Edges in the network graph" />
        <StatTile label="Gateways" value="—" hint="Root nodes for routing" />
        <StatTile label="Average hops" value="—" hint="Node → gateway" />
        <StatTile label="MST total cost" value="—" hint="Sum of spanning-tree weights" />
      </section>

      <div className="topology-grid">
        {/* 1. Topology dashboard */}
        <section className="topology-panel" aria-label="Mesh network graph">
          <header className="panel-head">
            <h2>Active mesh network</h2>
            <div className="filter-chips" role="group" aria-label="Graph view">
              {VIEW_MODES.map((mode) => (
                <button
                  key={mode.id}
                  type="button"
                  className={`filter-chip${view === mode.id ? " active" : ""}`}
                  aria-pressed={view === mode.id}
                  onClick={() => setView(mode.id)}
                >
                  {mode.label}
                </button>
              ))}
            </div>
          </header>

          <div className="topology-toolbar">
            <input
              type="search"
              className="topology-input"
              placeholder="Filter by zone or node…"
              aria-label="Filter graph"
              disabled
            />
            <div className="topology-zoom" role="group" aria-label="Zoom">
              <button type="button" className="topology-icon-btn" aria-label="Zoom out" disabled>−</button>
              <button type="button" className="topology-icon-btn" aria-label="Reset view" disabled>⟲</button>
              <button type="button" className="topology-icon-btn" aria-label="Zoom in" disabled>+</button>
            </div>
          </div>

          <div className="topology-canvas">
            <svg viewBox="0 0 100 100" preserveAspectRatio="xMidYMid meet" role="img" aria-label="Placeholder mesh graph">
              {DEMO_LINKS.map(([from, to]) => {
                const a = nodeAt(from);
                const b = nodeAt(to);
                return (
                  <line
                    key={`${from}-${to}`}
                    x1={a.x}
                    y1={a.y}
                    x2={b.x}
                    y2={b.y}
                    className="topology-link"
                  />
                );
              })}
              {DEMO_NODES.map((node) => (
                <g key={node.id} className={`topology-node topology-node-${node.kind}`}>
                  <circle cx={node.x} cy={node.y} r={node.kind === "gateway" ? 5 : 3.5} />
                  <text x={node.x} y={node.y + (node.kind === "gateway" ? 9 : 7.5)}>{node.id}</text>
                </g>
              ))}
            </svg>
            <span className="topology-canvas-note">Graph placeholder — live topology renders here</span>
          </div>

          <ul className="topology-legend" aria-label="Legend">
            <li><span className="legend-dot legend-gateway" /> Gateway</li>
            <li><span className="legend-dot legend-sensor" /> Sensor node</li>
            <li><span className="legend-dot legend-offline" /> Offline node</li>
            <li><span className="legend-line" /> Mesh link (weighted)</li>
            <li><span className="legend-line legend-line-route" /> Active route</li>
          </ul>
        </section>

        {/* 2. Route tracker */}
        <aside className="topology-panel route-panel" aria-label="Route tracker">
          <header className="panel-head">
            <h2>Route tracker</h2>
            <span className="panel-head-count">by MAC / node ID</span>
          </header>

          <form className="route-form" onSubmit={(event) => event.preventDefault()}>
            <label className="route-label" htmlFor="route-mac">Node MAC address or identifier</label>
            <input
              id="route-mac"
              className="topology-input"
              placeholder="e.g. AA:BB:CC:DD:EE:FF"
              value={mac}
              onChange={(event) => setMac(event.target.value)}
            />
            <button type="submit" className="register-btn" disabled={!mac.trim()}>
              Trace route
            </button>
          </form>

          <dl className="route-facts">
            <div><dt>Hops</dt><dd>—</dd></div>
            <div><dt>Path cost</dt><dd>—</dd></div>
            <div><dt>Latency</dt><dd>—</dd></div>
          </dl>

          <ol className="route-hops" aria-label="Route hops">
            {HOP_PLACEHOLDERS.map((label, index) => (
              <li key={index} className="route-hop">
                <span className="route-hop-index">{index + 1}</span>
                <div>
                  <span className="route-hop-name">{label}</span>
                  <span className="route-hop-meta">MAC — · link cost —</span>
                </div>
              </li>
            ))}
          </ol>

          <p className="panel-empty">Enter a node identifier to see its path to the gateway.</p>
        </aside>
      </div>

      {/* 3. Telemetry history */}
      <section className="topology-panel" aria-label="Sensor telemetry history">
        <header className="panel-head">
          <h2>Telemetry history</h2>
          <div className="history-controls">
            <select className="topology-input" aria-label="Sensor" disabled defaultValue="">
              <option value="">Select a sensor…</option>
            </select>
            <select className="topology-input" aria-label="Time range" disabled defaultValue="24h">
              <option value="1h">Last hour</option>
              <option value="24h">Last 24 hours</option>
              <option value="7d">Last 7 days</option>
            </select>
            <select className="topology-input" aria-label="Sort order" disabled defaultValue="newest">
              <option value="newest">Newest first</option>
              <option value="oldest">Oldest first</option>
            </select>
          </div>
        </header>

        <div className="history-scroll">
          <table className="data-table">
            <thead>
              <tr>
                <th>Timestamp</th>
                <th>Reading type</th>
                <th>Value</th>
                <th>Route (hops)</th>
                <th>Status</th>
              </tr>
            </thead>
            <tbody>
              {Array.from({ length: 5 }, (_, index) => (
                <tr key={index} className="history-placeholder-row">
                  <td className="mono">—</td>
                  <td>—</td>
                  <td className="mono">—</td>
                  <td>—</td>
                  <td>—</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>

        <footer className="history-foot">
          <span className="panel-head-count">0 entries</span>
          <div className="topology-zoom">
            <button type="button" className="filter-chip" disabled>← Prev</button>
            <button type="button" className="filter-chip" disabled>Next →</button>
          </div>
        </footer>
      </section>
    </div>
  );
}

export default TopologyPage;
