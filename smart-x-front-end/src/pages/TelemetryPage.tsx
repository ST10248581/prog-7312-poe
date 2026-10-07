import { useCallback, useEffect, useRef, useState } from "react";
import {
  ApiError,
  getAlerts,
  getEngagement,
  getFilterOptions,
  getSensorDetail,
  getSensors,
  getSummary,
} from "../services/apiService";
import type {
  Alert,
  EcosystemSummary,
  EngagementState,
  FilterOptions,
  SensorDetail,
  SensorFilters,
  SensorListItem,
} from "../services/apiService";
import AlertFeed from "../components/telemetry/AlertFeed";
import ConfigurationProgress from "../components/telemetry/ConfigurationProgress";
import FilterBar from "../components/telemetry/FilterBar";
import MeshInsights from "../components/telemetry/MeshInsights";
import SensorCard from "../components/telemetry/SensorCard";
import RegisterSensorModal from "../components/telemetry/RegisterSensorModal";
import SensorDetailModal from "../components/telemetry/SensorDetailModal";
import StatTile from "../components/telemetry/StatTile";
import { onApiReconnected } from "../services/apiStatus";
import { isAbortError, usePolling } from "../hooks/usePolling";
import { usePersistentState } from "../state/appState";
import { formatCompact, formatNumber, formatTime } from "../utils/format";
import "./TelemetryPage.css";

const REFRESH_MS = 10_000;

