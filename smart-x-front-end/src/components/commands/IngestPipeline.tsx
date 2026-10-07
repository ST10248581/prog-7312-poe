import { useState } from "react";
import { formatNumber, formatRelative, formatTime } from "../../utils/format";
import type { PacketIntakeResult, PipelineStatus } from "../../services/apiService";

interface IngestPipelineProps {
  pipeline: PipelineStatus | null;
  /** Sends a routine burst plus one critical spike, to show the priority lane overtaking. */
  onSimulateSpike: () => Promise<PacketIntakeResult>;
  /** Floods the standard lane past its capacity, to show backpressure shedding the oldest packets. */
  onSimulateFlood: () => Promise<PacketIntakeResult>;
  onInspect: (nodeId: string) => void;
}

type Simulation = "spike" | "flood";

/**
 * The telemetry intake behind the command stream, made visible.
 *
 * Two lanes: routine packets queue first-in, first-out (Queue&lt;T&gt;) and drain
 * at a fixed budget per tick; critical ones go to a PriorityQueue that is
 * emptied at once, overtaking everything still queued. The standard lane has a
 * capacity, past which the oldest packets are shed (backpressure). Under them
 * are the two HashSets that keep alerts unique — open error states and
 * disconnected nodes — with how many repeats each has absorbed, and what
 * changed in the disconnected set since this page last looked (set algebra).
 */
