import type { FilterOptions, SensorFilters } from "../../services/apiService";
import { humanise } from "../../utils/format";

interface FilterBarProps {
  options: FilterOptions | null;
  filters: SensorFilters;
  resultCount: number;
  totalCount: number;
  onChange: (filters: SensorFilters) => void;
}

/**
 * The zoom-and-filter step. Every control narrows the same query, so the grid,
 * the counts and the detail panel always describe one consistent slice.
 */
function FilterBar({
  options,
  filters,
  resultCount,
  totalCount,
  onChange,
}: FilterBarProps) {
  const toggle = (key: "categories" | "statuses" | "zones", value: string) => {
    const current = filters[key] ?? [];
    const next = current.includes(value)
      ? current.filter((entry) => entry !== value)
      : [...current, value];

    onChange({ ...filters, [key]: next });
  };

  const isActive = (key: "categories" | "statuses" | "zones", value: string) =>
    (filters[key] ?? []).includes(value);

  const hasFilters =
    (filters.categories?.length ?? 0) > 0 ||
    (filters.statuses?.length ?? 0) > 0 ||
    (filters.zones?.length ?? 0) > 0 ||
    filters.anomaliesOnly === true;

  return (
    <section className="filter-bar" aria-label="Filter sensors">
      <div className="filter-group">
        <span className="filter-group-label">Status</span>
        <div className="filter-chips">
          {(options?.statuses ?? []).map((status) => (
            <button
              key={status}
              type="button"
              className={`filter-chip status-${status.toLowerCase()}${
                isActive("statuses", status) ? " active" : ""
              }`}
              onClick={() => toggle("statuses", status)}
            >
              <span className="filter-chip-dot" />
              {status}
            </button>
          ))}
        </div>
      </div>

      <div className="filter-group">
        <span className="filter-group-label">Category</span>
        <div className="filter-chips">
          {(options?.categories ?? []).map((category) => (
            <button
              key={category}
              type="button"
              className={`filter-chip${isActive("categories", category) ? " active" : ""}`}
              onClick={() => toggle("categories", category)}
            >
              {humanise(category)}
            </button>
          ))}
        </div>
      </div>

      <div className="filter-group">
        <span className="filter-group-label">Zone</span>
        <div className="filter-chips">
          {(options?.zones ?? []).map((zone) => (
            <button
              key={zone}
              type="button"
              className={`filter-chip${isActive("zones", zone) ? " active" : ""}`}
              onClick={() => toggle("zones", zone)}
            >
              {zone}
            </button>
          ))}
        </div>
      </div>

      <div className="filter-actions">
        <button
          type="button"
          className={`filter-toggle${filters.anomaliesOnly ? " active" : ""}`}
          onClick={() => onChange({ ...filters, anomaliesOnly: !filters.anomaliesOnly })}
        >
          Anomalies only
        </button>

        <span className="filter-count">
          <strong>{resultCount}</strong> of {totalCount} sensors
        </span>

        {hasFilters && (
          <button type="button" className="filter-clear" onClick={() => onChange({})}>
            Clear
          </button>
        )}
      </div>
    </section>
  );
}

export default FilterBar;
