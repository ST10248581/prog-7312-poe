import { useCallback, useRef, useState } from "react";
import type { AttachmentType, SensorCategory, SensorDetail } from "../../services/apiService";
import { getAttachmentDownloadUrl, updateSensorPayload, uploadAttachment } from "../../services/apiService";
import {
  formatBytes,
  formatDateTime,
  formatRelative,
  formatTime,
  humanise,
} from "../../utils/format";
import LiveChart from "./LiveChart";
import TroubleshootingGuide from "./TroubleshootingGuide";

interface SensorDetailPanelProps {
  detail: SensorDetail | null;
  loading: boolean;
  onClose: () => void;
  onPayloadUpdated?: (updatedDetail: SensorDetail) => void;
  onDetailRefresh?: () => void;
}

type Tab = "readings" | "thresholds" | "attachments" | "ingestion" | "alerts" | "registration";

const TABS: { id: Tab; label: string }[] = [
  { id: "readings", label: "Readings" },
  { id: "thresholds", label: "Thresholds" },
  { id: "attachments", label: "Attachments" },
  { id: "ingestion", label: "Ingestion" },
  { id: "alerts", label: "Alerts" },
  { id: "registration", label: "Registration" },
];

const SENSOR_CATEGORIES: SensorCategory[] = [
  "Environmental",
  "PowerConsumption",
  "Actuator",
  "Motion",
  "Connectivity",
];

const ATTACHMENT_TYPES: AttachmentType[] = [
  "ConfigFile",
  "DeploymentPhoto",
  "HardwareLog",
];

/**
 * Details on demand. Nothing here is shown at overview level; it opens only
 * once a specific node is being investigated.
 */
