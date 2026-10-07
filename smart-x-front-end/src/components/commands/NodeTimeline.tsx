import { formatTime, humanise } from "../../utils/format";
import type { NodeTimeline as NodeTimelineData, TimelineSeries } from "../../services/apiService";

interface NodeTimelineProps {
  nodeId: string;
  timeline: NodeTimelineData | null;
  error: string | null;
  /** Window to read, in minutes; the API binary-searches the log for its start. */
  windowMinutes: number;
  onWindowChange: (minutes: number) => void;
}

/** Windows the timeline can show. Shorter windows make the range read's skip obvious. */
const TIMELINE_WINDOWS = [5, 15, 60, 180];

const WIDTH = 760;
const CHART_HEIGHT = 120;
const PAD_LEFT = 48;
const PAD_RIGHT = 12;
const PAD_TOP = 10;
const PAD_BOTTOM = 18;
const EVENT_STRIP = 22;

const PLOT_WIDTH = WIDTH - PAD_LEFT - PAD_RIGHT;
const PLOT_HEIGHT = CHART_HEIGHT - PAD_TOP - PAD_BOTTOM;

/**
 * The selected node's log over the last hour: one strip per metric, with its
 * limits drawn in, and a shared event rail below for commands, alerts and link
 * changes.
 *
 * The API reads this out of the node's SortedList with a range read: a binary
 * search finds the first entry inside the window and everything older is
 * skipped without being visited. The points arrive in timestamp order and
 * already thinned, so the chart maps them straight to coordinates, with no
 * sorting or bucketing in the browser. The read's cost is shown under the title.
 */
function NodeTimeline({ nodeId, timeline, error, windowMinutes, onWindowChange }: NodeTimelineProps) {
  if (error) {
    return (
      <section className="node-timeline" aria-label={`Timeline for ${nodeId}`}>
        <header className="panel-head">
          <h2>Node timeline · {nodeId}</h2>
        </header>
        <p className="panel-empty">{error}</p>
      </section>
    );
  }

  if (!timeline) {
    return (
      <section className="node-timeline" aria-label={`Timeline for ${nodeId}`}>
        <header className="panel-head">
          <h2>Node timeline · {nodeId}</h2>
        </header>
        <p className="panel-empty">Loading the node's log…</p>
      </section>
    );
  }

  const from = new Date(timeline.fromUtc).getTime();
  const to = new Date(timeline.toUtc).getTime();
  const span = Math.max(to - from, 1);
  const x = (iso: string) => PAD_LEFT + ((new Date(iso).getTime() - from) / span) * PLOT_WIDTH;

  return (
    <section className="node-timeline" aria-label={`Timeline for ${timeline.nodeId}`}>
      <header className="panel-head">
        <h2>
          Node timeline · {timeline.nodeId}{" "}
          <span className="node-timeline-name">{timeline.sensorName}</span>
          {timeline.isDisconnected && <span className="node-timeline-offline">disconnected</span>}
        </h2>
        <div className="node-timeline-windows" role="group" aria-label="Timeline window">
          {TIMELINE_WINDOWS.map((minutes) => (
            <button
              key={minutes}
              type="button"
              className={`filter-chip${minutes === windowMinutes ? " active" : ""}`}
              aria-pressed={minutes === windowMinutes}
              onClick={() => onWindowChange(minutes)}
            >
              {minutes < 60 ? `${minutes} min` : `${minutes / 60} h`}
            </button>
          ))}
        </div>
      </header>

      <p className="node-timeline-range" title="Binary search over the sorted keys, then a forward read of the window">
        Range read: skipped <strong>{timeline.entriesSkipped.toLocaleString()}</strong> older entries,
        read <strong>{timeline.entriesInWindow.toLocaleString()}</strong> of {timeline.logSize.toLocaleString()} in{" "}
        <strong>{(timeline.rangeReadMicroseconds / 1000).toFixed(3)} ms</strong> · SortedList, O(log n + k)
      </p>

      {timeline.series.map((series) => (
        <SeriesChart key={series.readingType} series={series} x={x} />
      ))}

      <svg
        className="node-timeline-events"
        viewBox={`0 0 ${WIDTH} ${EVENT_STRIP + 16}`}
        role="img"
        aria-label={`${timeline.events.length} events`}
      >
        <line
          x1={PAD_LEFT}
          x2={WIDTH - PAD_RIGHT}
          y1={EVENT_STRIP / 2}
          y2={EVENT_STRIP / 2}
          className="timeline-rail"
        />
        {timeline.events.map((event) => (
          <g key={`${event.timestampUtc}-${event.kind}-${event.label}`}>
            <circle
              cx={x(event.timestampUtc)}
              cy={EVENT_STRIP / 2}
              r={event.kind === "Command" ? 3 : 4.5}
              className={`timeline-event event-${event.kind.toLowerCase()}`}
            >
              <title>
                {formatTime(event.timestampUtc)} · {humanise(event.kind)}: {event.label}
              </title>
            </circle>
          </g>
        ))}
        <text x={PAD_LEFT} y={EVENT_STRIP + 12} className="timeline-axis">
          {formatTime(timeline.fromUtc)}
        </text>
        <text x={WIDTH - PAD_RIGHT} y={EVENT_STRIP + 12} textAnchor="end" className="timeline-axis">
          now
        </text>
      </svg>

      <div className="node-timeline-legend">
        <span><i className="event-command" /> command</span>
        <span><i className="event-alert" /> alert</span>
        <span><i className="event-recovered" /> recovered</span>
        <span><i className="event-disconnected" /> link lost</span>
        <span><i className="event-reconnected" /> link restored</span>
      </div>
    </section>
  );
}

