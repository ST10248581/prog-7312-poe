import { useState } from "react";
import CommandFilterBar from "../components/commands/CommandFilterBar";
import CommandHistoryTable from "../components/commands/CommandHistoryTable";
import CommandStream from "../components/commands/CommandStream";
import OverrideConsole from "../components/commands/OverrideConsole";
import ThroughputStrip from "../components/commands/ThroughputStrip";
import StatTile from "../components/telemetry/StatTile";
import {
  PLACEHOLDER_COMMANDS,
  PLACEHOLDER_NODES,
  PLACEHOLDER_THROUGHPUT,
  PLACEHOLDER_ZONES,
} from "../components/commands/placeholderData";
import { EMPTY_FILTERS, TIME_WINDOWS } from "../components/commands/types";
import type { CommandFilters, CommandRecord } from "../components/commands/types";
import { formatTime } from "../utils/format";
// Shared widget styles — stat tiles, filter chips, panels and .data-table all
// live in the telemetry sheet. Imported explicitly so this route does not rely
// on the telemetry route having been loaded first.
import "./TelemetryPage.css";
import "./CommandsPage.css";

const PAGE_SIZE = 25;

/**
 * Real-Time Command Stream and History — layout pass.
 *
 * Structure mirrors the telemetry route: overview -> filter -> stream and act
 * -> audit. Filtering, paging and dispatch are all server-side concerns; this
 * page only holds the filter state and renders what it is given, so wiring the
 * API means replacing the placeholder constants with fetches, not reshaping
 * the components.
 *
 * Not yet implemented: /api/commands (list + page), /api/commands/stream
 * (live tail), POST /api/commands (override dispatch).
 */
function CommandsPage() {
  const [filters, setFilters] = useState<CommandFilters>(EMPTY_FILTERS);
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [targetNode, setTargetNode] = useState("");
  const [live, setLive] = useState(true);
  const [page, setPage] = useState(1);

  // Placeholder stand-ins for the API response. The real page refetches on
  // every filter change; nothing is narrowed in the browser.
  const commands = PLACEHOLDER_COMMANDS;
  const totalCount = 1284;
  const pending = commands.filter(
    (command) => command.status === "Queued" || command.status === "Sent",
  );

  const windowLabel =
    TIME_WINDOWS.find((window) => window.minutes === filters.windowMinutes)?.label ?? "1h";

  const handleFilterChange = (next: CommandFilters) => {
    setFilters(next);
    // A new slice invalidates the page cursor.
    setPage(1);
  };

  // Selecting a row aims the override console at that node — correcting a bad
  // command should not mean retyping its target.
  const handleSelect = (command: CommandRecord) => {
    setSelectedId(command.id);
    setTargetNode(command.nodeId);
  };

  return (
    <div className="commands-page">
      <header className="page-head">
        <div>
          <h1 className="page-title">Real-Time Command Stream and History</h1>
          <p className="page-sub">
            Watch → filter → override → audit. Live dispatch traffic across the
            Smart-X mesh, with manual control of any node and the full command
            log behind it.
          </p>
        </div>

        <div className="page-head-actions">
          <button
            type="button"
            className={`live-toggle${live ? " active" : ""}`}
            onClick={() => setLive((value) => !value)}
          >
            <span className="live-dot" />
            {live ? "Live" : "Paused"}
          </button>
          <span className="page-refresh">Updated {formatTime(new Date().toISOString())}</span>
        </div>
      </header>

      <div className="page-notice">
        <strong>Layout preview.</strong> This route is the planned structure only —
        the figures, stream rows and history below are placeholders, and the
        override form does not dispatch. Filtering, paging and dispatch are all
        API-side; the controls here just hold the query.
      </div>

      {/* Overview: dispatch health before any individual command. */}
      <section className="stat-row" aria-label="Command overview">
        <StatTile
          label="Dispatch rate"
          value="9.4"
          unit="/min"
          tone="accent"
          hint="Across all origins"
        />

        <StatTile label="In flight" value={pending.length} unit="awaiting ack">
          <div className="status-breakdown">
            <span className="status-chip">3 queued</span>
            <span className="status-chip status-warning">2 retrying</span>
          </div>
        </StatTile>

        <StatTile
          label="Acknowledged"
          value="97.2"
          unit="%"
          progress={97.2}
          hint="Last 24 hours"
        />

        <StatTile
          label="Failed"
          value="11"
          tone="danger"
          hint="4 expired without acknowledgement"
        />

        <StatTile
          label="Manual overrides"
          value="26"
          tone="warning"
          hint="Today, across 9 operators"
        />

        <StatTile
          label="Round trip"
          value="284"
          unit="ms"
          hint="Median, acknowledged commands"
        />
      </section>

      {/* Visualises the incoming stream as a rate, so a burst or a stall is
          visible without reading individual rows. */}
      <section className="throughput-panel" aria-label="Dispatch throughput">
        <header className="panel-head">
          <h2>Dispatch throughput</h2>
          <span className="panel-head-count">commands per minute</span>
        </header>
        <ThroughputStrip values={PLACEHOLDER_THROUGHPUT} windowLabel={windowLabel} />
      </section>

      <CommandFilterBar
        filters={filters}
        zones={PLACEHOLDER_ZONES}
        resultCount={commands.length}
        totalCount={totalCount}
        onChange={handleFilterChange}
      />

      <div className="commands-grid">
        <CommandStream
          commands={commands}
          selectedId={selectedId}
          live={live}
          onSelect={handleSelect}
        />

        <OverrideConsole
          nodes={PLACEHOLDER_NODES}
          targetNode={targetNode}
          pending={pending}
          onTargetChange={setTargetNode}
        />
      </div>

      <CommandHistoryTable
        commands={commands}
        page={page}
        pageSize={PAGE_SIZE}
        totalCount={totalCount}
        onPageChange={setPage}
      />
    </div>
  );
}

export default CommandsPage;
