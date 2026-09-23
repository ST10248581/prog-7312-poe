import { useEffect, useState } from "react";
import { humanise } from "../../utils/format";
import {
  ALERT_SEVERITIES,
  ALERT_STATE_HINTS,
  CATEGORY_HINTS,
  COMMAND_ORIGINS,
  COMMAND_STATUSES,
  COMMAND_TYPES,
  EMPTY_FILTERS,
  NODE_ALERT_STATES,
  OPERATION_CATEGORIES,
  TIME_WINDOWS,
  describeFilters,
  hasActiveFilters,
  removeFilter,
} from "./types";
import type { AlertSeverity, CommandFilters, OperationCategory } from "./types";
import type { CommandFilterOptions } from "../../services/apiService";

/**
 * How long the search box waits before asking the API. Every keystroke is a
 * request otherwise, and three endpoints answer each one.
 */
const SEARCH_DEBOUNCE_MS = 350;

interface CommandFilterBarProps {
  /** Straight from `/api/commands/filter-options`; null until it arrives. */
  options: CommandFilterOptions | null;
  filters: CommandFilters;
  resultCount: number;
  totalCount: number;
  /** Per-category counts from the summary, so a chip can show what it holds. */
  categoryCounts?: Record<string, number>;
  onChange: (filters: CommandFilters) => void;
}

/**
 * One filter state for the whole page — the stream, the throughput strip and
 * the history table all describe the same slice.
 *
 * Every control only edits `filters` and hands it back up; the page owns the
 * request and refetches on every change, so this component never narrows a
 * list itself. The option lists come from the API, with the local constants
 * standing in only until that first response lands.
 */
