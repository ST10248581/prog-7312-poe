import { useState } from "react";
import Sparkline from "../telemetry/Sparkline";
import { formatRelative, humanise } from "../../utils/format";
import type { LiveDevice, LiveDeviceResult, LiveReading } from "../../services/apiService";

/** Cards shown before "Show all" — enough to fill two rows without burying the stream. */
const COLLAPSED_COUNT = 12;

interface LiveDevicePanelProps {
  /** Straight from `/api/commands/devices`; null until it arrives. */
  result: LiveDeviceResult | null;
  error: string | null;
  selectedNode: string;
  /** True when a filter narrows the list, so an empty result can say why. */
  filtered: boolean;
  onInspect: (nodeId: string) => void;
}

/**
 * The incoming sensor data, visualised: every registered device that matches
 * the page filter, with each metric's latest value and a short trail of the
 * readings behind it.
 *
 * The API returns the whole matching fleet in one response, already filtered
 * by device category, alert state, severity, zone and search, and ordered
 * worst-first — this panel only lays it out. Selecting a card aims the override
 * console and the timeline at that device.
 */
function LiveDevicePanel({ result, error, selectedNode, filtered, onInspect }: LiveDevicePanelProps) {
  const [expanded, setExpanded] = useState(false);

  const devices = result?.items ?? [];
  const visible = expanded ? devices : devices.slice(0, COLLAPSED_COUNT);
  const alerting = devices.filter((device) => device.alertState === "Active").length;
  const offline = devices.filter((device) => device.isDisconnected).length;

  return (
    <section className="live-devices" aria-label="Live device readings">
      <header className="panel-head">
        <h2>Live device readings</h2>
        <span className="panel-head-count">
          {result
            ? `${devices.length} of ${result.totalRegistered} devices · ${alerting} alerting · ${offline} offline`
            : "loading…"}
        </span>
      </header>

      {error && !result && <p className="panel-empty">Unable to load devices: {error}</p>}

      {result && devices.length === 0 && (
        <p className="panel-empty">
          {filtered
            ? "No device matches the current filter. Device category, node alert, severity, zone and search all apply here."
            : "No devices are registered."}
        </p>
      )}

      {devices.length > 0 && (
        <div className="live-device-grid">
          {visible.map((device) => (
            <DeviceCard
              key={device.nodeId}
              device={device}
              selected={device.nodeId === selectedNode}
              onInspect={onInspect}
            />
          ))}
        </div>
      )}

      {devices.length > COLLAPSED_COUNT && (
        <button
          type="button"
          className="live-device-more"
          onClick={() => setExpanded((value) => !value)}
        >
          {expanded ? "Show fewer" : `Show all ${devices.length} devices`}
        </button>
      )}
    </section>
  );
}

interface DeviceCardProps {
  device: LiveDevice;
  selected: boolean;
  onInspect: (nodeId: string) => void;
}

function DeviceCard({ device, selected, onInspect }: DeviceCardProps) {
  const alerting = device.alertState === "Active" || device.alertState === "Acknowledged";
  const tone = device.isDisconnected
    ? "offline"
    : device.alertState === "Active"
      ? (device.alertSeverity ?? "warning").toLowerCase()
      : "ok";

  // A div with button semantics rather than a <button>: the card holds a list,
  // which a button's content model does not allow.
  return (
    <div
      role="button"
      tabIndex={0}
      aria-pressed={selected}
      className={`live-device live-device-${tone}${selected ? " selected" : ""}`}
      onClick={() => onInspect(device.nodeId)}
      onKeyDown={(event) => {
        if (event.key === "Enter" || event.key === " ") {
          event.preventDefault();
          onInspect(device.nodeId);
        }
      }}
      title={`${device.macAddress} — inspect ${device.nodeId}`}
    >
      <div className="live-device-head">
        <span className="live-device-node">{device.nodeId}</span>
        {alerting && (
          <span
            className={`node-alert node-alert-${device.alertState.toLowerCase()} severity-${(
              device.alertSeverity ?? "Info"
            ).toLowerCase()}`}
            title={`${device.openAlertCount} open alert${device.openAlertCount === 1 ? "" : "s"}, ${device.alertState.toLowerCase()}`}
          >
            <span className="node-alert-dot" aria-hidden="true" />
            {device.openAlertCount}
          </span>
        )}
        <span className={`live-device-link${device.isDisconnected ? " down" : ""}`}>
          {device.isDisconnected ? "offline" : device.lastReadingUtc ? formatRelative(device.lastReadingUtc) : "no data"}
        </span>
      </div>

      <div className="live-device-meta">
        {humanise(device.category)} · {device.zone}
        {device.room ? ` · ${device.room}` : ""}
      </div>

      <ul className="live-device-readings">
        {device.readings.map((reading) => (
          <ReadingRow key={reading.readingType} reading={reading} offline={device.isDisconnected} />
        ))}
      </ul>
    </div>
  );
}

function ReadingRow({ reading, offline }: { reading: LiveReading; offline: boolean }) {
  const status = offline ? "Offline" : reading.outOfRange ? "Warning" : "Online";

  const value =
    reading.value === null
      ? "—"
      : reading.isBoolean
        ? reading.value >= 0.5
          ? "detected"
          : "idle"
        : `${reading.value.toFixed(reading.value >= 100 ? 0 : 2)} ${reading.unit}`;

  const limits =
    reading.minThreshold !== null && reading.maxThreshold !== null
      ? `Limits ${reading.minThreshold}–${reading.maxThreshold} ${reading.unit}`
      : undefined;

  return (
    <li className="live-reading" title={limits}>
      <span className="live-reading-type">{reading.readingType}</span>
      <span className={`live-reading-value${reading.outOfRange ? " out" : ""}`}>{value}</span>
      <Sparkline values={reading.recent} status={status} />
    </li>
  );
}

export default LiveDevicePanel;
