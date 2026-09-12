import { useCallback, useEffect, useState } from "react";
import {
  compareLoad,
  getAggregateLoad,
  getDeployment,
  getZoneLoad,
  ingestBatches,
} from "../../services/apiService";
import type {
  AggregateLoad,
  DeploymentValidationReport,
  LoadComparison,
  TelemetryIngestResult,
  TelemetrySample,
} from "../../services/apiService";
import DeploymentTree from "./DeploymentTree";
import StatTile from "./StatTile";
import { formatNumber, humanise } from "../../utils/format";

const MAX_ISSUES_SHOWN = 6;

/** Keeps the aggregate query well inside the server request-line limit. */
const MAX_LOAD_IDS = 120;

/**
 * A gateway buffer flush: three sequential batches of differing length, one lost
 * sample sent as "NaN" so the positions stay aligned, and one value pushed past
 * the 4.0 kW threshold so the anomaly counter has something to report.
 */
function buildDemoBatches(): TelemetrySample[][] {
  const sample = () => Number((1.2 + Math.random() * 1.6).toFixed(2));
  return [
    [sample(), sample(), sample()],
    [sample(), "NaN", 9.9, sample()],
    [sample(), sample()],
  ];
}

interface MeshInsightsProps {
  /** Set when exactly one zone is filtered, so the panel scopes to that branch. */
  zone?: string;
  sensorIds: string[];
  onSelectSensor: (sensorProfileId: string) => void;
  onIngested: () => void;
}

/**
 * Mesh-level view: the deployment hierarchy the sensors are registered into,
 * the aggregate draw of the metered devices in scope, and a gateway ingest
 * probe. All three are served by the central SmartXTelemetryEngine.
 */
