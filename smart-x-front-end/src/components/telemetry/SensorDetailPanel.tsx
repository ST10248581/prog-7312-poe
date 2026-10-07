import { useCallback, useEffect, useRef, useState } from "react";
import type { DragEvent, FormEvent } from "react";
import type { AttachmentType, SensorDetail, SensorProfile } from "../../services/apiService";
import {
  ApiError,
  getAttachmentDownloadUrl,
  updateSensorPayload,
  uploadAttachment,
} from "../../services/apiService";
import { acceptAttribute, allowedExtensions, validateAttachment } from "../../utils/validation";
import {
  formatBytes,
  formatDateTime,
  formatRelative,
  formatTime,
  humanise,
} from "../../utils/format";
import LiveChart from "./LiveChart";
import RegistrationFields from "./RegistrationFields";
import { useRegistrationForm } from "../../hooks/useRegistrationForm";
import TroubleshootingGuide from "./TroubleshootingGuide";

interface SensorDetailPanelProps {
  detail: SensorDetail | null;
  loading: boolean;
  onClose: () => void;
  onPayloadUpdated?: (updatedDetail: SensorDetail) => void;
  onDetailRefresh?: () => void;
  /** Zones reported by the API, for the registration form's zone dropdown. */
  zones?: string[];
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

const ATTACHMENT_TYPES: AttachmentType[] = [
  "ConfigFile",
  "DeploymentPhoto",
  "HardwareLog",
];

/**
 * Details on demand. Nothing here is shown at overview level; it opens only
 * once a specific node is being investigated.
 */
function registrationOf(profile: SensorProfile | undefined) {
  return {
    macAddress: profile?.macAddress ?? "",
    room: profile?.room ?? "",
    zone: profile?.zone ?? "",
    nodeId: profile?.nodeId ?? "",
    category: profile?.category ?? "Environmental",
  };
}

function SensorDetailPanel({ detail, loading, onClose, onPayloadUpdated, onDetailRefresh, zones }: SensorDetailPanelProps) {
  // The parent keys this component by sensor id, so opening a different node
  // remounts it and these both start fresh.
  const [tab, setTab] = useState<Tab>("readings");
  const [seriesIndex, setSeriesIndex] = useState(0);

  // Upload state
  const fileInputRef = useRef<HTMLInputElement>(null);
  const uploadAbortRef = useRef<AbortController | null>(null);
  const [uploadType, setUploadType] = useState<AttachmentType>("ConfigFile");
  const [uploadDesc, setUploadDesc] = useState("");
  const [uploadFile, setUploadFile] = useState<File | null>(null);
  const [uploadProgress, setUploadProgress] = useState<number | null>(null);
  const [dragActive, setDragActive] = useState(false);
  const [uploadMessage, setUploadMessage] = useState<{ type: "success" | "error"; text: string } | null>(null);

  const uploading = uploadProgress !== null;
  const fileError = uploadFile ? validateAttachment(uploadFile, uploadType) : null;

  // Cancel an in-flight upload if the panel closes.
  useEffect(() => () => uploadAbortRef.current?.abort(), []);

  const chooseFile = useCallback((file: File | null) => {
    setUploadFile(file);
    setUploadMessage(null);
  }, []);

  const handleDrop = useCallback(
    (event: DragEvent<HTMLLabelElement>) => {
      event.preventDefault();
      setDragActive(false);
      if (!uploading) {
        chooseFile(event.dataTransfer.files?.[0] ?? null);
      }
    },
    [chooseFile, uploading]
  );

  const handleUpload = useCallback(async (event: FormEvent) => {
    event.preventDefault();
    if (!uploadFile || !detail || fileError) {
      return;
    }

    const controller = new AbortController();
    uploadAbortRef.current = controller;
    setUploadProgress(0);
    setUploadMessage(null);

    try {
      const attachment = await uploadAttachment(detail.profile.id, uploadFile, uploadType, uploadDesc, {
        onProgress: setUploadProgress,
        signal: controller.signal,
      });
      setUploadMessage({
        type: "success",
        text: `"${attachment.fileName}" uploaded and encrypted. SHA-256 ${attachment.sha256.slice(0, 12)}…`,
      });
      setUploadDesc("");
      setUploadFile(null);
      if (fileInputRef.current) fileInputRef.current.value = "";
      onDetailRefresh?.();
    } catch (err) {
      if (controller.signal.aborted) {
        setUploadMessage({ type: "error", text: "Upload cancelled." });
      } else {
        setUploadMessage({
          type: "error",
          text: err instanceof ApiError ? (err.fieldErrors.file ?? err.message) : "Upload failed.",
        });
      }
    } finally {
      uploadAbortRef.current = null;
      setUploadProgress(null);
    }
  }, [detail, uploadFile, fileError, uploadType, uploadDesc, onDetailRefresh]);

  // Registration form state
  const registration = useRegistrationForm(registrationOf(detail?.profile));
  const [payloadSaving, setPayloadSaving] = useState(false);
  const [payloadMessage, setPayloadMessage] = useState<{ type: "success" | "error"; text: string } | null>(null);

  // Re-read the form from the profile when switching to the registration tab.
  const resetRegistration = registration.reset;
  const initPayloadForm = useCallback(() => {
    if (!detail) return;
    resetRegistration(registrationOf(detail.profile));
    setPayloadMessage(null);
  }, [detail, resetRegistration]);

  const handlePayloadSave = useCallback(async (event: FormEvent) => {
    event.preventDefault();
    registration.markSubmitted();
    if (!detail || !registration.isValid) return;

    setPayloadSaving(true);
    setPayloadMessage(null);
    try {
      const updatedProfile = await updateSensorPayload(detail.profile.id, registration.form);
      // Update the detail in place so the header reflects changes
      const updatedDetail: SensorDetail = {
        ...detail,
        profile: updatedProfile,
      };
      onPayloadUpdated?.(updatedDetail);
      registration.reset(registrationOf(updatedProfile));
      setPayloadMessage({ type: "success", text: "Registration updated successfully." });
    } catch (err) {
      if (err instanceof ApiError && Object.keys(err.fieldErrors).length > 0) {
        registration.setServerErrors(err.fieldErrors);
        setPayloadMessage({
          type: "error",
          text: err.status === 409 ? "Another device already uses that identity." : "Please correct the highlighted fields.",
        });
      } else {
        setPayloadMessage({
          type: "error",
          text: err instanceof Error ? err.message : "Failed to save registration.",
        });
      }
    } finally {
      setPayloadSaving(false);
    }
  }, [detail, registration, onPayloadUpdated]);

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
            <form className="upload-form" onSubmit={handleUpload} noValidate>
              <h3 className="upload-form-title">Upload Attachment</h3>

              <div className="upload-form-row">
                <label className="payload-field upload-field-type">
                  <span className="payload-field-label">Type</span>
                  <select
                    className="payload-input"
                    value={uploadType}
                    disabled={uploading}
                    onChange={(e) => {
                      setUploadType(e.target.value as AttachmentType);
                      setUploadMessage(null);
                    }}
                  >
                    {ATTACHMENT_TYPES.map((t) => (
                      <option key={t} value={t}>{humanise(t)}</option>
                    ))}
                  </select>
                </label>

                <label className="payload-field upload-field-desc">
                  <span className="payload-field-label">Description</span>
                  <input
                    type="text"
                    className="payload-input"
                    value={uploadDesc}
                    maxLength={200}
                    disabled={uploading}
                    placeholder="Optional description"
                    onChange={(e) => setUploadDesc(e.target.value)}
                  />
                </label>
              </div>

              {/* The whole zone is the file input's label, so clicking it or
                  dropping a file on it both choose the file. */}
              <label
                className={`upload-dropzone${dragActive ? " drag-active" : ""}${fileError ? " has-error" : ""}${uploading ? " is-uploading" : ""}`}
                onDragOver={(event) => {
                  event.preventDefault();
                  if (!uploading) setDragActive(true);
                }}
                onDragLeave={() => setDragActive(false)}
                onDrop={handleDrop}
              >
                <input
                  ref={fileInputRef}
                  type="file"
                  className="upload-dropzone-input"
                  accept={acceptAttribute(uploadType)}
                  disabled={uploading}
                  onChange={(e) => chooseFile(e.target.files?.[0] ?? null)}
                />
                <span className="upload-dropzone-icon" aria-hidden="true">⇪</span>
                {uploadFile ? (
                  <span className="upload-dropzone-file">
                    <span className="upload-dropzone-name">{uploadFile.name}</span>
                    <span className="upload-dropzone-meta">{formatBytes(uploadFile.size)} · click or drop to replace</span>
                  </span>
                ) : (
                  <span className="upload-dropzone-file">
                    <span className="upload-dropzone-name">Drop a file here or click to browse</span>
                    <span className="upload-dropzone-meta">
                      {allowedExtensions(uploadType).join(" ")} · up to 10 MB · encrypted at rest
                    </span>
                  </span>
                )}
              </label>

              {fileError && (
                <div className="payload-field-error" role="alert">{fileError}</div>
              )}

              {uploading && (
                <div className="upload-progress" role="progressbar" aria-label="Upload progress"
                  aria-valuemin={0} aria-valuemax={100} aria-valuenow={Math.round((uploadProgress ?? 0) * 100)}>
                  <div className="upload-progress-bar">
                    <div className="upload-progress-fill" style={{ width: `${(uploadProgress ?? 0) * 100}%` }} />
                  </div>
                  <span className="upload-progress-label">
                    {(uploadProgress ?? 0) < 1 ? `Uploading ${Math.round((uploadProgress ?? 0) * 100)}%` : "Encrypting…"}
                  </span>
                </div>
              )}

              {uploadMessage && (
                <div className={`payload-message payload-message-${uploadMessage.type}`} role="status">
                  {uploadMessage.text}
                </div>
              )}

              <div className="payload-actions">
                <button
                  type="submit"
                  className="payload-btn payload-btn-save"
                  disabled={uploading || !uploadFile || Boolean(fileError)}
                >
                  {uploading ? "Uploading…" : "Upload File"}
                </button>
                {uploading && (
                  <button
                    type="button"
                    className="payload-btn payload-btn-reset"
                    onClick={() => uploadAbortRef.current?.abort()}
                  >
                    Cancel
                  </button>
                )}
              </div>
            </form>

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
                    <span className="attachment-name" title={attachment.fileName}>{attachment.fileName}</span>
                    <span className="attachment-meta">
                      {humanise(attachment.attachmentType)} · {formatBytes(attachment.fileSizeBytes)} ·{" "}
                      {attachment.uploadedBy} · {formatDateTime(attachment.uploadedUtc)}
                    </span>
                    {attachment.description && (
                      <span className="attachment-desc">{attachment.description}</span>
                    )}
                    {attachment.isEncrypted && (
                      <span className="attachment-security" title={`SHA-256 ${attachment.sha256}`}>
                        🔒 AES-256-GCM · SHA-256 {attachment.sha256.slice(0, 12)}…
                      </span>
                    )}
                  </div>
                  {/* The API answers with Content-Disposition: attachment, so a
                      plain link downloads the file without leaving the page. */}
                  <a
                    className="attachment-download"
                    href={getAttachmentDownloadUrl(profile.id, attachment.id)}
                    download={attachment.fileName}
                    title={`Download ${attachment.fileName}`}
                    aria-label={`Download ${attachment.fileName}`}
                  >
                    ↓
                  </a>
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

        {tab === "registration" && (
          <form className="payload-form" noValidate onSubmit={handlePayloadSave}>
            <p className="payload-form-intro">
              Manage the sensor registration record for this device. Changes are applied immediately.
            </p>

            <RegistrationFields state={registration} zones={zones} disabled={payloadSaving} />

            {payloadMessage && (
              <div className={`payload-message payload-message-${payloadMessage.type}`} role="status">
                {payloadMessage.text}
              </div>
            )}

            <div className="payload-actions">
              <button
                type="submit"
                className="payload-btn payload-btn-save"
                disabled={payloadSaving || !registration.isValid}
              >
                {payloadSaving ? "Saving…" : "Save Registration"}
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
          </form>
        )}
      </div>

        <TroubleshootingGuide detail={detail} />
      </div>
    </section>
  );
}

export default SensorDetailPanel;
