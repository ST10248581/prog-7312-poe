import { useMemo, useRef, useState } from "react";
import type { SensorSeries } from "../../services/apiService";
import { formatTime } from "../../utils/format";

interface LiveChartProps {
  series: SensorSeries;
}

const WIDTH = 760;
const HEIGHT = 240;
const PAD_LEFT = 48;
const PAD_RIGHT = 16;
const PAD_TOP = 16;
const PAD_BOTTOM = 26;

const PLOT_WIDTH = WIDTH - PAD_LEFT - PAD_RIGHT;
const PLOT_HEIGHT = HEIGHT - PAD_TOP - PAD_BOTTOM;

/**
 * The primary real-time feedback surface: one reading type over time, with the
 * configured thresholds drawn in so a value can be judged against its limits
 * without leaving the chart.
 */
function LiveChart({ series }: LiveChartProps) {
  const svgRef = useRef<SVGSVGElement>(null);
  const [hoverIndex, setHoverIndex] = useState<number | null>(null);

  const points = series.points;

  const scale = useMemo(() => {
    const values = points
      .map((point) =>
        point.value ?? (point.booleanValue === true ? 1 : point.booleanValue === false ? 0 : null)
      )
      .filter((value): value is number => value !== null);

    if (values.length === 0) {
      return null;
    }

    // Thresholds participate in the scale so the limit lines are always visible.
    const candidates = [...values];
    if (series.minThreshold !== null) candidates.push(series.minThreshold);
    if (series.maxThreshold !== null) candidates.push(series.maxThreshold);

    const rawMin = Math.min(...candidates);
    const rawMax = Math.max(...candidates);
    const padding = (rawMax - rawMin || 1) * 0.12;

    const min = rawMin - padding;
    const max = rawMax + padding;

    return {
      min,
      max,
      y: (value: number) =>
        PAD_TOP + PLOT_HEIGHT - ((value - min) / (max - min)) * PLOT_HEIGHT,
      x: (index: number) =>
        PAD_LEFT + (points.length === 1 ? PLOT_WIDTH / 2 : (index / (points.length - 1)) * PLOT_WIDTH),
    };
  }, [points, series.minThreshold, series.maxThreshold]);

  if (!scale || points.length === 0) {
    return <div className="chart-empty">No telemetry in this window.</div>;
  }

  const valueAt = (index: number) => {
    const point = points[index];
    return (
      point.value ?? (point.booleanValue === true ? 1 : point.booleanValue === false ? 0 : null)
    );
  };

  const path = points
    .map((_, index) => {
      const value = valueAt(index);
      if (value === null) return "";
      return `${index === 0 ? "M" : "L"} ${scale.x(index).toFixed(1)},${scale.y(value).toFixed(1)}`;
    })
    .filter(Boolean)
    .join(" ");

  const areaPath = `${path} L ${scale.x(points.length - 1).toFixed(1)},${PAD_TOP + PLOT_HEIGHT} L ${PAD_LEFT},${PAD_TOP + PLOT_HEIGHT} Z`;

  const anomalies = points
    .map((point, index) => ({ point, index }))
    .filter(({ point }) => point.isAnomaly);

  const gridValues = [0, 0.25, 0.5, 0.75, 1].map(
    (fraction) => scale.min + (scale.max - scale.min) * fraction
  );

  const handleMove = (event: React.MouseEvent<SVGSVGElement>) => {
    const svg = svgRef.current;
    if (!svg) return;

    const rect = svg.getBoundingClientRect();
    const ratio = (event.clientX - rect.left) / rect.width;
    const plotRatio = (ratio * WIDTH - PAD_LEFT) / PLOT_WIDTH;
    const index = Math.round(plotRatio * (points.length - 1));

    setHoverIndex(Math.min(Math.max(index, 0), points.length - 1));
  };

  const hovered = hoverIndex !== null ? points[hoverIndex] : null;
  const hoveredValue = hoverIndex !== null ? valueAt(hoverIndex) : null;

  return (
    <div className="live-chart">
      <svg
        ref={svgRef}
        viewBox={`0 0 ${WIDTH} ${HEIGHT}`}
        className="live-chart-svg"
        onMouseMove={handleMove}
        onMouseLeave={() => setHoverIndex(null)}
        role="img"
        aria-label={`${series.readingType} over time for ${series.nodeId}`}
      >
        <defs>
          <linearGradient id="chart-fill" x1="0" y1="0" x2="0" y2="1">
            <stop offset="0%" stopColor="var(--accent)" stopOpacity="0.28" />
            <stop offset="100%" stopColor="var(--accent)" stopOpacity="0" />
          </linearGradient>
        </defs>

        {/* Out-of-range bands: the eye should find a breach before reading a number. */}
        {series.maxThreshold !== null && (
          <rect
            x={PAD_LEFT}
            y={PAD_TOP}
            width={PLOT_WIDTH}
            height={Math.max(scale.y(series.maxThreshold) - PAD_TOP, 0)}
            className="chart-breach-band"
          />
        )}
        {series.minThreshold !== null && (
          <rect
            x={PAD_LEFT}
            y={scale.y(series.minThreshold)}
            width={PLOT_WIDTH}
            height={Math.max(PAD_TOP + PLOT_HEIGHT - scale.y(series.minThreshold), 0)}
            className="chart-breach-band"
          />
        )}

        {gridValues.map((value) => (
          <g key={value}>
            <line
              x1={PAD_LEFT}
              x2={WIDTH - PAD_RIGHT}
              y1={scale.y(value)}
              y2={scale.y(value)}
              className="chart-grid-line"
            />
            <text x={PAD_LEFT - 8} y={scale.y(value) + 3} className="chart-axis-label" textAnchor="end">
              {value.toFixed(1)}
            </text>
          </g>
        ))}

        <path d={areaPath} fill="url(#chart-fill)" />
        <path d={path} className="chart-line" fill="none" />

        {[
          { value: series.maxThreshold, label: "max" },
          { value: series.minThreshold, label: "min" },
        ].map(({ value, label }) =>
          value === null ? null : (
            <g key={label}>
              <line
                x1={PAD_LEFT}
                x2={WIDTH - PAD_RIGHT}
                y1={scale.y(value)}
                y2={scale.y(value)}
                className="chart-threshold-line"
              />
              <text
                x={WIDTH - PAD_RIGHT - 4}
                y={scale.y(value) - 4}
                className="chart-threshold-label"
                textAnchor="end"
              >
                {label} {value.toFixed(1)} {series.unit}
              </text>
            </g>
          )
        )}

        {anomalies.map(({ index }) => {
          const value = valueAt(index);
          if (value === null) return null;
          return (
            <circle
              key={index}
              cx={scale.x(index)}
              cy={scale.y(value)}
              r={3.5}
              className="chart-anomaly-dot"
            />
          );
        })}

        {hoverIndex !== null && hoveredValue !== null && (
          <g className="chart-hover">
            <line
              x1={scale.x(hoverIndex)}
              x2={scale.x(hoverIndex)}
              y1={PAD_TOP}
              y2={PAD_TOP + PLOT_HEIGHT}
              className="chart-crosshair"
            />
            <circle cx={scale.x(hoverIndex)} cy={scale.y(hoveredValue)} r={4} className="chart-hover-dot" />
          </g>
        )}

        <text x={PAD_LEFT} y={HEIGHT - 8} className="chart-axis-label">
          {formatTime(points[0].timestampUtc)}
        </text>
        <text x={WIDTH - PAD_RIGHT} y={HEIGHT - 8} className="chart-axis-label" textAnchor="end">
          {formatTime(points[points.length - 1].timestampUtc)}
        </text>
      </svg>

      {hovered && hoveredValue !== null && (
        <div className="chart-readout">
          <span className="chart-readout-value">
            {hoveredValue.toFixed(2)} {series.unit}
          </span>
          <span className="chart-readout-time">{formatTime(hovered.timestampUtc)}</span>
          <span className={`chart-readout-quality quality-${hovered.quality.toLowerCase()}`}>
            {hovered.quality}
          </span>
          {hovered.isAnomaly && <span className="chart-readout-flag">anomaly</span>}
        </div>
      )}
    </div>
  );
}

export default LiveChart;