function MeshInsights({
  zone,
  sensorIds,
  onSelectSensor,
  onIngested,
}: MeshInsightsProps) {
  const [report, setReport] = useState<DeploymentValidationReport | null>(null);
  const [load, setLoad] = useState<AggregateLoad | null>(null);
  const [comparison, setComparison] = useState<LoadComparison | null>(null);
  const [ingest, setIngest] = useState<TelemetryIngestResult | null>(null);

  const [overrides, setOverrides] = useState<Record<string, boolean>>({});
  const [loading, setLoading] = useState(true);
  const [ingesting, setIngesting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Joined so the effect depends on the contents of the selection rather than on
  // the identity of a fresh array on every render.
  const sensorKey = sensorIds.join(",");

  const loadMesh = useCallback(async () => {
    try {
      const ids = sensorKey ? sensorKey.split(",") : [];

      const [deploymentData, loadData] = await Promise.all([
        getDeployment(zone),
        zone
          ? getZoneLoad(zone)
          : ids.length > 0
            ? getAggregateLoad(ids.slice(0, MAX_LOAD_IDS))
            : Promise.resolve(null),
      ]);

      setReport(deploymentData);
      setLoad(loadData);

      // Delta between the two heaviest meters — the contributors come back
      // sorted, so this is the comparison worth showing without a picker.
      if (loadData && loadData.contributors.length >= 2) {
        setComparison(
          await compareLoad(
            loadData.contributors[0].sensorProfileId,
            loadData.contributors[1].sensorProfileId
          )
        );
      } else {
        setComparison(null);
      }

      setError(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Unable to reach the mesh API");
    } finally {
      setLoading(false);
    }
  }, [zone, sensorKey]);

  useEffect(() => {
    // oxlint-disable-next-line react/set-state-in-effect
    loadMesh();
  }, [loadMesh]);

  const handleToggle = useCallback((path: string) => {
    setOverrides((current) => ({
      ...current,
      [path]: !(current[path] ?? path.split(" -> ").length <= 2),
    }));
  }, []);

  const target = load?.contributors[0] ?? null;

  const handleIngest = useCallback(async () => {
    if (!target) {
      return;
    }

    setIngesting(true);
    try {
      const result = await ingestBatches(target.sensorProfileId, {
        readingType: "Power",
        intervalSeconds: 300,
        batches: buildDemoBatches(),
      });

      setIngest(result);
      setError(null);
      await loadMesh();
      onIngested();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Ingest failed");
    } finally {
      setIngesting(false);
    }
  }, [target, loadMesh, onIngested]);

  const criticalCount =
    report?.issues.filter((issue) => issue.severity === "Critical").length ?? 0;

  const issueTone = criticalCount > 0 ? "danger" : (report?.issues.length ?? 0) > 0 ? "warning" : "accent";

  const scopeLabel = zone ?? "current selection";

  return (
    <section className="mesh-section" aria-label="Mesh topology and deployment">
      <header className="panel-head">
        <h2>Mesh topology and deployment</h2>
        <span className="panel-head-count">
          {loading ? "loading…" : `scope: ${scopeLabel}`}
        </span>
      </header>

      {error && <p className="mesh-error">{error}</p>}

      {/* Recursion: the validator walks the nested tree and reports on it. */}
      <div className="stat-row">
        <StatTile
          label="Deployment nodes"
          value={report ? report.nodesVisited : "—"}
          hint={report ? `${report.maxDepthReached} tiers deep` : undefined}
        />
        <StatTile
          label="Sensors placed"
          value={report ? report.sensorsPlaced : "—"}
          hint="Bound to a registered profile"
        />
        <StatTile
          label="Config issues"
          value={report ? report.issues.length : "—"}
          tone={issueTone}
          hint={report ? `${criticalCount} critical` : undefined}
        />
        <StatTile
          label="Profile validation"
          value={report ? (report.isValid ? "Pass" : "Fail") : "—"}
          tone={report?.isValid === false ? "danger" : "accent"}
          hint="Facility → Zone → Sub-Zone → Node"
        />
      </div>

      <div className="mesh-grid">
        <div className="mesh-tree-panel">
          <header className="mesh-sub-head">
            <h3>Deployment hierarchy</h3>
            <span className="mesh-sub-note">Select a node to investigate it</span>
          </header>

          {report?.root ? (
            <DeploymentTree
              root={report.root}
              overrides={overrides}
              issuePaths={new Set(report.issues.map((issue) => issue.path))}
              onToggle={handleToggle}
              onSelectSensor={onSelectSensor}
            />
          ) : (
            <p className="panel-empty">
              {loading ? "Building the deployment tree…" : "No deployment data."}
            </p>
          )}

          {report && report.issues.length > 0 && (
            <div className="mesh-issues">
              <h4>Validation findings</h4>
              <ul>
                {report.issues.slice(0, MAX_ISSUES_SHOWN).map((issue, index) => (
                  <li key={`${issue.path}-${issue.kind}-${index}`}>
                    <span
                      className={`issue-severity severity-${issue.severity.toLowerCase()}`}
                    >
                      {issue.severity}
                    </span>
                    <div className="issue-body">
                      <span className="issue-kind">{humanise(issue.kind)}</span>
                      <span className="issue-path">{issue.path}</span>
                    </div>
                  </li>
                ))}
              </ul>
              {report.issues.length > MAX_ISSUES_SHOWN && (
                <p className="mesh-sub-note">
                  Showing the {MAX_ISSUES_SHOWN} highest-severity of{" "}
                  {report.issues.length} findings.
                </p>
              )}
            </div>
          )}
        </div>

        <div className="mesh-side">
          {/* Operator overloading: the aggregate is a fold with `+`, the delta a `-`. */}
          <header className="mesh-sub-head">
            <h3>Metered load</h3>
            <span className="mesh-sub-note">{load ? load.scope : scopeLabel}</span>
          </header>

          <div className="mesh-stat-column">
            <StatTile
              label="Aggregate draw"
              value={load && load.meterCount > 0 ? formatNumber(load.totalValue, 2) : "—"}
              unit={load?.unit || undefined}
              tone="accent"
              hint={
                load && load.meterCount > 0
                  ? `${load.meterCount} meters summed`
                  : "No metered devices in scope"
              }
            />

            <StatTile
              label="Average per meter"
              value={load && load.meterCount > 0 ? formatNumber(load.averageValue, 2) : "—"}
              unit={load?.unit || undefined}
            />

            {comparison && (
              <StatTile
                label="Heaviest two, compared"
                value={formatNumber(Math.abs(comparison.delta), 2)}
                unit={comparison.unit}
                tone={comparison.areEquivalent ? "default" : "warning"}
                hint={`${comparison.left.nodeId} vs ${comparison.right.nodeId} · ${formatNumber(
                  Math.abs(comparison.deltaPercent),
                  1
                )}%`}
              >
                <p className="mesh-summary">{comparison.summary}</p>
              </StatTile>
            )}
          </div>

          {/* Generics + jagged arrays: a gateway flush, end to end. */}
          <header className="mesh-sub-head">
            <h3>Gateway ingest</h3>
            <button
              type="button"
              className="register-btn"
              disabled={!target || ingesting}
              onClick={handleIngest}
            >
              {ingesting ? "Flushing…" : "Simulate flush"}
            </button>
          </header>

          {target ? (
            <p className="mesh-sub-note">
              Sends three ragged batches to {target.nodeId}, including one lost
              sample and one over-threshold reading.
            </p>
          ) : (
            <p className="mesh-sub-note">
              No metered device in scope to ingest against.
            </p>
          )}

          {ingest && (
            <>
              <div className="mesh-stat-column">
                <StatTile
                  label="Batches accepted"
                  value={ingest.acceptedCount}
                  unit={`/ ${ingest.rawSampleCount}`}
                  hint={`${ingest.batchCount} batches · ${ingest.rejectedCount} rejected`}
                />
                <StatTile
                  label="Payload type"
                  value={ingest.payloadType}
                  hint={`${ingest.anomalyCount} anomalies · ${ingest.processingMs} ms`}
                />
              </div>

              <table className="mesh-batch-table">
                <thead>
                  <tr>
                    <th>Batch</th>
                    <th>n</th>
                    <th>OK</th>
                    <th>Rej</th>
                    <th>Anom</th>
                    <th>Min</th>
                    <th>Max</th>
                    <th>Mean</th>
                  </tr>
                </thead>
                <tbody>
                  {ingest.batchStatistics.map((statistic) => (
                    <tr key={statistic.batchIndex}>
                      <td>{statistic.batchIndex}</td>
                      <td>{statistic.sampleCount}</td>
                      <td>{statistic.acceptedCount}</td>
                      <td>{statistic.rejectedCount}</td>
                      <td>{statistic.anomalyCount}</td>
                      <td>{statistic.minValue}</td>
                      <td>{statistic.maxValue}</td>
                      <td>{statistic.meanValue}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </>
          )}
        </div>
      </div>
    </section>
  );
}

export default MeshInsights;
