interface SparklineProps {
  values: number[];
  status: "Online" | "Warning" | "Offline";
}

const WIDTH = 120;
const HEIGHT = 28;

/**
 * Card-level trend line. Deliberately axis-free: at this level the shape is the
 * message, and exact values live in the detail panel.
 */
function Sparkline({ values, status }: SparklineProps) {
  if (values.length < 2) {
    return <div className="sparkline-empty">no recent data</div>;
  }

  const min = Math.min(...values);
  const max = Math.max(...values);
  const span = max - min || 1;

  const points = values.map((value, index) => {
    const x = (index / (values.length - 1)) * WIDTH;
    const y = HEIGHT - ((value - min) / span) * (HEIGHT - 4) - 2;
    return `${x.toFixed(1)},${y.toFixed(1)}`;
  });

  const line = `M ${points.join(" L ")}`;
  const area = `${line} L ${WIDTH},${HEIGHT} L 0,${HEIGHT} Z`;
  const gradientId = `spark-${status}`;

  return (
    <svg
      className={`sparkline status-${status.toLowerCase()}`}
      viewBox={`0 0 ${WIDTH} ${HEIGHT}`}
      preserveAspectRatio="none"
      aria-hidden="true"
    >
      <defs>
        <linearGradient id={gradientId} x1="0" y1="0" x2="0" y2="1">
          <stop offset="0%" className="spark-stop-top" />
          <stop offset="100%" className="spark-stop-bottom" />
        </linearGradient>
      </defs>
      <path d={area} fill={`url(#${gradientId})`} />
      <path d={line} className="sparkline-stroke" fill="none" />
    </svg>
  );
}

export default Sparkline;
