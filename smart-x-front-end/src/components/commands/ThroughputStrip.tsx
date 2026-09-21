interface ThroughputStripProps {
  /** Commands per minute, oldest first. */
  values: number[];
  windowLabel: string;
}

const WIDTH = 600;
const HEIGHT = 64;
const GAP = 2;

/**
 * Dispatch rate over the selected window. Bars rather than a line: each value
 * is a count for a discrete minute, not a sample of something continuous.
 */
function ThroughputStrip({ values, windowLabel }: ThroughputStripProps) {
  if (values.length === 0) {
    return <p className="panel-empty">No dispatch activity in this window.</p>;
  }

  const peak = Math.max(...values);
  const barWidth = WIDTH / values.length;
  const average = values.reduce((total, value) => total + value, 0) / values.length;

  return (
    <div className="throughput-strip">
      <svg
        className="throughput-svg"
        viewBox={`0 0 ${WIDTH} ${HEIGHT}`}
        preserveAspectRatio="none"
        role="img"
        aria-label={`Command dispatch rate over the last ${windowLabel}, peaking at ${peak} per minute`}
      >
        {values.map((value, index) => {
          const height = peak === 0 ? 0 : (value / peak) * (HEIGHT - 4);
          return (
            <rect
              key={index}
              className={`throughput-bar${value >= peak ? " peak" : ""}`}
              x={index * barWidth}
              y={HEIGHT - height}
              width={Math.max(barWidth - GAP, 1)}
              height={height}
              rx={1}
            />
          );
        })}
      </svg>

      <div className="throughput-legend">
        <span>
          peak <strong>{peak}</strong>/min
        </span>
        <span>
          average <strong>{average.toFixed(1)}</strong>/min
        </span>
        <span className="throughput-window">last {windowLabel}</span>
      </div>
    </div>
  );
}

export default ThroughputStrip;
