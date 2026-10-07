import { useCallback, useEffect, useRef, useState } from "react";
import type { FormEvent } from "react";
import { ApiError, createSensor } from "../../services/apiService";
import type { SensorCategory } from "../../services/apiService";
import RegistrationFields from "./RegistrationFields";
import { useRegistrationForm } from "../../hooks/useRegistrationForm";

interface RegisterSensorModalProps {
  /** Zones reported by the API, for the zone dropdown. */
  zones?: string[];
  onClose: () => void;
  onRegistered: () => void;
}

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

function RegisterSensorModal({ zones, onClose, onRegistered }: RegisterSensorModalProps) {
  const dialogRef = useRef<HTMLDivElement>(null);
  const formState = useRegistrationForm(emptyForm());
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

  const { form, isValid, markSubmitted, reset, setServerErrors } = formState;

  const handleSubmit = useCallback(
    async (event: FormEvent) => {
      event.preventDefault();
      markSubmitted();
      if (!isValid) {
        return;
      }

      setSaving(true);
      setMessage(null);
      try {
        const created = await createSensor({ ...form, name: form.name.trim(), room: form.room.trim() });
        setMessage({ type: "success", text: `${created.nodeId} registered. It will show as offline until it first reports.` });
        reset(emptyForm());
        onRegistered();
      } catch (err) {
        if (err instanceof ApiError && Object.keys(err.fieldErrors).length > 0) {
          // 400 or 409: the API named the field, so the message goes under it.
          setServerErrors(err.fieldErrors);
          setMessage({
            type: "error",
            text: err.status === 409 ? "That device is already registered." : "Please correct the highlighted fields.",
          });
        } else {
          setMessage({ type: "error", text: err instanceof Error ? err.message : "Failed to register device." });
        }
      } finally {
        setSaving(false);
      }
    },
    [form, isValid, markSubmitted, reset, setServerErrors, onRegistered]
  );

  return (
    <div className="modal-backdrop" onClick={onClose} role="presentation">
      <div
        ref={dialogRef}
        className="modal-dialog register-modal-dialog"
        role="dialog"
        aria-modal="true"
        aria-labelledby="register-sensor-title"
        tabIndex={-1}
        onClick={(event) => event.stopPropagation()}
      >
        <section className="detail-panel">
          <header className="detail-head">
            <div className="detail-head-main">
              <div>
                <h2 id="register-sensor-title" className="detail-title">Register New Device</h2>
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
            <form className="payload-form" noValidate onSubmit={handleSubmit}>
              <RegistrationFields state={formState} zones={zones} disabled={saving} />

              {message && (
                <div className={`payload-message payload-message-${message.type}`} role="status">
                  {message.text}
                </div>
              )}

              <div className="payload-actions">
                <button
                  type="submit"
                  className="payload-btn payload-btn-save"
                  disabled={saving || !isValid}
                  title={!isValid ? "Complete every field to register" : undefined}
                >
                  {saving ? "Registering…" : "Register Device"}
                </button>
                <button
                  type="button"
                  className="payload-btn payload-btn-reset"
                  disabled={saving}
                  onClick={() => {
                    reset(emptyForm());
                    setMessage(null);
                  }}
                >
                  Clear
                </button>
              </div>
            </form>
          </div>
        </section>
      </div>
    </div>
  );
}

export default RegisterSensorModal;
