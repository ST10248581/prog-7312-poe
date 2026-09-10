import type { EngagementState, SensorListItem } from "../../services/apiService";

interface ConfigurationProgressProps {
  engagement: EngagementState | null;
  sensors: SensorListItem[];
}

/**
 * Gamification, deliberately limited to configuration completeness. It scores
 * setup work the user controls — never alert volume or uptime, which would turn
 * operational noise into a score to chase.
 */
function ConfigurationProgress({ engagement, sensors }: ConfigurationProgressProps) {
  if (!engagement) {
    return null;
  }

  const documented = sensors.filter((sensor) => sensor.attachmentCount > 0).length;
  const documentedPercent = sensors.length === 0 ? 0 : (documented / sensors.length) * 100;

  const tasks = [
    {
      label: "Sensors registered",
      done: engagement.sensorsRegistered > 0,
      detail: `${engagement.sensorsRegistered} profiles`,
    },
    {
      label: "Telemetry flowing",
      done: engagement.readingsIngested > 0,
      detail: `${engagement.readingsIngested.toLocaleString()} readings`,
    },
    {
      label: "Profiles documented",
      done: documentedPercent >= 100,
      detail: `${documented}/${sensors.length} with attachments`,
    },
    {
      label: "Alerts triaged",
      done: engagement.alertsResolved > 0,
      detail: `${engagement.alertsResolved} resolved`,
    },
  ];

  const complete = tasks.filter((task) => task.done).length;
  const percent = (complete / tasks.length) * 100;

  return (
    <section className="config-progress" aria-label="Configuration progress">
      <div className="config-progress-head">
        <div>
          <span className="config-progress-title">Configuration progress</span>
          <span className="config-progress-sub">
            Setup completeness for <code>{engagement.userId}</code>
          </span>
        </div>
        <span className="config-progress-score">
          {complete}
          <span className="config-progress-total">/{tasks.length}</span>
        </span>
      </div>

      <div className="config-progress-bar">
        <div className="config-progress-fill" style={{ width: `${percent}%` }} />
      </div>

      <ul className="config-task-list">
        {tasks.map((task) => (
          <li key={task.label} className={task.done ? "done" : undefined}>
            <span className="config-task-check">{task.done ? "✓" : "○"}</span>
            <span className="config-task-label">{task.label}</span>
            <span className="config-task-detail">{task.detail}</span>
          </li>
        ))}
      </ul>
    </section>
  );
}

export default ConfigurationProgress;
