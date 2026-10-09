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
  SENSOR_CATEGORIES,
  SENSOR_CATEGORY_HINTS,
  TIME_WINDOWS,
  describeFilters,
  removeFilter,
} from "./types";
import type { AlertSeverity, CommandFilters, OperationCategory, SensorCategory } from "./types";
import type { CommandFilterOptions } from "../../services/apiService";

/**
 * How long the search box waits before asking the API. Every keystroke is a
 * request otherwise, and three endpoints answer each one.
 */
const SEARCH_DEBOUNCE_MS = 350;

/**
 * Which half of the filter a bar edits. The device half describes the node —
 * it narrows the live device panel and the commands sent to those nodes — and
 * sits on the device panel. The command half describes the command itself and
 * sits on the stream, the only lists it affects.
 */
export type FilterScope = "device" | "command";

const SCOPE_KEYS: Record<FilterScope, readonly (keyof CommandFilters)[]> = {
  device: ["search", "zone", "sensorCategories", "alertStates", "minAlertSeverity"],
  command: ["statuses", "origins", "commandTypes", "operationCategories", "manualOnly", "windowMinutes"],
};

/** The filters with this scope's facets back at their defaults. */
function clearScope(filters: CommandFilters, scope: FilterScope): CommandFilters {
  const next = { ...filters };
  for (const key of SCOPE_KEYS[scope]) {
    (next as Record<string, unknown>)[key] = EMPTY_FILTERS[key];
  }
  return next;
}

function scopeIsActive(filters: CommandFilters, scope: FilterScope): boolean {
  return SCOPE_KEYS[scope].some(
    (key) => JSON.stringify(filters[key]) !== JSON.stringify(EMPTY_FILTERS[key]),
  );
}

interface CommandFilterBarProps {
  scope: FilterScope;
  /** Straight from `/api/commands/filter-options`; null until it arrives. */
  options: CommandFilterOptions | null;
  filters: CommandFilters;
  /** Command scope: commands streaming, and the total matching. */
  resultCount?: number;
  totalCount?: number;
  /** Command scope: per-category counts from the summary, so a chip can show what it holds. */
  categoryCounts?: Record<string, number>;
  /**
   * Device scope: counts from `/api/commands/devices`, per device category and
   * per alert state, so those chips say how many devices they select.
   */
  deviceCounts?: {
    categories: Record<string, number>;
    alertStates: Record<string, number>;
  };
  onChange: (filters: CommandFilters) => void;
}

/**
 * One filter state for the whole page, edited from two compact bars, each next
 * to the lists it narrows.
 *
 * Every control only edits `filters` and hands it back up; the page owns the
 * request and refetches on every change, so this component never narrows a
 * list itself. The option lists come from the API, with the local constants
 * standing in only until that first response lands.
 */
