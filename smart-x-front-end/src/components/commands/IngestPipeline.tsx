import { useState } from "react";
import { formatNumber, formatRelative, formatTime } from "../../utils/format";
import type { PacketIntakeResult, PipelineStatus } from "../../services/apiService";

interface IngestPipelineProps {
  pipeline: PipelineStatus | null;
  /** Sends a routine burst plus one critical spike, to show the priority lane overtaking. */
  onSimulateSpike: () => Promise<PacketIntakeResult>;
  onInspect: (nodeId: string) => void;
}

/**
 * The telemetry intake behind the command stream, made visible.
 *
 * Two lanes: routine packets queue first-in, first-out and drain at a fixed
 * budget per tick; critical ones go to a priority queue that is emptied at
 * once. Under them are the two sets that keep alerts unique: open error states
 * and disconnected nodes. The duplicates counter is how many repeat alerts
 * those sets have absorbed.
 */
function IngestPipeline({ pipeline, onSimulateSpike, onInspect }: IngestPipelineProps) {
  const [sending, setSending] = useState(false);
  const [result, setResult] = useState<PacketIntakeResult | null>(null);
  const [error, setError] = useState<string | null>(null);

  const handleSimulate = async () => {
    setSending(true);
    setError(null);

    try {
      setResult(await onSimulateSpike());
    } catch (err) {
      setError(err instanceof Error ? err.message : "The intake rejected the packets.");
    } finally {
      setSending(false);
    }
  };

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
          <strong className="pipeline-lane-depth">{pipeline?.standardQueueDepth ?? "—"}</strong>
          <span className="pipeline-lane-meta">
            {pipeline
              ? `${formatNumber(pipeline.standardProcessed)} processed · avg wait ${Math.round(pipeline.averageStandardWaitMs)} ms`
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
        </div>
      </div>

      <div className="pipeline-counters">
        <span title="Repeat breach and disconnect alerts the sets recognised and dropped">
          <strong>{pipeline ? formatNumber(pipeline.duplicatesSuppressed) : "—"}</strong>{" "}
          duplicates suppressed
        </span>
        <span>
          <strong>{pipeline ? formatNumber(pipeline.totalReceived) : "—"}</strong> packets received
        </span>
      </div>

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
                  title={`Since ${formatTime(state.sinceUtc)}`}
                >
                  {state.nodeId} · {state.readingType} {state.direction === "Low" ? "↓" : "↑"}
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
        </h3>
        {pipeline && pipeline.disconnectedNodes.length > 0 ? (
          <ul className="pipeline-chips">
            {pipeline.disconnectedNodes.map((node) => (
              <li key={node.nodeId}>
                <button
                  type="button"
                  className="pipeline-chip severity-offline"
                  onClick={() => onInspect(node.nodeId)}
                  title={node.sinceUtc ? `Lost ${formatRelative(node.sinceUtc)}` : undefined}
                >
                  {node.nodeId}
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
        <button
          type="button"
          className="override-btn override-btn-reset"
          onClick={handleSimulate}
          disabled={sending}
          title="Posts 20 routine packets and one severe power spike to the intake"
        >
          {sending ? "Sending…" : "Simulate power spike"}
        </button>

        {result && !error && (
          <p className="pipeline-simulate-result" role="status">
            {result.queuedStandard} routine packets queued behind {Math.max(0, result.standardQueueDepth - result.queuedStandard)} others;{" "}
            {result.processedCritical} critical processed immediately
            {result.criticalAlerts[0] ? ` (waited ${result.criticalAlerts[0].queueWaitMs} ms)` : ""}.
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
