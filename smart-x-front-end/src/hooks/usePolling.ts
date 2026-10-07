import { useEffect, useRef } from "react";

interface PollingOptions {
  /** Delay between the end of one run and the start of the next. */
  intervalMs: number;
  /** When false, the task still runs once whenever `key` changes, but does not repeat. */
  live?: boolean;
  /** Changing this restarts the cycle at once, cancelling the run in flight (e.g. new filters). */
  key?: string;
}

/**
 * Runs `task` now and then every `intervalMs`, without ever letting the UI
 * fall behind:
 *
 * - **No overlap.** The next run is scheduled only after the current one
 *   settles (a setTimeout chain, not setInterval), so a slow response can never
 *   stack a second request on top of it.
 * - **Cancellation.** Each run gets an AbortSignal. It is aborted when the key
 *   changes, polling stops, or the component unmounts, so a stale response is
 *   never applied and the request itself is cancelled.
 * - **Hidden tabs rest.** While the tab is hidden nothing is scheduled; when it
 *   becomes visible again the task runs immediately.
 *
 * The task can change on every render; the latest one is always used.
 */
export function usePolling(task: (signal: AbortSignal) => Promise<unknown>, options: PollingOptions) {
  const { intervalMs, live = true, key = "" } = options;
  const taskRef = useRef(task);

  useEffect(() => {
    taskRef.current = task;
  });

  useEffect(() => {
    let stopped = false;
    let timer: number | undefined;
    let controller: AbortController | null = null;

    const run = async () => {
      if (stopped) {
        return;
      }

      controller = new AbortController();
      try {
        await taskRef.current(controller.signal);
      } catch {
        // Tasks report their own errors; a rejection must not end the cycle.
      }
      controller = null;

      if (!stopped && live && document.visibilityState === "visible") {
        timer = window.setTimeout(run, intervalMs);
      }
    };

    const onVisibilityChange = () => {
      if (!live || stopped) {
        return;
      }

      if (document.visibilityState === "visible") {
        // Back in view: refresh now rather than wait out the interval.
        if (!controller) {
          window.clearTimeout(timer);
          void run();
        }
      } else {
        window.clearTimeout(timer);
      }
    };

    void run();
    document.addEventListener("visibilitychange", onVisibilityChange);

    return () => {
      stopped = true;
      window.clearTimeout(timer);
      controller?.abort();
      document.removeEventListener("visibilitychange", onVisibilityChange);
    };
  }, [intervalMs, live, key]);
}

/** True for the rejection fetch produces when its signal is aborted. */
export function isAbortError(error: unknown): boolean {
  return error instanceof DOMException && error.name === "AbortError";
}
