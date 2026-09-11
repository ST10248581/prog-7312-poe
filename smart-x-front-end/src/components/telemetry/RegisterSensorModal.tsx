import { useCallback, useEffect, useRef, useState } from "react";
import type { SensorCategory } from "../../services/apiService";
import { createSensor } from "../../services/apiService";
import { humanise } from "../../utils/format";

interface RegisterSensorModalProps {
  onClose: () => void;
  onRegistered: () => void;
}

const SENSOR_CATEGORIES: SensorCategory[] = [
  "Environmental",
  "PowerConsumption",
  "Actuator",
  "Motion",
  "Connectivity",
];

function emptyForm() {
  return {
    name: "",
    macAddress: "",
    room: "",
    zone: "",
    nodeId: "",
    category: "Environmental" as SensorCategory,
  };
}

function RegisterSensorModal({ onClose, onRegistered }: RegisterSensorModalProps) {
  const dialogRef = useRef<HTMLDivElement>(null);
  const [form, setForm] = useState(emptyForm);
  const [saving, setSaving] = useState(false);
  const [message, setMessage] = useState<{ type: "success" | "error"; text: string } | null>(null);

  useEffect(() => {
    const handleKey = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        onClose();
      }
    };

    document.addEventListener("keydown", handleKey);
    const previousOverflow = document.body.style.overflow;
    document.body.style.overflow = "hidden";
    dialogRef.current?.focus();

    return () => {
      document.removeEventListener("keydown", handleKey);
      document.body.style.overflow = previousOverflow;
    };
  }, [onClose]);

  const handleSave = useCallback(async () => {
    if (!form.name.trim()) {
      setMessage({ type: "error", text: "Device name is required." });
      return;
    }
    if (!form.macAddress.trim()) {
      setMessage({ type: "error", text: "MAC address is required." });
      return;
    }

    setSaving(true);
    setMessage(null);
    try {
      await createSensor(form);
      setMessage({ type: "success", text: "Device registered successfully." });
      setForm(emptyForm());
      onRegistered();
    } catch (err) {
      setMessage({
        type: "error",
        text: err instanceof Error ? err.message : "Failed to register device.",
      });
    } finally {
      setSaving(false);
    }
  }, [form, onRegistered]);

  return (
    <div className="modal-backdrop" onClick={onClose} role="presentation">
      <div
        ref={dialogRef}
        className="modal-dialog register-modal-dialog"
        role="dialog"
        aria-modal="true"
        aria-label="Register new sensor"
        tabIndex={-1}
        onClick={(event) => event.stopPropagation()}
      >
        <section className="detail-panel">
          <header className="detail-head">
            <div className="detail-head-main">
              <div>
                <h2 className="detail-title">Register New Device</h2>
                <div className="detail-subtitle">
                  Add a new sensor to the mesh by providing its registration details.
                </div>
              </div>
            </div>
            <button type="button" className="detail-close" onClick={onClose} aria-label="Close">
              ✕
            </button>
          </header>

          <div className="detail-scroll">
            <div className="payload-form">
              <label className="payload-field">
                <span className="payload-field-label">Device Name</span>
                <input
                  type="text"
                  className="payload-input"
                  value={form.name}
                  placeholder="e.g. Temp Sensor Floor 2"
                  onChange={(e) => setForm({ ...form, name: e.target.value })}
                />
              </label>

              <label className="payload-field">
                <span className="payload-field-label">MAC Address / Unique Identifier</span>
                <input
                  type="text"
                  className="payload-input"
                  value={form.macAddress}
                  placeholder="e.g. AA:BB:CC:DD:EE:FF"
                  onChange={(e) => setForm({ ...form, macAddress: e.target.value })}
                />
              </label>

              <label className="payload-field">
                <span className="payload-field-label">Room</span>
                <input
                  type="text"
                  className="payload-input"
                  value={form.room}
                  placeholder="e.g. Server Room A"
                  onChange={(e) => setForm({ ...form, room: e.target.value })}
                />
              </label>

              <label className="payload-field">
                <span className="payload-field-label">Zone</span>
                <input
                  type="text"
                  className="payload-input"
                  value={form.zone}
                  placeholder="e.g. Zone A"
                  onChange={(e) => setForm({ ...form, zone: e.target.value })}
                />
              </label>

              <label className="payload-field">
                <span className="payload-field-label">Node ID</span>
                <input
                  type="text"
                  className="payload-input"
                  value={form.nodeId}
                  placeholder="e.g. NODE-001"
                  onChange={(e) => setForm({ ...form, nodeId: e.target.value })}
                />
              </label>

              <label className="payload-field">
                <span className="payload-field-label">Sensor Category</span>
                <select
                  className="payload-input"
                  value={form.category}
                  onChange={(e) =>
                    setForm({ ...form, category: e.target.value as SensorCategory })
                  }
                >
                  {SENSOR_CATEGORIES.map((cat) => (
                    <option key={cat} value={cat}>
                      {humanise(cat)}
                    </option>
                  ))}
                </select>
              </label>

              {message && (
                <div className={`payload-message payload-message-${message.type}`}>
                  {message.text}
                </div>
              )}

              <div className="payload-actions">
                <button
                  type="button"
                  className="payload-btn payload-btn-save"
                  disabled={saving}
                  onClick={handleSave}
                >
                  {saving ? "Registering..." : "Register Device"}
                </button>
                <button
                  type="button"
                  className="payload-btn payload-btn-reset"
                  disabled={saving}
                  onClick={() => { setForm(emptyForm()); setMessage(null); }}
                >
                  Clear
                </button>
              </div>
            </div>
          </div>
        </section>
      </div>
    </div>
  );
}

export default RegisterSensorModal;
