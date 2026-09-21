import { humanise } from "../../utils/format";
import {
  COMMAND_ORIGINS,
  COMMAND_STATUSES,
  COMMAND_TYPES,
  EMPTY_FILTERS,
  TIME_WINDOWS,
} from "./types";
import type { CommandFilters } from "./types";

interface CommandFilterBarProps {
  filters: CommandFilters;
  zones: string[];
  resultCount: number;
  totalCount: number;
  onChange: (filters: CommandFilters) => void;
}

/**
 * One filter state for the whole page — the stream, the throughput strip and
 * the history table all describe the same slice.
 *
 * Every control only edits `filters` and hands it back up. The page owns the
 * request, so when the API lands the change is a fetch in the page, not a
 * change here: this component never narrows a list itself.
 */
function CommandFilterBar({
  filters,
  zones,
  resultCount,
  totalCount,
  onChange,
}: CommandFilterBarProps) {
  type ChipKey = "statuses" | "origins" | "commandTypes";

  const toggle = <K extends ChipKey>(key: K, value: CommandFilters[K][number]) => {
    const current = filters[key] as CommandFilters[K][number][];
    const next = current.includes(value)
      ? current.filter((entry) => entry !== value)
      : [...current, value];

    onChange({ ...filters, [key]: next });
  };

  const isActive = (key: ChipKey, value: string) =>
    (filters[key] as string[]).includes(value);

  const hasFilters =
    filters.statuses.length > 0 ||
    filters.origins.length > 0 ||
    filters.commandTypes.length > 0 ||
    filters.zone !== "" ||
    filters.search !== "" ||
    filters.manualOnly ||
    filters.windowMinutes !== EMPTY_FILTERS.windowMinutes;

  return (
    <section className="filter-bar command-filter-bar" aria-label="Filter commands">
      <div className="filter-group">
        <span className="filter-group-label">Status</span>
        <div className="filter-chips">
          {COMMAND_STATUSES.map((status) => (
            <button
              key={status}
              type="button"
              className={`filter-chip cmd-status-${status.toLowerCase()}${
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
        <span className="filter-group-label">Origin</span>
        <div className="filter-chips">
          {COMMAND_ORIGINS.map((origin) => (
            <button
              key={origin}
              type="button"
              className={`filter-chip${isActive("origins", origin) ? " active" : ""}`}
              onClick={() => toggle("origins", origin)}
            >
              {origin}
            </button>
          ))}
        </div>
      </div>

      <div className="filter-group">
        <span className="filter-group-label">Command</span>
        <div className="filter-chips">
          {COMMAND_TYPES.map((type) => (
            <button
              key={type}
              type="button"
              className={`filter-chip${isActive("commandTypes", type) ? " active" : ""}`}
              onClick={() => toggle("commandTypes", type)}
            >
              {humanise(type)}
            </button>
          ))}
        </div>
      </div>

      <div className="filter-group">
        <span className="filter-group-label">Zone</span>
        <select
          className="filter-select"
          value={filters.zone}
          onChange={(event) => onChange({ ...filters, zone: event.target.value })}
        >
          <option value="">All zones</option>
          {zones.map((zone) => (
            <option key={zone} value={zone}>
              {zone}
            </option>
          ))}
        </select>
      </div>

      {/* Window is a segmented control rather than a chip row: the options are
          mutually exclusive, so multi-select styling would mislead. */}
      <div className="filter-group">
        <span className="filter-group-label">Window</span>
        <div className="filter-segment" role="group" aria-label="Time window">
          {TIME_WINDOWS.map((window) => (
            <button
              key={window.minutes}
              type="button"
              className={`filter-segment-btn${
                filters.windowMinutes === window.minutes ? " active" : ""
              }`}
              aria-pressed={filters.windowMinutes === window.minutes}
              onClick={() => onChange({ ...filters, windowMinutes: window.minutes })}
            >
              {window.label}
            </button>
          ))}
        </div>
      </div>

      <div className="filter-group filter-group-search">
        <label className="filter-group-label" htmlFor="command-search">
          Search
        </label>
        <input
          id="command-search"
          type="search"
          className="filter-search"
          placeholder="Node id or sensor name…"
          value={filters.search}
          onChange={(event) => onChange({ ...filters, search: event.target.value })}
        />
      </div>

      <div className="filter-actions">
        <button
          type="button"
          className={`filter-toggle${filters.manualOnly ? " active" : ""}`}
          onClick={() => onChange({ ...filters, manualOnly: !filters.manualOnly })}
        >
          Manual overrides only
        </button>

        <span className="filter-count">
          <strong>{resultCount}</strong> of {totalCount} commands
        </span>

        {hasFilters && (
          <button
            type="button"
            className="filter-clear"
            onClick={() => onChange(EMPTY_FILTERS)}
          >
            Clear
          </button>
        )}
      </div>
    </section>
  );
}

export default CommandFilterBar;
