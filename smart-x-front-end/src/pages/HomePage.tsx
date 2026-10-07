import { useState } from "react";
import { Link } from "react-router-dom";
import StatTile from "../components/telemetry/StatTile";
import { hasActiveFilters } from "../components/commands/types";
import type { CommandFilters } from "../components/commands/types";
import { getCommandSummary, getInsights, getPipelineStatus, getSummary } from "../services/apiService";
import type { CommandSummary, EcosystemSummary, InsightsResponse, PipelineStatus, SensorFilters } from "../services/apiService";
import { isAbortError, usePolling } from "../hooks/usePolling";
import { usePeekState, usePersistentState } from "../state/appState";
import { formatNumber, formatTime } from "../utils/format";
import "./TelemetryPage.css";
import "./HomePage.css";

const REFRESH_MS = 10_000;

/** How many telemetry filter values are switched on. */
function countTelemetryFilters(filters: SensorFilters | undefined): number {
  if (!filters) return 0;
  return (
    (filters.categories?.length ?? 0) +
    (filters.statuses?.length ?? 0) +
    (filters.zones?.length ?? 0) +
    (filters.anomaliesOnly ? 1 : 0)
  );
}

/**
 * The landing page: one glance at the whole mesh, then a way into each
 * module. The cards also say where each module was left — the filters and
 * node kept for it — because returning to a page restores that state.
 */
function HomePage() {
  // Cached like the pages' own data, so coming back here shows the last
  // figures straight away while they refresh.
  const [telemetry, setTelemetry] = usePersistentState<EcosystemSummary | null>("home.telemetry", null);
  const [commands, setCommands] = usePersistentState<CommandSummary | null>("home.commands", null);
  const [pipeline, setPipeline] = usePersistentState<PipelineStatus | null>("home.pipeline", null);
  const [insights, setInsights] = usePersistentState<InsightsResponse | null>("home.insights", null);
  const [updated, setUpdated] = useState<Date | null>(null);

  usePolling(
    async (signal) => {
      const [summary, commandSummary, pipelineStatus, insightData] = await Promise.allSettled([
        getSummary(signal),
        getCommandSummary({}, signal),
        getPipelineStatus(undefined, signal),
        getInsights(undefined, signal),
      ]);
      if (signal.aborted) return;

      if (summary.status === "fulfilled") setTelemetry(summary.value);
      if (commandSummary.status === "fulfilled") setCommands(commandSummary.value);
      if (pipelineStatus.status === "fulfilled") setPipeline(pipelineStatus.value);
      if (insightData.status === "fulfilled") setInsights(insightData.value);

      const failure = [summary, commandSummary, pipelineStatus, insightData].find(
        (result) => result.status === "rejected" && !isAbortError(result.reason)
      );
      if (!failure) setUpdated(new Date());
    },
    { intervalMs: REFRESH_MS }
  );

  // What each module will reopen with.
  const telemetryFilters = usePeekState<SensorFilters>("telemetry.filters", { session: true });
  const commandFilters = usePeekState<CommandFilters>("commands.filters", { session: true });
  const targetNode = usePeekState<string>("commands.targetNode", { session: true });

  const telemetryFilterCount = countTelemetryFilters(telemetryFilters);
  const attention = pipeline?.setChanges.needsAttention.length ?? 0;

  return (
    <div className="home-page">
      <header className="page-head">
        <div>
          <h1 className="page-title">Smart-X Mesh Overview</h1>
          <p className="page-sub">
            The whole IoT mesh at a glance. Open a module to investigate sensors or act on
            devices; each one keeps its filters and selection while you move between them.
          </p>
        </div>
        <span className="page-refresh">
          {updated ? `Updated ${formatTime(updated.toISOString())}` : "Connecting…"}
        </span>
      </header>

      <section className="stat-row" aria-label="Mesh overview">
        <StatTile
          label="Mesh health"
          value={telemetry ? telemetry.meshHealthScore.toFixed(1) : "—"}
          unit="%"
          tone={!telemetry || telemetry.meshHealthScore >= 85 ? "accent" : telemetry.meshHealthScore >= 65 ? "warning" : "danger"}
          progress={telemetry?.meshHealthScore}
          hint={telemetry ? `${telemetry.onlineCount} of ${telemetry.totalSensors} sensors online` : undefined}
        />
        <StatTile
          label="Active alerts"
          value={telemetry ? telemetry.activeAlertCount : "—"}
          tone={telemetry && telemetry.criticalAlertCount > 0 ? "danger" : "default"}
          hint={telemetry ? `${telemetry.criticalAlertCount} critical` : undefined}
        />
        <StatTile
          label="Commands in flight"
          value={commands ? commands.inFlightCount : "—"}
          hint={commands ? `${commands.queuedCount} queued · ${commands.dispatchRate.toFixed(1)}/min` : undefined}
        />
        <StatTile
          label="Needs attention"
          value={pipeline ? attention : "—"}
          tone={attention > 0 ? "warning" : "default"}
          hint="Disconnected ∪ critical breach"
        />
      </section>

      <div className="home-pillars">
        <Link to="/telemetry" className="home-pillar">
          <span className="home-pillar-tag">Module 1</span>
          <h2>Sensor Data Ingestion and Telemetry</h2>
          <p>
            Live readings, thresholds and alerts for every sensor. Register devices, attach
            logs and photos, and follow guided troubleshooting.
          </p>
          <dl className="home-pillar-stats">
            <div>
              <dt>Readings / hour</dt>
              <dd>{telemetry ? formatNumber(telemetry.readingsLastHour) : "—"}</dd>
            </div>
            <div>
              <dt>Anomalies / hour</dt>
              <dd>{telemetry ? telemetry.anomaliesLastHour : "—"}</dd>
            </div>
            <div>
              <dt>Ingest success</dt>
              <dd>{telemetry ? `${telemetry.ingestSuccessRate.toFixed(1)}%` : "—"}</dd>
            </div>
          </dl>
          <span className="home-pillar-resume">
            {telemetryFilterCount > 0
              ? `Resume with ${telemetryFilterCount} filter${telemetryFilterCount === 1 ? "" : "s"} applied →`
              : "Open telemetry →"}
          </span>
        </Link>

        <Link to="/commands" className="home-pillar">
          <span className="home-pillar-tag">Module 2</span>
          <h2>Real-Time Command Stream and History</h2>
          <p>
            Watch dispatch traffic, override any node with undo and redo, see the intake
            queues at work, and act on the engine&apos;s suggestions.
          </p>
          <dl className="home-pillar-stats">
            <div>
              <dt>Acknowledged</dt>
              <dd>{commands ? `${commands.acknowledgedRate.toFixed(1)}%` : "—"}</dd>
            </div>
            <div>
              <dt>Suggestions</dt>
              <dd>{insights ? insights.suggestions.length : "—"}</dd>
            </div>
            <div>
              <dt>Rules learned</dt>
              <dd>{insights ? insights.learning.rulesLearned : "—"}</dd>
            </div>
          </dl>
          <span className="home-pillar-resume">
            {targetNode
              ? `Resume on ${targetNode}${commandFilters && hasActiveFilters(commandFilters) ? " with filters" : ""} →`
              : commandFilters && hasActiveFilters(commandFilters)
                ? "Resume with your filters →"
                : "Open command stream →"}
          </span>
        </Link>
      </div>
    </div>
  );
}

export default HomePage;
