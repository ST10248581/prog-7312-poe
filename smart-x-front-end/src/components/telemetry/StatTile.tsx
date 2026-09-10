import type { ReactNode } from "react";

interface StatTileProps {
  label: string;
  value: ReactNode;
  unit?: string;
  hint?: string;
  tone?: "default" | "accent" | "warning" | "danger";
  /** 0-100; renders a progress bar under the value. */
  progress?: number;
  children?: ReactNode;
}

function StatTile({
  label,
  value,
  unit,
  hint,
  tone = "default",
  progress,
  children,
}: StatTileProps) {
  return (
    <div className={`stat-tile tone-${tone}`}>
      <span className="stat-tile-label">{label}</span>
      <div className="stat-tile-value">
        {value}
        {unit && <span className="stat-tile-unit">{unit}</span>}
      </div>

      {progress !== undefined && (
        <div className="stat-tile-bar" role="presentation">
          <div
            className="stat-tile-bar-fill"
            style={{ width: `${Math.min(Math.max(progress, 0), 100)}%` }}
          />
        </div>
      )}

      {children}
      {hint && <span className="stat-tile-hint">{hint}</span>}
    </div>
  );
}

export default StatTile;