function TelemetryPage() {
  // Everything that should survive leaving the page lives in the app state
  // store rather than plain useState: the operator's choices (filters, live
  // toggle, the open sensor) and the last data shown, so coming back renders
  // the page exactly as it was while it refreshes. Choices are also kept in
  // sessionStorage, so a reload restores them too.
  const [summary, setSummary] = usePersistentState<EcosystemSummary | null>("telemetry.summary", null);
  const [sensors, setSensors] = usePersistentState<SensorListItem[]>("telemetry.sensors", []);
  const [alerts, setAlerts] = usePersistentState<Alert[]>("telemetry.alerts", []);
  const [options, setOptions] = usePersistentState<FilterOptions | null>("telemetry.options", null);
  const [engagement, setEngagement] = usePersistentState<EngagementState | null>("telemetry.engagement", null);
  const [detail, setDetail] = usePersistentState<SensorDetail | null>("telemetry.detail", null);

  const [filters, setFilters] = usePersistentState<SensorFilters>("telemetry.filters", {}, { session: true });
  const [selectedId, setSelectedId] = usePersistentState<string | null>("telemetry.selectedId", null);
  const [liveRefresh, setLiveRefresh] = usePersistentState("telemetry.live", true, { session: true });
  const [lastRefresh, setLastRefresh] = usePersistentState<Date | null>("telemetry.lastRefresh", null);

  const [loading, setLoading] = useState(summary === null);
  const [detailLoading, setDetailLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [showRegister, setShowRegister] = useState(false);

  // Tracks the most recently requested node so a slow response for a previously
  // selected sensor cannot overwrite the current one.
  const requestedIdRef = useRef<string | null>(null);

  // `loading` covers the first paint only; live refreshes swap data in place
  // rather than flashing the panel back to a loading state.
  // The signal is aborted when the filters change, polling stops or the page
  // unmounts, so a response that is no longer wanted is never applied.
  const loadDashboard = useCallback(async (activeFilters: SensorFilters, signal?: AbortSignal) => {
    try {
      const [summaryData, sensorData, alertData] = await Promise.all([
        getSummary(signal),
        getSensors(activeFilters, signal),
        getAlerts("Active", 12, signal),
      ]);
      if (signal?.aborted) return;

      setSummary(summaryData);
      setSensors(sensorData);
      setAlerts(alertData);
      setLastRefresh(new Date());
      setError(null);
    } catch (err) {
      if (isAbortError(err)) return;
      // An unreachable API is announced once, app-wide, by ApiStatusBanner;
      // only errors the API itself returned are shown here.
      setError(err instanceof ApiError && err.isUnreachable ? null : err instanceof Error ? err.message : "Request failed.");
    } finally {
      setLoading(false);
    }
  }, [setSummary, setSensors, setAlerts, setLastRefresh]);

  // Static lookups: fetched once, and again if the API was down at startup.
  const loadLookups = useCallback(() => {
    getFilterOptions().then(setOptions).catch(() => undefined);
    getEngagement().then(setEngagement).catch(() => undefined);
  }, [setOptions, setEngagement]);

  useEffect(() => {
    loadLookups();
  }, [loadLookups]);

  // The API has come back: refresh everything rather than wait for the next poll.
  useEffect(
    () =>
      onApiReconnected(() => {
        loadLookups();
        loadDashboard(filters);
      }),
    [loadLookups, loadDashboard, filters]
  );

  // The real-time feedback loop. Loads at once and whenever the filters change
  // (cancelling the request in flight), then every REFRESH_MS while live —
  // never overlapping a slow response, and resting while the tab is hidden.
  usePolling((signal) => loadDashboard(filters, signal), {
    intervalMs: REFRESH_MS,
    live: liveRefresh,
    key: JSON.stringify(filters),
  });

  // Details on demand, driven by the selection event rather than an effect.
  const handleSelect = useCallback(async (id: string) => {
    setSelectedId(id);
    setDetailLoading(true);
    requestedIdRef.current = id;

    try {
      const data = await getSensorDetail(id);
      if (requestedIdRef.current === id) {
        setDetail(data);
      }
    } catch {
      if (requestedIdRef.current === id) {
        setDetail(null);
      }
    } finally {
      if (requestedIdRef.current === id) {
        setDetailLoading(false);
      }
    }
  }, [setSelectedId, setDetail]);

  const handleCloseDetail = useCallback(() => {
    requestedIdRef.current = null;
    setSelectedId(null);
    setDetail(null);
  }, [setSelectedId, setDetail]);

  const handlePayloadUpdated = useCallback((updatedDetail: SensorDetail) => {
    setDetail(updatedDetail);
    // Refresh the sensor grid so cards reflect the updated fields
    loadDashboard(filters);
  }, [loadDashboard, filters, setDetail]);

  const handleDetailRefresh = useCallback(() => {
    if (selectedId) {
      handleSelect(selectedId);
    }
  }, [selectedId, handleSelect]);

  // A gateway flush adds readings, so the overview tiles and the selected
  // sensor's batch history both go stale.
  const handleMeshIngested = useCallback(() => {
    loadDashboard(filters);
    if (selectedId) {
      handleSelect(selectedId);
    }
  }, [loadDashboard, filters, selectedId, handleSelect]);

  const handleSensorRegistered = useCallback(() => {
    loadDashboard(filters);
    getFilterOptions().then(setOptions).catch(() => undefined);
  }, [loadDashboard, filters, setOptions]);

  const healthTone =
    !summary || summary.meshHealthScore >= 85
      ? "accent"
      : summary.meshHealthScore >= 65
        ? "warning"
        : "danger";

  return (
    <div className="telemetry-page">
      <header className="page-head">
        <div>
          <h1 className="page-title">Sensor Data Ingestion and Telemetry</h1>
          <p className="page-sub">
            Overview → filter → investigate → troubleshoot. Live readings, threshold
            markers and prioritised alerts across the Smart-X mesh.
          </p>
        </div>

        <div className="page-head-actions">
          <button
            type="button"
            className={`live-toggle${liveRefresh ? " active" : ""}`}
            onClick={() => setLiveRefresh((value) => !value)}
          >
            <span className="live-dot" />
            {liveRefresh ? "Live" : "Paused"}
          </button>
          <span className="page-refresh">
            {lastRefresh ? `Updated ${formatTime(lastRefresh.toISOString())}` : "Connecting…"}
          </span>
        </div>
      </header>

      {error && (
        <div className="page-error" role="alert">
          <strong>The dashboard could not refresh.</strong> {error}
        </div>
      )}

      {/* Overview first: whole-mesh health before any single node. */}
      <section className="stat-row" aria-label="Ecosystem overview">
        <StatTile
          label="Mesh health"
          value={summary ? summary.meshHealthScore.toFixed(1) : "—"}
          unit="%"
          tone={healthTone}
          progress={summary?.meshHealthScore}
          hint={summary ? `${summary.totalSensors} registered sensors` : undefined}
        />

        <StatTile label="Device status" value={summary ? summary.onlineCount : "—"} unit="online">
          <div className="status-breakdown">
            <span className="status-chip status-warning">
              {summary?.warningCount ?? 0} warning
            </span>
            <span className="status-chip status-offline">
              {summary?.offlineCount ?? 0} offline
            </span>
          </div>
        </StatTile>

        <StatTile
          label="Active alerts"
          value={summary ? summary.activeAlertCount : "—"}
          tone={summary && summary.criticalAlertCount > 0 ? "danger" : "default"}
          hint={summary ? `${summary.criticalAlertCount} critical` : undefined}
        />

        <StatTile
          label="Readings / hour"
          value={summary ? formatNumber(summary.readingsLastHour) : "—"}
          hint={summary ? `${formatCompact(summary.totalReadings)} total ingested` : undefined}
        />

        <StatTile
          label="Anomalies / hour"
          value={summary ? summary.anomaliesLastHour : "—"}
          tone={summary && summary.anomaliesLastHour > 0 ? "warning" : "default"}
          hint="Readings outside their threshold"
        />

        <StatTile
          label="Ingest success"
          value={summary ? summary.ingestSuccessRate.toFixed(1) : "—"}
          unit="%"
          progress={summary?.ingestSuccessRate}
          hint={summary ? `${summary.averageProcessingMs} ms average` : undefined}
        />
      </section>

      <ConfigurationProgress engagement={engagement} sensors={sensors} />

      {/* Filter: narrows every panel below in one move. */}
      <FilterBar
        options={options}
        filters={filters}
        resultCount={sensors.length}
        totalCount={summary?.totalSensors ?? 0}
        onChange={setFilters}
      />

      <div className="dashboard-grid">
        <section className="sensor-panel" aria-label="Sensors">
          <header className="panel-head">
            <h2>Mesh nodes</h2>
            <div className="panel-head-actions">
              <button
                type="button"
                className="register-btn"
                onClick={() => setShowRegister(true)}
              >
                + Register Device
              </button>
              <span className="panel-head-count">
                {loading ? "loading…" : `${sensors.length} shown`}
              </span>
            </div>
          </header>

          <div className="sensor-grid">
            {sensors.map((sensor) => (
              <SensorCard
                key={sensor.id}
                sensor={sensor}
                selected={sensor.id === selectedId}
                onSelect={handleSelect}
              />
            ))}
          </div>

          {!loading && sensors.length === 0 && (
            <p className="panel-empty">
              {lastRefresh ? "No sensors match the current filters." : "Waiting for the Smart-X API — sensors appear once it responds."}
            </p>
          )}
        </section>

        <AlertFeed
          alerts={alerts}
          sensors={sensors}
          totalActive={summary?.activeAlertCount ?? 0}
          loaded={lastRefresh !== null}
          onSelectSensor={handleSelect}
        />
      </div>

      {/* Mesh-level view: the deployment hierarchy behind the flat sensor grid,
          plus aggregate load and gateway ingest. */}
      <MeshInsights
        zone={filters.zones?.length === 1 ? filters.zones[0] : undefined}
        sensorIds={sensors.map((sensor) => sensor.id)}
        onSelectSensor={handleSelect}
        onIngested={handleMeshIngested}
      />

      {/* Investigate + troubleshoot, opened on demand over the overview. */}
      {selectedId && (
        <SensorDetailModal
          key={selectedId}
          detail={detail}
          loading={detailLoading}
          onClose={handleCloseDetail}
          onPayloadUpdated={handlePayloadUpdated}
          onDetailRefresh={handleDetailRefresh}
          zones={options?.zones}
        />
      )}

      {showRegister && (
        <RegisterSensorModal
          zones={options?.zones}
          onClose={() => setShowRegister(false)}
          onRegistered={handleSensorRegistered}
        />
      )}
    </div>
  );
}

export default TelemetryPage;
