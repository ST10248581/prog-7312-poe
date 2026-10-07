import { useState } from "react";
import type { FormEvent } from "react";
import { lookupDevice, recordActivity } from "../../services/apiService";
import type { DeviceLookupResult } from "../../services/apiService";
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
  const [lookupKey, setLookupKey] = useState("");
  const [lookup, setLookup] = useState<DeviceLookupResult | null>(null);
  const [lookupError, setLookupError] = useState<string | null>(null);
  const [lookingUp, setLookingUp] = useState(false);

  // Exact lookup against the API's registry dictionaries (node id, then MAC in
  // any notation). Unlike the filter search, which narrows the list, this is a
  // single O(1) probe, and the API reports how long it took.
  const handleLookup = async (event: FormEvent) => {
    event.preventDefault();
    const key = lookupKey.trim();
    if (!key) return;

    setLookingUp(true);
    setLookupError(null);
    try {
      const result = await lookupDevice(key);
      setLookup(result);
      if (result.found) {
        recordActivity("Search", key);
      }
    } catch (err) {
      setLookup(null);
      setLookupError(err instanceof Error ? err.message : "Lookup failed.");
    } finally {
      setLookingUp(false);
    }
  };

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

      <form className="device-lookup" onSubmit={handleLookup} role="search">
        <label className="device-lookup-label" htmlFor="device-lookup-key">
          Instant lookup
        </label>
        <input
          id="device-lookup-key"
          className="device-lookup-input"
          value={lookupKey}
          onChange={(event) => setLookupKey(event.target.value)}
          placeholder="Node ID or MAC, e.g. ENV-001 or 5c-a1-2a-2c-3c-6f"
          spellCheck={false}
          autoComplete="off"
        />
        <button type="submit" className="filter-toggle" disabled={lookingUp || lookupKey.trim() === ""}>
          {lookingUp ? "Looking up…" : "Find"}
        </button>
      </form>

      {lookupError && <p className="device-lookup-result miss">{lookupError}</p>}

      {lookup && (
        <div className={`device-lookup-result${lookup.found ? " hit" : " miss"}`} role="status">
          <p className="device-lookup-summary">
            {lookup.found ? (
              <>
                Found <strong>{lookup.device?.nodeId}</strong> via the{" "}
                {lookup.matchedBy === "MacAddress" ? "MAC address" : "node ID"} dictionary in{" "}
                <strong>{(lookup.elapsedMicroseconds / 1000).toFixed(4)} ms</strong>
              </>
            ) : (
              <>No device is registered under <code>{lookup.normalisedKey}</code> (checked in {(lookup.elapsedMicroseconds / 1000).toFixed(4)} ms)</>
            )}
            <span className="device-lookup-meta">
              {" "}· one hash probe, {lookup.registrySize} entries · key <code>{lookup.normalisedKey}</code>
            </span>
            <button type="button" className="device-lookup-clear" onClick={() => setLookup(null)} aria-label="Clear lookup">
              ✕
            </button>
          </p>
          {lookup.device && (
            <div className="live-device-grid">
              <DeviceCard device={lookup.device} selected={lookup.device.nodeId === selectedNode} onInspect={onInspect} />
            </div>
          )}
        </div>
      )}

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