function SensorDetailPanel({ detail, loading, onClose, onPayloadUpdated, onDetailRefresh }: SensorDetailPanelProps) {
  // The parent keys this component by sensor id, so opening a different node
  // remounts it and these both start fresh.
  const [tab, setTab] = useState<Tab>("readings");
  const [seriesIndex, setSeriesIndex] = useState(0);

  // Upload state
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [uploadType, setUploadType] = useState<AttachmentType>("ConfigFile");
  const [uploadDesc, setUploadDesc] = useState("");
  const [uploading, setUploading] = useState(false);
  const [uploadMessage, setUploadMessage] = useState<{ type: "success" | "error"; text: string } | null>(null);

  const handleUpload = useCallback(async () => {
    const file = fileInputRef.current?.files?.[0];
    if (!file || !detail) {
      setUploadMessage({ type: "error", text: "Please select a file." });
      return;
    }

    setUploading(true);
    setUploadMessage(null);
    try {
      await uploadAttachment(detail.profile.id, file, uploadType, uploadDesc);
      setUploadMessage({ type: "success", text: `"${file.name}" uploaded successfully.` });
      setUploadDesc("");
      if (fileInputRef.current) fileInputRef.current.value = "";
      onDetailRefresh?.();
    } catch (err) {
      setUploadMessage({
        type: "error",
        text: err instanceof Error ? err.message : "Upload failed.",
      });
    } finally {
      setUploading(false);
    }
  }, [detail, uploadType, uploadDesc, onDetailRefresh]);

  // Registration form state
  const [payloadForm, setPayloadForm] = useState<{
    macAddress: string;
    room: string;
    zone: string;
    nodeId: string;
    category: SensorCategory;
  } | null>(null);
  const [payloadSaving, setPayloadSaving] = useState(false);
  const [payloadMessage, setPayloadMessage] = useState<{ type: "success" | "error"; text: string } | null>(null);

  // Initialise the form from the profile when switching to the registration tab
  const initPayloadForm = useCallback(() => {
    if (!detail) return;
    setPayloadForm({
      macAddress: detail.profile.macAddress,
      room: detail.profile.room,
      zone: detail.profile.zone,
      nodeId: detail.profile.nodeId,
      category: detail.profile.category,
    });
    setPayloadMessage(null);
  }, [detail]);

  const handlePayloadSave = useCallback(async () => {
    if (!detail || !payloadForm) return;
    setPayloadSaving(true);
    setPayloadMessage(null);
    try {
      const updatedProfile = await updateSensorPayload(detail.profile.id, payloadForm);
      // Update the detail in place so the header reflects changes
      const updatedDetail: SensorDetail = {
        ...detail,
        profile: updatedProfile,
      };
      onPayloadUpdated?.(updatedDetail);
      setPayloadMessage({ type: "success", text: "Registration updated successfully." });
    } catch (err) {
      setPayloadMessage({
        type: "error",
        text: err instanceof Error ? err.message : "Failed to save registration.",
      });
    } finally {
      setPayloadSaving(false);
    }
  }, [detail, payloadForm, onPayloadUpdated]);

  if (loading && !detail) {
    return (
      <section className="detail-panel">
        <div className="panel-empty">Loading node…</div>
      </section>
    );
  }

  if (!detail) {
    return (
      <section className="detail-panel detail-panel-idle">
        <div className="detail-idle">
          <span className="detail-idle-icon">⬡</span>
          <h2>Select a node to investigate</h2>
          <p>
            The overview and alert feed stay above. Choosing a sensor opens its live
            series, thresholds, attachments and ingestion history here.
          </p>
        </div>
      </section>
    );
  }

  const { profile, series, thresholds, attachments, recentBatches, recentReadings, alerts } = detail;
  const activeSeries = series[Math.min(seriesIndex, Math.max(series.length - 1, 0))];
  const status = profile.status.toLowerCase();

  return (
    <section className="detail-panel">
      <header className="detail-head">
        <div className="detail-head-main">
          <span className={`status-dot large status-${status}`} aria-hidden="true" />
          <div>
            <h2 className="detail-title">
              {profile.name}
              <span className="detail-node">{profile.nodeId}</span>
            </h2>
            <div className="detail-subtitle">
              {humanise(profile.category)} · {profile.zone} · {profile.room} ·{" "}
              <code>{profile.macAddress}</code>
            </div>
          </div>
        </div>

        <button type="button" className="detail-close" onClick={onClose} aria-label="Close details">
          ✕
        </button>
      </header>

      {/* Everything below the header scrolls, so the dialog keeps a fixed
          height and the title bar stays put. */}
      <div className="detail-scroll">
        <div className="detail-facts">
        <div className="detail-fact">
          <span className="detail-fact-label">Status</span>
          <span className={`detail-fact-value status-text-${status}`}>{profile.status}</span>
        </div>
        <div className="detail-fact">
          <span className="detail-fact-label">Last seen</span>
          <span className="detail-fact-value">{formatRelative(profile.lastSeenUtc)}</span>
        </div>
        <div className="detail-fact">
          <span className="detail-fact-label">Firmware</span>
          <span className="detail-fact-value">{profile.firmwareVersion}</span>
        </div>
        <div className="detail-fact">
          <span className="detail-fact-label">Serial</span>
          <span className="detail-fact-value">{profile.serialNumber}</span>
        </div>
        <div className="detail-fact">
          <span className="detail-fact-label">Registered</span>
          <span className="detail-fact-value">{formatDateTime(profile.registeredUtc)}</span>
        </div>
      </div>

      {series.length > 0 && activeSeries && (
        <div className="detail-chart-block">
          <div className="detail-chart-head">
            <div className="series-tabs">
              {series.map((entry, index) => (
                <button
                  key={entry.readingType}
                  type="button"
                  className={`series-tab${index === seriesIndex ? " active" : ""}`}
                  onClick={() => setSeriesIndex(index)}
                >
                  {entry.readingType}
                  {entry.anomalyCount > 0 && (
                    <span className="series-tab-flag">{entry.anomalyCount}</span>
                  )}
                </button>
              ))}
            </div>

            <div className="detail-chart-latest">
              {activeSeries.latestValue !== null && (
                <>
                  <span className="detail-chart-value">{activeSeries.latestValue.toFixed(2)}</span>
                  <span className="detail-chart-unit">{activeSeries.unit}</span>
                </>
              )}
            </div>
          </div>

          {activeSeries.isStale && (
            <div className="stale-notice">
              This node stopped reporting. Showing the window up to its last reading
              {activeSeries.lastReadingUtc && ` (${formatDateTime(activeSeries.lastReadingUtc)})`}.
            </div>
          )}

          <LiveChart series={activeSeries} />
        </div>
      )}

      <nav className="detail-tabs">
        {TABS.map((entry) => (
          <button
            key={entry.id}
            type="button"
            className={`detail-tab${tab === entry.id ? " active" : ""}`}
            onClick={() => {
              setTab(entry.id);
              if (entry.id === "registration") {
                initPayloadForm();
              }
            }}
          >
            {entry.label}
          </button>
        ))}
      </nav>

      <div className="detail-tab-body">
        {tab === "readings" && (
          <table className="data-table">
            <thead>
              <tr>
                <th>Time</th>
                <th>Type</th>
                <th>Value</th>
                <th>Quality</th>
              </tr>
            </thead>
            <tbody>
              {recentReadings.map((reading) => (
                <tr key={reading.id} className={reading.isAnomaly ? "row-anomaly" : undefined}>
                  <td>{formatTime(reading.timestampUtc)}</td>
                  <td>{reading.readingType}</td>
                  <td className="mono">{reading.textValue ?? "—"}</td>
                  <td>
                    <span className={`quality-pill quality-${reading.quality.toLowerCase()}`}>
                      {reading.quality}
                    </span>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}

        {tab === "thresholds" && (
          <table className="data-table">
            <thead>
              <tr>
                <th>Reading type</th>
                <th>Min</th>
                <th>Max</th>
                <th>Severity</th>
                <th>State</th>
              </tr>
            </thead>
            <tbody>
              {thresholds.map((threshold) => (
                <tr key={threshold.id}>
                  <td>{threshold.readingType}</td>
                  <td className="mono">{threshold.minValue?.toFixed(2) ?? "—"}</td>
                  <td className="mono">{threshold.maxValue?.toFixed(2) ?? "—"}</td>
                  <td>
                    <span className={`alert-severity severity-${threshold.severity.toLowerCase()}`}>
                      {threshold.severity}
                    </span>
                  </td>
                  <td>{threshold.isEnabled ? "Enabled" : "Disabled"}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}

        {tab === "attachments" && (
          <div className="attachments-section">
            <div className="upload-form">
              <h3 className="upload-form-title">Upload Attachment</h3>
              <div className="upload-form-row">
                <label className="payload-field upload-field-file">
                  <span className="payload-field-label">File</span>
                  <input
                    ref={fileInputRef}
                    type="file"
                    className="upload-file-input"
                  />
                </label>
                <label className="payload-field upload-field-type">
                  <span className="payload-field-label">Type</span>
                  <select
                    className="payload-input"
                    value={uploadType}
                    onChange={(e) => setUploadType(e.target.value as AttachmentType)}
                  >
                    {ATTACHMENT_TYPES.map((t) => (
                      <option key={t} value={t}>{humanise(t)}</option>
                    ))}
                  </select>
                </label>
              </div>
              <label className="payload-field">
                <span className="payload-field-label">Description</span>
                <input
                  type="text"
                  className="payload-input"
                  value={uploadDesc}
                  placeholder="Optional description"
                  onChange={(e) => setUploadDesc(e.target.value)}
                />
              </label>
              {uploadMessage && (
                <div className={`payload-message payload-message-${uploadMessage.type}`}>
                  {uploadMessage.text}
                </div>
              )}
              <div className="payload-actions">
                <button
                  type="button"
                  className="payload-btn payload-btn-save"
                  disabled={uploading}
                  onClick={handleUpload}
                >
                  {uploading ? "Uploading..." : "Upload File"}
                </button>
              </div>
            </div>

            <ul className="attachment-list">
              {attachments.length === 0 && <li className="panel-empty">No files attached.</li>}
              {attachments.map((attachment) => (
                <li key={attachment.id} className="attachment-item">
                  <span className={`attachment-icon type-${attachment.attachmentType.toLowerCase()}`}>
                    {attachment.attachmentType === "DeploymentPhoto"
                      ? "🖼"
                      : attachment.attachmentType === "ConfigFile"
                        ? "⚙"
                        : "📄"}
                  </span>
                  <div className="attachment-body">
                    <span className="attachment-name">{attachment.fileName}</span>
                    <span className="attachment-meta">
                      {humanise(attachment.attachmentType)} · {formatBytes(attachment.fileSizeBytes)} ·{" "}
                      {attachment.uploadedBy} · {formatDateTime(attachment.uploadedUtc)}
                    </span>
                    <span className="attachment-desc">{attachment.description}</span>
                  </div>
                  <button
                    type="button"
                    className="attachment-download"
                    title="Download"
                    onClick={() => {
                      const url = getAttachmentDownloadUrl(profile.id, attachment.id);
                      const iframe = document.createElement("iframe");
                      iframe.style.display = "none";
                      iframe.src = url;
                      document.body.appendChild(iframe);
                      setTimeout(() => iframe.remove(), 30000);
                    }}
                  >
                    ↓
                  </button>
                </li>
              ))}
            </ul>
          </div>
        )}

        {tab === "ingestion" && (
          <table className="data-table">
            <thead>
              <tr>
                <th>Received</th>
                <th>Readings</th>
                <th>Accepted</th>
                <th>Rejected</th>
                <th>Source</th>
                <th>Time</th>
              </tr>
            </thead>
            <tbody>
              {recentBatches.map((batch) => (
                <tr key={batch.id} className={batch.rejectedCount > 0 ? "row-anomaly" : undefined}>
                  <td>{formatDateTime(batch.receivedUtc)}</td>
                  <td className="mono">{batch.readingCount}</td>
                  <td className="mono">{batch.acceptedCount}</td>
                  <td className="mono">{batch.rejectedCount}</td>
                  <td className="mono">{batch.sourceIpAddress}</td>
                  <td className="mono">{batch.processingMs} ms</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}

        {tab === "alerts" && (
          <ul className="detail-alert-list">
            {alerts.length === 0 && <li className="panel-empty">No alerts recorded.</li>}
            {alerts.slice(0, 20).map((alert) => (
              <li key={alert.id} className={`detail-alert severity-${alert.severity.toLowerCase()}`}>
                <div className="detail-alert-head">
                  <span className={`alert-severity severity-${alert.severity.toLowerCase()}`}>
                    {alert.severity}
                  </span>
                  <span className="alert-type">{humanise(alert.alertType)}</span>
                  <span className={`alert-status status-${alert.status.toLowerCase()}`}>
                    {alert.status}
                  </span>
                  <span className="alert-time">{formatDateTime(alert.triggeredUtc)}</span>
                </div>
                <p className="alert-message">{alert.message}</p>
              </li>
            ))}
          </ul>
        )}

        {tab === "registration" && payloadForm && (
          <div className="payload-form">
            <p className="payload-form-intro">
              Manage the sensor registration record for this device. Changes are applied immediately.
            </p>

            <label className="payload-field">
              <span className="payload-field-label">MAC Address / Unique Identifier</span>
              <input
                type="text"
                className="payload-input"
                value={payloadForm.macAddress}
                placeholder="e.g. AA:BB:CC:DD:EE:FF"
                onChange={(e) => setPayloadForm({ ...payloadForm, macAddress: e.target.value })}
              />
            </label>

            <label className="payload-field">
              <span className="payload-field-label">Room</span>
              <input
                type="text"
                className="payload-input"
                value={payloadForm.room}
                placeholder="e.g. Server Room A"
                onChange={(e) => setPayloadForm({ ...payloadForm, room: e.target.value })}
              />
            </label>

            <label className="payload-field">
              <span className="payload-field-label">Zone</span>
              <input
                type="text"
                className="payload-input"
                value={payloadForm.zone}
                placeholder="e.g. Zone A"
                onChange={(e) => setPayloadForm({ ...payloadForm, zone: e.target.value })}
              />
            </label>

            <label className="payload-field">
              <span className="payload-field-label">Node ID</span>
              <input
                type="text"
                className="payload-input"
                value={payloadForm.nodeId}
                placeholder="e.g. NODE-001"
                onChange={(e) => setPayloadForm({ ...payloadForm, nodeId: e.target.value })}
              />
            </label>

            <label className="payload-field">
              <span className="payload-field-label">Sensor Category</span>
              <select
                className="payload-input"
                value={payloadForm.category}
                onChange={(e) =>
                  setPayloadForm({ ...payloadForm, category: e.target.value as SensorCategory })
                }
              >
                {SENSOR_CATEGORIES.map((cat) => (
                  <option key={cat} value={cat}>
                    {humanise(cat)}
                  </option>
                ))}
              </select>
            </label>

            {payloadMessage && (
              <div className={`payload-message payload-message-${payloadMessage.type}`}>
                {payloadMessage.text}
              </div>
            )}

            <div className="payload-actions">
              <button
                type="button"
                className="payload-btn payload-btn-save"
                disabled={payloadSaving}
                onClick={handlePayloadSave}
              >
                {payloadSaving ? "Saving..." : "Save Registration"}
              </button>
              <button
                type="button"
                className="payload-btn payload-btn-reset"
                disabled={payloadSaving}
                onClick={initPayloadForm}
              >
                Reset
              </button>
            </div>
          </div>
        )}
      </div>

        <TroubleshootingGuide detail={detail} />
      </div>
    </section>
  );
}

export default SensorDetailPanel;
