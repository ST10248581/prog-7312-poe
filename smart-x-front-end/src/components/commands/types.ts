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
  CommandOrigin,
  CommandPriority,
  CommandStatus,
  CommandType,
  DeviceCommand,
} from "../../services/apiService";

export type {
  CommandOrigin,
  CommandPriority,
  CommandStatus,
  CommandType,
  DeviceCommand,
};

/** The command record as rendered by this page. */
export type CommandRecord = DeviceCommand;

export interface CommandFilters {
  statuses: CommandStatus[];
  origins: CommandOrigin[];
  commandTypes: CommandType[];
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

export const COMMAND_PRIORITIES: CommandPriority[] = ["Normal", "High", "Immediate"];

/** Human-readable note on what each priority does to the queue. */
export const PRIORITY_HINTS: Record<CommandPriority, string> = {
  Normal: "Normal — queued behind automation",
  High: "High — jumps the queue",
  Immediate: "Immediate — pre-empts in-flight work",
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
    zone: filters.zone,
    search: filters.search,
    manualOnly: filters.manualOnly,
    windowMinutes: filters.windowMinutes,
  };
}
