import type { SensorListItem } from "../../services/apiService";
import { formatRelative, humanise } from "../../utils/format";
import Sparkline from "./Sparkline";

interface SensorCardProps {
  sensor: SensorListItem;
  selected: boolean;
  onSelect: (id: string) => void;
}

function SensorCard({ sensor, selected, onSelect }: SensorCardProps) {
  const status = sensor.status.toLowerCase();

  return (
    <button
      type="button"
      className={`sensor-card status-${status}${selected ? " selected" : ""}`}
      onClick={() => onSelect(sensor.id)}
      aria-pressed={selected}
    >
      <header className="sensor-card-head">
        <span className={`status-dot status-${status}`} aria-hidden="true" />
        <span className="sensor-card-node">{sensor.nodeId}</span>
        <span className="sensor-card-status">{sensor.status}</span>
      </header>

      <div className="sensor-card-name">{sensor.name}</div>

      <div className="sensor-card-reading">
        {sensor.latestValue === null ? (
          <span className="sensor-card-value muted">—</span>
        ) : (
          <>
            <span className="sensor-card-value">{sensor.latestValue.toFixed(1)}</span>
            <span className="sensor-card-unit">{sensor.latestUnit}</span>
          </>
        )}
        <Sparkline values={sensor.sparkline} status={sensor.status} />
      </div>

      <footer className="sensor-card-foot">
        <span className="sensor-card-meta">
          {sensor.zone} · {sensor.room}
        </span>
        <span className="sensor-card-meta">{humanise(sensor.category)}</span>
      </footer>

      <div className="sensor-card-badges">
        {sensor.activeAlertCount > 0 && (
          <span className="badge badge-danger">{sensor.activeAlertCount} alerts</span>
        )}
        {sensor.anomalyCountLast24h > 0 && (
          <span className="badge badge-warning">{sensor.anomalyCountLast24h} anomalies</span>
        )}
        <span className="badge badge-muted">{formatRelative(sensor.lastSeenUtc)}</span>
      </div>
    </button>
  );
}

export default SensorCard;