function IngestPipeline({ pipeline, onSimulateSpike, onSimulateFlood, onInspect }: IngestPipelineProps) {
  const [sending, setSending] = useState<Simulation | null>(null);
  const [result, setResult] = useState<{ kind: Simulation; data: PacketIntakeResult } | null>(null);
  const [error, setError] = useState<string | null>(null);

  const simulate = async (kind: Simulation) => {
    setSending(kind);
    setError(null);

    try {
      const data = await (kind === "spike" ? onSimulateSpike() : onSimulateFlood());
      setResult({ kind, data });
    } catch (err) {
      setError(err instanceof Error ? err.message : "The intake rejected the packets.");
    } finally {
      setSending(null);
    }
  };

  const capacity = pipeline?.standardQueueCapacity ?? 0;
  const fill = pipeline && capacity > 0 ? Math.min(100, (pipeline.standardQueueDepth / capacity) * 100) : 0;
  const fillTone = fill >= 90 ? "danger" : fill >= 60 ? "warning" : "ok";
  const changes = pipeline?.setChanges;

  return (
    <section className="ingest-pipeline" aria-label="Telemetry intake">
      <header className="panel-head">
        <h2>Telemetry intake</h2>
        <span className="panel-head-count">
          {pipeline ? `${pipeline.registeredDevices} devices registered` : "loading…"}
        </span>
      </header>

      <div className="pipeline-lanes">
        <div className="pipeline-lane">
          <span className="pipeline-lane-label">Standard queue · FIFO</span>
          <strong className="pipeline-lane-depth">
            {pipeline?.standardQueueDepth ?? "—"}
            {pipeline && <span className="pipeline-lane-capacity"> / {formatNumber(capacity)}</span>}
          </strong>
          <div
            className={`pipeline-capacity pipeline-capacity-${fillTone}`}
            role="meter"
            aria-label="Standard queue fill"
            aria-valuemin={0}
            aria-valuemax={capacity}
            aria-valuenow={pipeline?.standardQueueDepth ?? 0}
          >
            <div className="pipeline-capacity-fill" style={{ width: `${fill}%` }} />
          </div>
          <span className="pipeline-lane-meta">
            {pipeline
              ? `${formatNumber(pipeline.standardProcessed)} processed · ${pipeline.standardBudgetPerTick}/tick · avg wait ${Math.round(pipeline.averageStandardWaitMs)} ms`
              : ""}
          </span>
        </div>

        <div className="pipeline-lane pipeline-lane-critical">
          <span className="pipeline-lane-label">Critical lane · priority</span>
          <strong className="pipeline-lane-depth">{pipeline?.criticalQueueDepth ?? "—"}</strong>
          <span className="pipeline-lane-meta">
            {pipeline
              ? `${formatNumber(pipeline.criticalProcessed)} processed · avg wait ${Math.round(pipeline.averageCriticalWaitMs)} ms`
              : ""}
          </span>
          {pipeline && pipeline.bypassedStandard > 0 && (
            <span className="pipeline-bypass" title="Standard packets that were still queued when a critical packet was processed">
              ⤴ bypassed {formatNumber(pipeline.bypassedStandard)} queued standard packets
              {pipeline.lastCriticalBypassed > 0 && ` (last: ${pipeline.lastCriticalBypassed})`}
            </span>
          )}
        </div>
      </div>

      <div className="pipeline-counters">
        <span title="Repeat breach and disconnect alerts the sets recognised and dropped">
          <strong>{pipeline ? formatNumber(pipeline.duplicatesSuppressed) : "—"}</strong>{" "}
          duplicates suppressed
        </span>
        <span title="Oldest routine packets shed because the standard queue was full">
          <strong className={pipeline && pipeline.dropped > 0 ? "pipeline-dropped" : undefined}>
            {pipeline ? formatNumber(pipeline.dropped) : "—"}
          </strong>{" "}
          shed by backpressure
        </span>
        <span>
          <strong>{pipeline ? formatNumber(pipeline.totalReceived) : "—"}</strong> packets received
        </span>
      </div>

      {changes?.compared &&
        (changes.newlyDisconnected.length > 0 || changes.recovered.length > 0) && (
          <div className="pipeline-diff" role="status">
            <span className="pipeline-diff-title">Since your last refresh</span>
            {changes.newlyDisconnected.length > 0 && (
              <span className="pipeline-diff-down">
                ↓ lost {changes.newlyDisconnected.join(", ")}
              </span>
            )}
            {changes.recovered.length > 0 && (
              <span className="pipeline-diff-up">↑ back {changes.recovered.join(", ")}</span>
            )}
            <span className="pipeline-diff-meta" title="current \ previous, previous \ current, current ∩ previous">
              {changes.stillDisconnected} still down
            </span>
          </div>
        )}

      <div className="pipeline-set">
        <h3 className="pipeline-set-title">
          Open error states <span>{pipeline?.errorStates.length ?? 0}</span>
        </h3>
        {pipeline && pipeline.errorStates.length > 0 ? (
          <ul className="pipeline-chips">
            {pipeline.errorStates.map((state) => (
              <li key={`${state.nodeId}-${state.alertType}-${state.readingType}`}>
                <button
                  type="button"
                  className={`pipeline-chip severity-${state.severity.toLowerCase()}`}
                  onClick={() => onInspect(state.nodeId)}
                  title={`Since ${formatTime(state.sinceUtc)}${state.suppressedCount > 0 ? ` · ${state.suppressedCount} repeat warnings suppressed` : ""}`}
                >
                  {state.nodeId} · {state.readingType} {state.direction === "Low" ? "↓" : "↑"}
                  {state.suppressedCount > 0 && (
                    <span className="pipeline-chip-dupes">×{state.suppressedCount} suppressed</span>
                  )}
                </button>
              </li>
            ))}
          </ul>
        ) : (
          <p className="pipeline-empty">Every reporting node is within its limits.</p>
        )}
      </div>

      <div className="pipeline-set">
        <h3 className="pipeline-set-title">
          Disconnected nodes <span>{pipeline?.disconnectedNodes.length ?? 0}</span>
          {changes && changes.needsAttention.length > 0 && (
            <em className="pipeline-attention" title="Disconnected ∪ nodes with a critical open breach">
              {changes.needsAttention.length} need attention
            </em>
          )}
        </h3>
        {pipeline && pipeline.disconnectedNodes.length > 0 ? (
          <ul className="pipeline-chips">
            {pipeline.disconnectedNodes.map((node) => (
              <li key={node.nodeId}>
                <button
                  type="button"
                  className={`pipeline-chip severity-offline${changes?.newlyDisconnected.includes(node.nodeId) ? " is-new" : ""}`}
                  onClick={() => onInspect(node.nodeId)}
                  title={node.sinceUtc ? `Lost ${formatRelative(node.sinceUtc)}` : undefined}
                >
                  {node.nodeId}
                  {node.suppressedCount > 0 && (
                    <span className="pipeline-chip-dupes">×{node.suppressedCount} suppressed</span>
                  )}
                </button>
              </li>
            ))}
          </ul>
        ) : (
          <p className="pipeline-empty">All nodes are on the mesh.</p>
        )}
      </div>

      <div className="pipeline-set">
        <h3 className="pipeline-set-title">Recent pipeline alerts</h3>
        {pipeline && pipeline.recentAlerts.length > 0 ? (
          <ul className="pipeline-alerts">
            {pipeline.recentAlerts.slice(0, 6).map((alert) => (
              <li
                key={alert.id}
                className={`pipeline-alert severity-${alert.severity.toLowerCase()}`}
              >
                <span className={`pipeline-alert-lane lane-${alert.lane.toLowerCase()}`}>
                  {alert.lane}
                </span>
                <span className="pipeline-alert-message">
                  <strong>{alert.nodeId}</strong> {alert.message}
                  {alert.autoCommandSummary && (
                    <em className="pipeline-alert-auto"> → auto: {alert.autoCommandSummary}</em>
                  )}
                </span>
                <span className="pipeline-alert-wait">{alert.queueWaitMs} ms</span>
              </li>
            ))}
          </ul>
        ) : (
          <p className="pipeline-empty">No alerts raised since start-up.</p>
        )}
      </div>

      <div className="pipeline-simulate">
        <div className="pipeline-simulate-buttons">
          <button
            type="button"
            className="override-btn override-btn-reset"
            onClick={() => simulate("spike")}
            disabled={sending !== null}
            title="Posts 20 routine packets and one severe power spike to the intake"
          >
            {sending === "spike" ? "Sending…" : "Simulate power spike"}
          </button>
          <button
            type="button"
            className="override-btn override-btn-reset"
            onClick={() => simulate("flood")}
            disabled={sending !== null || !pipeline}
            title="Posts more routine packets than the standard queue holds, plus one spike"
          >
            {sending === "flood" ? "Flooding…" : "Flood standard lane"}
          </button>
        </div>

        {result && !error && (
          <p className="pipeline-simulate-result" role="status">
            {result.data.queuedStandard} routine packets queued
            {result.data.droppedStandard > 0 && `, ${formatNumber(result.data.droppedStandard)} oldest shed by backpressure`};{" "}
            {result.data.processedCritical} critical processed immediately
            {result.data.bypassedStandard > 0 && `, overtaking ${formatNumber(result.data.bypassedStandard)} queued packets`}
            {result.data.criticalAlerts[0] ? ` (waited ${result.data.criticalAlerts[0].queueWaitMs} ms)` : ""}.
          </p>
        )}

        {error && (
          <p className="override-result override-result-error" role="alert">
            <strong>Rejected.</strong> {error}
          </p>
        )}
      </div>
    </section>
  );
}

export default IngestPipeline;
