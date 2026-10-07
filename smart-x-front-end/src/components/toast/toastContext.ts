import { createContext, useContext } from "react";

export type ToastTone = "info" | "success" | "warning" | "error" | "insight";

export interface ToastAction {
  label: string;
  onClick: () => void;
}

export interface ToastInput {
  tone?: ToastTone;
  title: string;
  message?: string;
  /** Up to two buttons, e.g. "Apply" and "View". Clicking one closes the toast. */
  actions?: ToastAction[];
  /** Milliseconds before it closes itself; 0 keeps it until dismissed. Default 6000. */
  durationMs?: number;
}

export interface Toast extends ToastInput {
  id: number;
}

export interface ToastApi {
  show: (toast: ToastInput) => number;
  dismiss: (id: number) => void;
}

export const ToastContext = createContext<ToastApi | null>(null);

/** Shows short-lived notifications. Falls back to a no-op outside the provider. */
export function useToast(): ToastApi {
  return useContext(ToastContext) ?? { show: () => -1, dismiss: () => undefined };
}
