import { useCallback, useEffect, useRef, useState } from "react";
import {
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
import { formatCompact, formatNumber, formatTime } from "../utils/format";
import "./TelemetryPage.css";

const REFRESH_MS = 10_000;

function TelemetryPage() {
  const [summary, setSummary] = useState<EcosystemSummary | null>(null);
  const [sensors, setSensors] = useState<SensorListItem[]>([]);
  const [alerts, setAlerts] = useState<Alert[]>([]);
  const [options, setOptions] = useState<FilterOptions | null>(null);
  const [engagement, setEngagement] = useState<EngagementState | null>(null);
  const [detail, setDetail] = useState<SensorDetail | null>(null);

  const [filters, setFilters] = useState<SensorFilters>({});
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [liveRefresh, setLiveRefresh] = useState(true);
  const [loading, setLoading] = useState(true);
  const [detailLoading, setDetailLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [lastRefresh, setLastRefresh] = useState<Date | null>(null);
  const [showRegister, setShowRegister] = useState(false);

  // Tracks the most recently requested node so a slow response for a previously
  // selected sensor cannot overwrite the current one.
  const requestedIdRef = useRef<string | null>(null);

  // `loading` covers the first paint only; live refreshes swap data in place
  // rather than flashing the panel back to a loading state.
  const loadDashboard = useCallback(async (activeFilters: SensorFilters) => {
    try {
      const [summaryData, sensorData, alertData] = await Promise.all([
        getSummary(),
        getSensors(activeFilters),
        getAlerts("Active", 12),
      ]);

      setSummary(summaryData);
      setSensors(sensorData);
      setAlerts(alertData);
      setLastRefresh(new Date());
      setError(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Unable to reach the Smart-X API");
    } finally {
      setLoading(false);
    }
  }, []);

  // Static lookups, fetched once.
  useEffect(() => {
    getFilterOptions().then(setOptions).catch(() => undefined);
    getEngagement().then(setEngagement).catch(() => undefined);
  }, []);

  // Refetch whenever the filters change. The rule below sees setState inside
  // loadDashboard and assumes it runs synchronously; every call sits after an
  // await, and fetching from the API is exactly the external-system case the
  // rule carves out.
  useEffect(() => {
    // oxlint-disable-next-line react/set-state-in-effect
    loadDashboard(filters);
  }, [filters, loadDashboard]);

  // Live polling: the real-time feedback loop.
  useEffect(() => {
    if (!liveRefresh) {
      return;
    }

    const timer = window.setInterval(() => loadDashboard(filters), REFRESH_MS);
    return () => window.clearInterval(timer);
  }, [liveRefresh, filters, loadDashboard]);

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
  }, []);

  const handleCloseDetail = useCallback(() => {
    requestedIdRef.current = null;
    setSelectedId(null);
    setDetail(null);
  }, []);

  const handlePayloadUpdated = useCallback((updatedDetail: SensorDetail) => {
    setDetail(updatedDetail);
    // Refresh the sensor grid so cards reflect the updated fields
    loadDashboard(filters);
  }, [loadDashboard, filters]);

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
  }, [loadDashboard, filters]);

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
        <div className="page-error">
          <strong>API unreachable.</strong> {error} — start the backend with{" "}
          <code>dotnet run</code> in <code>smart-x-backend/SmartX.Api</code>.
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
            <p className="panel-empty">No sensors match the current filters.</p>
          )}
        </section>

        <AlertFeed
          alerts={alerts}
          sensors={sensors}
          totalActive={summary?.activeAlertCount ?? 0}
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
        />
      )}

      {showRegister && (
        <RegisterSensorModal
          onClose={() => setShowRegister(false)}
          onRegistered={handleSensorRegistered}
        />
      )}
    </div>
  );
}

export default TelemetryPage;