function CommandFilterBar({
  scope,
  options,
  filters,
  resultCount,
  totalCount,
  categoryCounts,
  deviceCounts,
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
    if (scope !== "device" || searchDraft === filters.search) {
      return;
    }

    const timer = window.setTimeout(
      () => onChange({ ...filters, search: searchDraft }),
      SEARCH_DEBOUNCE_MS,
    );

    return () => window.clearTimeout(timer);
  }, [scope, searchDraft, filters, onChange]);

  const statuses = options?.statuses ?? COMMAND_STATUSES;
  const origins = options?.origins ?? COMMAND_ORIGINS;
  const commandTypes = options?.commandTypes ?? COMMAND_TYPES;
  const zones = options?.zones ?? [];
  const alertStates = options?.alertStates ?? NODE_ALERT_STATES;
  const severities = options?.alertSeverities ?? ALERT_SEVERITIES;
  const sensorCategories: SensorCategory[] = options?.sensorCategories ?? SENSOR_CATEGORIES;

  // The API sends each category with the command types it covers, so the chip
  // can say what selecting it will include without repeating the mapping.
  const categories: OperationCategory[] =
    options?.operationCategories.map((option) => option.category) ?? OPERATION_CATEGORIES;

  const categoryTypes = (category: OperationCategory) =>
    options?.operationCategories
      .find((option) => option.category === category)
      ?.commandTypes.map(humanise)
      .join(", ");

  type ChipKey =
    | "sensorCategories"
    | "statuses"
    | "origins"
    | "commandTypes"
    | "operationCategories"
    | "alertStates";

  const toggle = <K extends ChipKey>(key: K, value: CommandFilters[K][number]) => {
    const current = filters[key] as CommandFilters[K][number][];
    const next = current.includes(value)
      ? current.filter((entry) => entry !== value)
      : [...current, value];

    onChange({ ...filters, [key]: next });
  };

  const isActive = (key: ChipKey, value: string) =>
    (filters[key] as string[]).includes(value);

  // Only this bar's own facets, so each summary describes the list beneath it.
  const activeChips = describeFilters(filters).filter((chip) =>
    SCOPE_KEYS[scope].includes(chip.key),
  );

  const clearButton = scopeIsActive(filters, scope) && (
    <button type="button" className="filter-clear" onClick={() => onChange(clearScope(filters, scope))}>
      Clear
    </button>
  );

  // Everything this bar is narrowing, each removable — an empty list is then
  // never misread as a stalled feed rather than a filter nobody remembers.
  const summary = activeChips.length > 0 && (
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
  );

  if (scope === "device") {
    return (
      <div className="filter-bar compact-filter-bar" role="search" aria-label="Filter devices">
        <div className="filter-row">
          {/* Search leads: it is the fastest way to a node, and the API matches
              it against MACs, zones, operators and command labels too. */}
          <div className="filter-search-wrap">
            <label className="visually-hidden" htmlFor="command-search">
              Search
            </label>
            <input
              id="command-search"
              type="search"
              className="filter-search"
              placeholder="Search device, MAC, zone, category, operator or command…"
              value={searchDraft}
              onChange={(event) => setSearchDraft(event.target.value)}
            />
            {searchDraft !== filters.search && (
              <span className="filter-search-pending" aria-live="polite">
                searching…
              </span>
            )}
          </div>

          <select
            className="filter-select"
            aria-label="Zone"
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

          <select
            className="filter-select"
            aria-label="Alert severity"
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

          {clearButton}
        </div>

        <div className="filter-row">
          <div className="filter-group">
            <span className="filter-group-label">Category</span>
            <div className="filter-chips">
              {sensorCategories.map((category) => {
                const count = deviceCounts?.categories[category];

                return (
                  <button
                    key={category}
                    type="button"
                    className={`filter-chip device-category-${category.toLowerCase()}${
                      isActive("sensorCategories", category) ? " active" : ""
                    }`}
                    title={SENSOR_CATEGORY_HINTS[category]}
                    onClick={() => toggle("sensorCategories", category)}
                  >
                    {humanise(category)}
                    {count !== undefined && <span className="filter-chip-count">{count}</span>}
                  </button>
                );
              })}
            </div>
          </div>

          {/* The alert facet describes the node, not the command — so it also
              answers "what is being sent to the nodes alerting right now". */}
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
                  {deviceCounts?.alertStates[state] !== undefined && (
                    <span className="filter-chip-count" title="Devices in this state">
                      {deviceCounts.alertStates[state]}
                    </span>
                  )}
                </button>
              ))}
            </div>
          </div>
        </div>

        {summary}
      </div>
    );
  }

  return (
    <div className="filter-bar compact-filter-bar" role="group" aria-label="Filter commands">
      <div className="filter-row">
        {/* Window is a segmented control rather than a chip row: the options are
            mutually exclusive, so multi-select styling would mislead. */}
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

        <button
          type="button"
          className={`filter-toggle${filters.manualOnly ? " active" : ""}`}
          onClick={() => onChange({ ...filters, manualOnly: !filters.manualOnly })}
        >
          Manual only
        </button>
      </div>

      <div className="filter-row">
        <div className="filter-group">
          <span className="filter-group-label">Operation</span>
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

        <div className="filter-actions">
          <span className="filter-count">
            <strong>{resultCount ?? 0}</strong> streaming of {totalCount ?? 0} matching
          </span>
          {clearButton}
        </div>
      </div>

      {summary}
    </div>
  );
}

export default CommandFilterBar;