function SeriesChart({ series, x }: { series: TimelineSeries; x: (iso: string) => number }) {
  const values = series.points.map((point) => point.value);

  if (values.length === 0) {
    return (
      <div className="node-timeline-series">
        <span className="node-timeline-series-label">
          {series.readingType} ({series.unit})
        </span>
        <p className="panel-empty">No {series.readingType.toLowerCase()} readings in the window.</p>
      </div>
    );
  }

  // Limits take part in the scale so the lines are always on the chart.
  const candidates = [...values];
  if (series.minThreshold !== null) candidates.push(series.minThreshold);
  if (series.maxThreshold !== null) candidates.push(series.maxThreshold);

  const rawMin = Math.min(...candidates);
  const rawMax = Math.max(...candidates);
  const padding = (rawMax - rawMin || 1) * 0.1;
  const min = rawMin - padding;
  const max = rawMax + padding;
  const y = (value: number) => PAD_TOP + PLOT_HEIGHT - ((value - min) / (max - min)) * PLOT_HEIGHT;

  const path = series.points
    .map((point, index) => `${index === 0 ? "M" : "L"} ${x(point.timestampUtc).toFixed(1)} ${y(point.value).toFixed(1)}`)
    .join(" ");

  const outOfRange = (value: number) =>
    (series.minThreshold !== null && value < series.minThreshold) ||
    (series.maxThreshold !== null && value > series.maxThreshold);

  const latest = series.points[series.points.length - 1];

  return (
    <div className="node-timeline-series">
      <span className="node-timeline-series-label">
        {series.readingType} ({series.unit})
        <strong className={outOfRange(latest.value) ? "is-breach" : undefined}>
          {latest.value.toFixed(2)}
        </strong>
      </span>

      <svg viewBox={`0 0 ${WIDTH} ${CHART_HEIGHT}`} className="node-timeline-svg" role="img"
        aria-label={`${series.readingType} over time`}>
        {[series.minThreshold, series.maxThreshold].map((limit, index) =>
          limit === null ? null : (
            <g key={index}>
              <line x1={PAD_LEFT} x2={WIDTH - PAD_RIGHT} y1={y(limit)} y2={y(limit)} className="timeline-limit" />
              <text x={PAD_LEFT - 6} y={y(limit) + 3} textAnchor="end" className="timeline-axis">
                {limit.toFixed(1)}
              </text>
            </g>
          ),
        )}

        <path d={path} className="timeline-line" />

        {series.points.filter((point) => outOfRange(point.value)).map((point) => (
          <circle key={point.timestampUtc} cx={x(point.timestampUtc)} cy={y(point.value)} r={2.5}
            className="timeline-breach">
            <title>{formatTime(point.timestampUtc)} · {point.value.toFixed(2)} {series.unit}</title>
          </circle>
        ))}
      </svg>
    </div>
  );
}

export default NodeTimeline;
