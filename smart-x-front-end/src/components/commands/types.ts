/**
 * Page-local contract for the command stream.
 *
 * The record and enum shapes now come from `apiService` — the API is the single
 * source of truth for what a command is. What stays here is the filter state
 * the page holds and the fallback option lists: the page sends `CommandFilters`
 * to the server and renders whatever comes back, so nothing is filtered,
 * sorted or paged in the browser.
 */

import type {
  AlertSeverity,
  CommandOrigin,
  CommandPriority,
  CommandStatus,
  CommandType,
  DeviceCommand,
  NodeAlertState,
  OperationCategory,
} from "../../services/apiService";

export type {
  AlertSeverity,
  CommandOrigin,
  CommandPriority,
  CommandStatus,
  CommandType,
  DeviceCommand,
  NodeAlertState,
  OperationCategory,
};

/** The command record as rendered by this page. */
export type CommandRecord = DeviceCommand;

export interface CommandFilters {
  statuses: CommandStatus[];
  origins: CommandOrigin[];
  commandTypes: CommandType[];
  /** What kind of operation was being performed, above the command type. */
  operationCategories: OperationCategory[];
  /** Alert state of the node a command was aimed at. */
  alertStates: NodeAlertState[];
  /** Severity floor for the node's open alerts; empty means no floor. */
  minAlertSeverity: AlertSeverity | "";
  zone: string;
  /** Minutes of history to request. */
  windowMinutes: number;
  /** Free text matched server-side against node id, sensor name and operator. */
  search: string;
  manualOnly: boolean;
}

export const EMPTY_FILTERS: CommandFilters = {
  statuses: [],
  origins: [],
  commandTypes: [],
  operationCategories: [],
  alertStates: [],
  minAlertSeverity: "",
  zone: "",
  windowMinutes: 60,
  search: "",
  manualOnly: false,
};

/**
 * Fallbacks for the filter controls. `/api/commands/filter-options` is the real
 * source; these only stand in for the first paint and for the case where the
 * API is unreachable, so the controls are never an empty row.
 */
export const COMMAND_STATUSES: CommandStatus[] = [
  "Queued",
  "Sent",
  "Acknowledged",
  "Failed",
  "Expired",
];

export const COMMAND_ORIGINS: CommandOrigin[] = ["Automation", "Manual", "Schedule"];

export const COMMAND_TYPES: CommandType[] = [
  "SetThreshold",
  "Recalibrate",
  "ToggleActuator",
  "RestartNode",
  "FirmwarePush",
  "RequestSample",
];

export const OPERATION_CATEGORIES: OperationCategory[] = [
  "Configuration",
  "Maintenance",
  "Control",
  "Diagnostics",
];

/** Escalating, matching the API — the order the chips are shown in. */
export const NODE_ALERT_STATES: NodeAlertState[] = [
  "Active",
  "Acknowledged",
  "Resolved",
  "Clear",
];

export const ALERT_SEVERITIES: AlertSeverity[] = ["Info", "Warning", "Critical"];

export const COMMAND_PRIORITIES: CommandPriority[] = ["Normal", "High", "Immediate"];

/** Human-readable note on what each priority does to the queue. */
export const PRIORITY_HINTS: Record<CommandPriority, string> = {
  Normal: "Normal — queued behind automation",
  High: "High — jumps the queue",
  Immediate: "Immediate — pre-empts in-flight work",
};

/** What each category covers, for the chip tooltips. */
export const CATEGORY_HINTS: Record<OperationCategory, string> = {
  Configuration: "Changes what the node is configured to do",
  Maintenance: "Restores a node that is drifting or unresponsive",
  Control: "Acts on the physical world through the node",
  Diagnostics: "Asks the node for something without changing it",
};

