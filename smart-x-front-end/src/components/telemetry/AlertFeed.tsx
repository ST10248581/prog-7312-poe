import type { Alert, SensorListItem } from "../../services/apiService";
import { formatRelative, humanise } from "../../utils/format";

interface AlertFeedProps {
  alerts: Alert[];
  sensors: SensorListItem[];
  totalActive: number;
  onSelectSensor: (id: string) => void;
}

/**
 * Prioritised, not exhaustive. The API returns alerts severity-first and capped;
 * the footer states what is being withheld so the cap is visible rather than
 * silently hiding events.
 */
function AlertFeed({ alerts, sensors, totalActive, onSelectSensor }: AlertFeedProps) {
  const nodeFor = (sensorProfileId: string) =>
    sensors.find((sensor) => sensor.id === sensorProfileId)?.nodeId ?? "unknown node";

  return (
    <section className="alert-feed" aria-label="Active alerts">
      <header className="panel-head">
        <h2>Priority alerts</h2>
        <span className="panel-head-count">{totalActive} active</span>
      </header>

      {alerts.length === 0 ? (
        <p className="panel-empty">Nothing active. The mesh is behaving.</p>
      ) : (
        <ul className="alert-list">
          {alerts.map((alert) => (
            <li key={alert.id}>
              <button
                type="button"
                className={`alert-item severity-${alert.severity.toLowerCase()}`}
                onClick={() => onSelectSensor(alert.sensorProfileId)}
              >
                <div className="alert-item-head">
                  <span className={`alert-severity severity-${alert.severity.toLowerCase()}`}>
                    {alert.severity}
                  </span>
                  <span className="alert-type">{humanise(alert.alertType)}</span>
                  <span className="alert-time">{formatRelative(alert.triggeredUtc)}</span>
                </div>
                <p className="alert-message">{alert.message}</p>
                <span className="alert-node">{nodeFor(alert.sensorProfileId)}</span>
              </button>
            </li>
          ))}
        </ul>
      )}

      {totalActive > alerts.length && (
        <footer className="alert-feed-foot">
          Showing the {alerts.length} highest-severity of {totalActive} active alerts.
          Lower-severity events stay in the log rather than the feed.
        </footer>
      )}
    </section>
  );
}

export default AlertFeed;
