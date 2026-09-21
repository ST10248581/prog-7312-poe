/**
 * Layout-stage contract for the command stream page.
 *
 * Nothing here is wired to the API yet. These shapes mirror what the planned
 * `/api/commands` endpoints are expected to return so the components can be
 * swapped onto real data without a rewrite: the page holds `CommandFilters`,
 * sends it to the server, and renders whatever comes back. No filtering,
 * sorting or paging happens in the browser.
 */

export type CommandStatus = "Queued" | "Sent" | "Acknowledged" | "Failed" | "Expired";

export type CommandOrigin = "Automation" | "Manual" | "Schedule";

export type CommandType =
  | "SetThreshold"
  | "Recalibrate"
  | "ToggleActuator"
  | "RestartNode"
  | "FirmwarePush"
  | "RequestSample";

export interface CommandRecord {
  id: string;
  issuedUtc: string;
  nodeId: string;
  sensorName: string;
  zone: string;
  commandType: CommandType;
  /** Rendered as-is in the stream, e.g. `temp.max=28.5`. */
  parameters: string;
  origin: CommandOrigin;
  status: CommandStatus;
  /** Time from dispatch to acknowledgement; null while still in flight. */
  roundTripMs: number | null;
  issuedBy: string;
  retries: number;
}

/**
 * The whole filter state, sent to the API as one query. Empty arrays mean
 * "no constraint" rather than "match nothing".
 */
export interface CommandFilters {
  statuses: CommandStatus[];
  origins: CommandOrigin[];
  commandTypes: CommandType[];
  zone: string;
  /** Minutes of history to request. */
  windowMinutes: number;
  /** Free text matched server-side against node id and sensor name. */
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
 * Option lists are hard-coded for the layout pass. The real page fetches them
 * from the API the way `FilterBar` already does on the telemetry route, so the
 * server stays the single source of truth for what is filterable.
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

export const TIME_WINDOWS = [
  { label: "15m", minutes: 15 },
  { label: "1h", minutes: 60 },
  { label: "6h", minutes: 360 },
  { label: "24h", minutes: 1440 },
];