/** What each alert state says about the node a command was aimed at. */
export const ALERT_STATE_HINTS: Record<NodeAlertState, string> = {
  Active: "Node has an unacknowledged alert standing against it",
  Acknowledged: "Alert seen by an operator, not yet resolved",
  Resolved: "Node has alert history, nothing outstanding",
  Clear: "No alert has ever been raised against the node",
};

export const TIME_WINDOWS = [
  { label: "15m", minutes: 15 },
  { label: "1h", minutes: 60 },
  { label: "6h", minutes: 360 },
  { label: "24h", minutes: 1440 },
];

/** Turns the page's filter state into the query the API expects. */
export function toCommandQuery(filters: CommandFilters) {
  return {
    statuses: filters.statuses,
    origins: filters.origins,
    commandTypes: filters.commandTypes,
    operationCategories: filters.operationCategories,
    alertStates: filters.alertStates,
    // "" is the page's "no floor"; the query helper drops undefined keys.
    minAlertSeverity: filters.minAlertSeverity || undefined,
    zone: filters.zone,
    search: filters.search,
    manualOnly: filters.manualOnly,
    windowMinutes: filters.windowMinutes,
  };
}

/** True when anything narrows the default slice — drives the Clear button. */
export function hasActiveFilters(filters: CommandFilters): boolean {
  return (
    filters.statuses.length > 0 ||
    filters.origins.length > 0 ||
    filters.commandTypes.length > 0 ||
    filters.operationCategories.length > 0 ||
    filters.alertStates.length > 0 ||
    filters.minAlertSeverity !== "" ||
    filters.zone !== "" ||
    filters.search !== "" ||
    filters.manualOnly ||
    filters.windowMinutes !== EMPTY_FILTERS.windowMinutes
  );
}

/**
 * The filters narrowing the slice, as short labels. The page shows these as
 * removable chips so an empty result is always explained by something visible.
 */
export function describeFilters(
  filters: CommandFilters,
): { key: keyof CommandFilters; value: string; label: string }[] {
  const chips: { key: keyof CommandFilters; value: string; label: string }[] = [];

  filters.operationCategories.forEach((category) =>
    chips.push({ key: "operationCategories", value: category, label: category }),
  );
  filters.alertStates.forEach((state) =>
    chips.push({ key: "alertStates", value: state, label: `Alert: ${state}` }),
  );
  filters.statuses.forEach((status) =>
    chips.push({ key: "statuses", value: status, label: status }),
  );
  filters.origins.forEach((origin) =>
    chips.push({ key: "origins", value: origin, label: origin }),
  );
  filters.commandTypes.forEach((type) =>
    chips.push({ key: "commandTypes", value: type, label: type }),
  );

  if (filters.minAlertSeverity !== "") {
    chips.push({
      key: "minAlertSeverity",
      value: filters.minAlertSeverity,
      label: `${filters.minAlertSeverity} and above`,
    });
  }

  if (filters.zone !== "") {
    chips.push({ key: "zone", value: filters.zone, label: filters.zone });
  }

  if (filters.manualOnly) {
    chips.push({ key: "manualOnly", value: "true", label: "Manual overrides" });
  }

  if (filters.search !== "") {
    chips.push({ key: "search", value: filters.search, label: `"${filters.search}"` });
  }

  return chips;
}

/** Removes one chip from the filter state, whatever kind of filter it was. */
export function removeFilter(
  filters: CommandFilters,
  key: keyof CommandFilters,
  value: string,
): CommandFilters {
  switch (key) {
    case "statuses":
    case "origins":
    case "commandTypes":
    case "operationCategories":
    case "alertStates": {
      const current = filters[key] as string[];
      return { ...filters, [key]: current.filter((entry) => entry !== value) };
    }
    case "manualOnly":
      return { ...filters, manualOnly: false };
    case "minAlertSeverity":
      return { ...filters, minAlertSeverity: "" };
    case "zone":
      return { ...filters, zone: "" };
    case "search":
      return { ...filters, search: "" };
    default:
      return filters;
  }
}