function CommandFilterBar({
  options,
  filters,
  resultCount,
  totalCount,
  categoryCounts,
  onChange,
}: CommandFilterBarProps) {
  // The search box is the one control the page does not own outright: it types
  // faster than the API should be asked, so the draft lives here and is pushed
  // up once typing settles.
  const [searchDraft, setSearchDraft] = useState(filters.search);

  // A reset from outside — Clear, or the chip for the search term — has to win
  // over whatever is sitting in the box.
  useEffect(() => {
    // oxlint-disable-next-line react/set-state-in-effect
    setSearchDraft(filters.search);
  }, [filters.search]);

  useEffect(() => {
    if (searchDraft === filters.search) {
      return;
    }

    const timer = window.setTimeout(
      () => onChange({ ...filters, search: searchDraft }),
      SEARCH_DEBOUNCE_MS,
    );

    return () => window.clearTimeout(timer);
  }, [searchDraft, filters, onChange]);

  const statuses = options?.statuses ?? COMMAND_STATUSES;
  const origins = options?.origins ?? COMMAND_ORIGINS;
  const commandTypes = options?.commandTypes ?? COMMAND_TYPES;
  const zones = options?.zones ?? [];
  const alertStates = options?.alertStates ?? NODE_ALERT_STATES;
  const severities = options?.alertSeverities ?? ALERT_SEVERITIES;

  // The API sends each category with the command types it covers, so the chip
  // can say what selecting it will include without repeating the mapping.
  const categories: OperationCategory[] =
    options?.operationCategories.map((option) => option.category) ?? OPERATION_CATEGORIES;

  const categoryTypes = (category: OperationCategory) =>
    options?.operationCategories
      .find((option) => option.category === category)
      ?.commandTypes.map(humanise)
      .join(", ");

  type ChipKey = "statuses" | "origins" | "commandTypes" | "operationCategories" | "alertStates";

  const toggle = <K extends ChipKey>(key: K, value: CommandFilters[K][number]) => {
    const current = filters[key] as CommandFilters[K][number][];
    const next = current.includes(value)
      ? current.filter((entry) => entry !== value)
      : [...current, value];

    onChange({ ...filters, [key]: next });
  };

  const isActive = (key: ChipKey, value: string) =>
    (filters[key] as string[]).includes(value);

  const activeChips = describeFilters(filters);

  return (
    <section className="filter-bar command-filter-bar" aria-label="Search and filter commands">
      {/* Search leads: it is the fastest way to a node, and the API matches it
          against the command type and category labels too, so typing a category
          name works before the chips below are even read. */}
      <div className="filter-group filter-group-search">
        <label className="filter-group-label" htmlFor="command-search">
          Search
        </label>
        <div className="filter-search-wrap">
          <input
            id="command-search"
            type="search"
            className="filter-search"
            placeholder="Node, sensor, zone, operator, parameters or command…"
            value={searchDraft}
            onChange={(event) => setSearchDraft(event.target.value)}
          />
          {searchDraft !== filters.search && (
            <span className="filter-search-pending" aria-live="polite">
              searching…
            </span>
          )}
        </div>
      </div>

      <div className="filter-group">
        <span className="filter-group-label">Operation category</span>
        <div className="filter-chips">
          {categories.map((category) => {
            const count = categoryCounts?.[category];

            return (
              <button
                key={category}
                type="button"
                className={`filter-chip category-${category.toLowerCase()}${
                  isActive("operationCategories", category) ? " active" : ""
                }`}
                title={`${CATEGORY_HINTS[category]}${
                  categoryTypes(category) ? ` — ${categoryTypes(category)}` : ""
                }`}
                onClick={() => toggle("operationCategories", category)}
              >
                {category}
                {count !== undefined && <span className="filter-chip-count">{count}</span>}
              </button>
            );
          })}
        </div>
      </div>

      {/* The alert facet describes the node a command was aimed at, not the
          command itself — the "what was being sent to the nodes that are
          alerting right now" question. */}
      <div className="filter-group">
        <span className="filter-group-label">Node alert</span>
        <div className="filter-chips">
          {alertStates.map((state) => (
            <button
              key={state}
              type="button"
              className={`filter-chip alert-state-${state.toLowerCase()}${
                isActive("alertStates", state) ? " active" : ""
              }`}
              title={ALERT_STATE_HINTS[state]}
              onClick={() => toggle("alertStates", state)}
            >
              <span className="filter-chip-dot" />
              {state}
            </button>
          ))}
        </div>
      </div>

      <div className="filter-group">
        <label className="filter-group-label" htmlFor="command-severity">
          Alert severity
        </label>
        <select
          id="command-severity"
          className="filter-select"
          value={filters.minAlertSeverity}
          onChange={(event) =>
            onChange({
              ...filters,
              minAlertSeverity: event.target.value as AlertSeverity | "",
            })
          }
        >
          <option value="">Any severity</option>
          {severities.map((severity) => (
            <option key={severity} value={severity}>
              {severity} and above
            </option>
          ))}
        </select>
      </div>

      <div className="filter-group">
        <span className="filter-group-label">Status</span>
        <div className="filter-chips">
          {statuses.map((status) => (
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
          {origins.map((origin) => (
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
          {commandTypes.map((type) => (
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

      <div className="filter-actions">
        <button
          type="button"
          className={`filter-toggle${filters.manualOnly ? " active" : ""}`}
          onClick={() => onChange({ ...filters, manualOnly: !filters.manualOnly })}
        >
          Manual overrides only
        </button>

        <span className="filter-count">
          <strong>{resultCount}</strong> streaming of {totalCount} matching
        </span>

        {hasActiveFilters(filters) && (
          <button
            type="button"
            className="filter-clear"
            onClick={() => onChange(EMPTY_FILTERS)}
          >
            Clear
          </button>
        )}
      </div>

      {/* Everything narrowing the slice, in one row. With eight facets spread
          across the bar, an empty stream is otherwise easy to misread as a
          stalled feed rather than a filter nobody remembers setting. */}
      {activeChips.length > 0 && (
        <div className="filter-active" aria-label="Active filters">
          <span className="filter-group-label">Filtering by</span>
          {activeChips.map((chip) => (
            <button
              key={`${chip.key}-${chip.value}`}
              type="button"
              className="filter-active-chip"
              title={`Remove ${chip.label}`}
              onClick={() => onChange(removeFilter(filters, chip.key, chip.value))}
            >
              {chip.label}
              <span aria-hidden="true">×</span>
            </button>
          ))}
        </div>
      )}
    </section>
  );
}

export default CommandFilterBar;
