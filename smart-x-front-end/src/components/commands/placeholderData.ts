import type { CommandRecord } from "./types";

/**
 * Throwaway sample rows for the layout pass — delete this file once the page
 * reads from the API. They exist only so the stream, the history table and the
 * throughput strip can be judged at realistic density rather than empty.
 */

const MINUTE = 60_000;

/** Timestamps are relative to load so the "live" column never looks stale. */
function minutesAgo(minutes: number): string {
  return new Date(Date.now() - minutes * MINUTE).toISOString();
}

export const PLACEHOLDER_COMMANDS: CommandRecord[] = [
  {
    id: "cmd-1041",
    issuedUtc: minutesAgo(0.2),
    nodeId: "NODE-A14",
    sensorName: "Roof intake temperature",
    zone: "Zone A",
    commandType: "SetThreshold",
    parameters: "temp.max=28.5",
    origin: "Manual",
    status: "Sent",
    roundTripMs: null,
    issuedBy: "operator",
    retries: 0,
  },
  {
    id: "cmd-1040",
    issuedUtc: minutesAgo(1.4),
    nodeId: "NODE-C03",
    sensorName: "Plant room pressure",
    zone: "Zone C",
    commandType: "RequestSample",
    parameters: "count=5",
    origin: "Automation",
    status: "Acknowledged",
    roundTripMs: 142,
    issuedBy: "anomaly-rule-7",
    retries: 0,
  },
  {
    id: "cmd-1039",
    issuedUtc: minutesAgo(2.6),
    nodeId: "NODE-B21",
    sensorName: "East corridor motion",
    zone: "Zone B",
    commandType: "ToggleActuator",
    parameters: "relay=1,state=on",
    origin: "Manual",
    status: "Acknowledged",
    roundTripMs: 318,
    issuedBy: "operator",
    retries: 0,
  },
  {
    id: "cmd-1038",
    issuedUtc: minutesAgo(4.1),
    nodeId: "NODE-D07",
    sensorName: "Substation power draw",
    zone: "Zone D",
    commandType: "Recalibrate",
    parameters: "offset=auto",
    origin: "Schedule",
    status: "Failed",
    roundTripMs: null,
    issuedBy: "nightly-calibration",
    retries: 2,
  },
  {
    id: "cmd-1037",
    issuedUtc: minutesAgo(5.9),
    nodeId: "NODE-A02",
    sensorName: "Lobby humidity",
    zone: "Zone A",
    commandType: "SetThreshold",
    parameters: "humidity.min=35",
    origin: "Automation",
    status: "Acknowledged",
    roundTripMs: 97,
    issuedBy: "drift-rule-2",
    retries: 0,
  },
  {
    id: "cmd-1036",
    issuedUtc: minutesAgo(8.3),
    nodeId: "NODE-C11",
    sensorName: "Chiller vibration",
    zone: "Zone C",
    commandType: "RestartNode",
    parameters: "mode=soft",
    origin: "Manual",
    status: "Queued",
    roundTripMs: null,
    issuedBy: "operator",
    retries: 0,
  },
  {
    id: "cmd-1035",
    issuedUtc: minutesAgo(11.7),
    nodeId: "NODE-B04",
    sensorName: "Loading bay temperature",
    zone: "Zone B",
    commandType: "FirmwarePush",
    parameters: "v2.4.1",
    origin: "Schedule",
    status: "Acknowledged",
    roundTripMs: 2410,
    issuedBy: "rollout-14",
    retries: 1,
  },
  {
    id: "cmd-1034",
    issuedUtc: minutesAgo(14.2),
    nodeId: "NODE-D19",
    sensorName: "Yard gate actuator",
    zone: "Zone D",
    commandType: "ToggleActuator",
    parameters: "relay=2,state=off",
    origin: "Manual",
    status: "Expired",
    roundTripMs: null,
    issuedBy: "operator",
    retries: 3,
  },
  {
    id: "cmd-1033",
    issuedUtc: minutesAgo(17.5),
    nodeId: "NODE-A14",
    sensorName: "Roof intake temperature",
    zone: "Zone A",
    commandType: "RequestSample",
    parameters: "count=1",
    origin: "Automation",
    status: "Acknowledged",
    roundTripMs: 121,
    issuedBy: "anomaly-rule-7",
    retries: 0,
  },
  {
    id: "cmd-1032",
    issuedUtc: minutesAgo(21.9),
    nodeId: "NODE-C03",
    sensorName: "Plant room pressure",
    zone: "Zone C",
    commandType: "Recalibrate",
    parameters: "offset=-0.4",
    origin: "Manual",
    status: "Acknowledged",
    roundTripMs: 655,
    issuedBy: "operator",
    retries: 0,
  },
];

/** Commands per minute for the last 30 minutes, oldest first. */
export const PLACEHOLDER_THROUGHPUT = [
  4, 6, 5, 9, 7, 6, 11, 14, 9, 8, 6, 5, 7, 12, 18, 15, 11, 9, 7, 6, 8, 10, 13, 9,
  7, 6, 5, 8, 12, 10,
];

/** Node ids offered by the override console's target picker. */
export const PLACEHOLDER_NODES = [
  "NODE-A02",
  "NODE-A14",
  "NODE-B04",
  "NODE-B21",
  "NODE-C03",
  "NODE-C11",
  "NODE-D07",
  "NODE-D19",
];

export const PLACEHOLDER_ZONES = ["Zone A", "Zone B", "Zone C", "Zone D"];
