import { useCallback, useMemo, useRef, useState } from "react";
import type { ReactNode } from "react";
import { ToastContext } from "./toastContext";
import type { Toast, ToastApi, ToastInput } from "./toastContext";
import "./Toasts.css";

const DEFAULT_DURATION_MS = 6_000;
const MAX_VISIBLE = 4;

/**
 * Renders notifications in one stack, bottom-right (bottom on a phone). Used
 * for the action engine's proactive suggestions and for the outcome of an
 * action taken from elsewhere on the page.
 */
function ToastProvider({ children }: { children: ReactNode }) {
  const [toasts, setToasts] = useState<Toast[]>([]);
  const nextId = useRef(1);
  const timers = useRef(new Map<number, number>());

  const dismiss = useCallback((id: number) => {
    window.clearTimeout(timers.current.get(id));
    timers.current.delete(id);
    setToasts((current) => current.filter((toast) => toast.id !== id));
  }, []);

  const show = useCallback(
    (input: ToastInput) => {
      const id = nextId.current++;
      setToasts((current) => [...current, { ...input, id }].slice(-MAX_VISIBLE));

      const duration = input.durationMs ?? DEFAULT_DURATION_MS;
      if (duration > 0) {
        timers.current.set(id, window.setTimeout(() => dismiss(id), duration));
      }
      return id;
    },
    [dismiss]
  );

  const api = useMemo<ToastApi>(() => ({ show, dismiss }), [show, dismiss]);

  return (
    <ToastContext.Provider value={api}>
      {children}
      <div className="toast-stack" role="region" aria-label="Notifications" aria-live="polite">
        {toasts.map((toast) => (
          <div key={toast.id} className={`toast toast-${toast.tone ?? "info"}`} role="status">
            <div className="toast-body">
              <strong className="toast-title">{toast.title}</strong>
              {toast.message && <p className="toast-message">{toast.message}</p>}
              {toast.actions && toast.actions.length > 0 && (
                <div className="toast-actions">
                  {toast.actions.slice(0, 2).map((action) => (
                    <button
                      key={action.label}
                      type="button"
                      className="toast-action"
                      onClick={() => {
                        action.onClick();
                        dismiss(toast.id);
                      }}
                    >
                      {action.label}
                    </button>
                  ))}
                </div>
              )}
            </div>
            <button
              type="button"
              className="toast-close"
              aria-label="Dismiss notification"
              onClick={() => dismiss(toast.id)}
            >
              ✕
            </button>
          </div>
        ))}
      </div>
    </ToastContext.Provider>
  );
}

export default ToastProvider;
