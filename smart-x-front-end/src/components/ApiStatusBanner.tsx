import { useCallback, useEffect, useRef, useState, useSyncExternalStore } from "react";
import { testConnection } from "../services/apiService";
import {
  getApiConnection,
  onApiReconnected,
  subscribeApiConnection,
} from "../services/apiStatus";
import { API_BASE_URL } from "../services/http";
import "./ApiStatusBanner.css";

/** Waits between automatic reconnection attempts: 3 s, doubling to 30 s. */
const FIRST_RETRY_MS = 3_000;
const MAX_RETRY_MS = 30_000;

/** Startup feels instant if the API answers quickly, so "Connecting…" only shows after this. */
const CONNECTING_GRACE_MS = 800;

/**
 * The startup gateway. Checks the API once on load, then follows the shared
 * connection state that every request reports. When the API is down it
 * explains how to start it, retries on a back-off with a visible countdown,
 * and offers an immediate retry; when the API returns it says so briefly and
 * the pages reload their data (see onApiReconnected).
 */
function ApiStatusBanner() {
  const connection = useSyncExternalStore(subscribeApiConnection, getApiConnection);

  const [checking, setChecking] = useState(false);
  const [showConnecting, setShowConnecting] = useState(false);
  const [secondsUntilRetry, setSecondsUntilRetry] = useState<number | null>(null);
  const [justReconnected, setJustReconnected] = useState(false);
  const retryDelayRef = useRef(FIRST_RETRY_MS);

  const check = useCallback(async () => {
    setChecking(true);
    try {
      await testConnection();
    } catch {
      // The failure has already been reported to the connection store.
    } finally {
      setChecking(false);
    }
  }, []);

  // Initial check, with a grace period before "Connecting…" appears.
  useEffect(() => {
    const grace = window.setTimeout(() => setShowConnecting(true), CONNECTING_GRACE_MS);
    // Probing the API is the external-system case this rule carves out; the
    // state it sets tracks that request.
    // oxlint-disable-next-line react/set-state-in-effect
    void check();
    return () => window.clearTimeout(grace);
  }, [check]);

  // While offline, count down to the next automatic attempt.
  useEffect(() => {
    if (connection !== "offline" || checking) {
      return;
    }

    const delay = retryDelayRef.current;
    const deadline = Date.now() + delay;
    retryDelayRef.current = Math.min(delay * 2, MAX_RETRY_MS);

    const tick = () => setSecondsUntilRetry(Math.max(0, Math.ceil((deadline - Date.now()) / 1000)));
    const countdown = window.setInterval(tick, 250);
    const retry = window.setTimeout(() => void check(), delay);
    tick();

    return () => {
      window.clearInterval(countdown);
      window.clearTimeout(retry);
    };
  }, [connection, checking, check]);

  // Celebrate the reconnect briefly, then get out of the way.
  useEffect(
    () =>
      onApiReconnected(() => {
        retryDelayRef.current = FIRST_RETRY_MS;
        setJustReconnected(true);
        window.setTimeout(() => setJustReconnected(false), 3_000);
      }),
    []
  );

  if (connection === "online") {
    return justReconnected ? (
      <div className="api-banner api-banner-online" role="status">
        <span className="api-banner-dot" aria-hidden="true" />
        <span className="api-banner-text">
          <strong>Reconnected.</strong> Live data is flowing again.
        </span>
      </div>
    ) : null;
  }

  if (connection === "checking") {
    return showConnecting ? (
      <div className="api-banner api-banner-checking" role="status">
        <span className="api-banner-spinner" aria-hidden="true" />
        <span className="api-banner-text">Connecting to the Smart-X API…</span>
      </div>
    ) : null;
  }

  return (
    <div className="api-banner api-banner-offline" role="alert">
      <span className="api-banner-dot" aria-hidden="true" />
      <div className="api-banner-text">
        <strong>API offline.</strong> The dashboard cannot reach <code>{API_BASE_URL}</code>. Start the
        backend with <code>dotnet run</code> in <code>smart-x-backend/SmartX.Api</code>.
        <span className="api-banner-countdown">
          {checking
            ? " Checking…"
            : secondsUntilRetry !== null
              ? ` Retrying in ${secondsUntilRetry}s.`
              : ""}
        </span>
      </div>
      <button type="button" className="api-banner-retry" onClick={() => void check()} disabled={checking}>
        {checking ? "Checking…" : "Retry now"}
      </button>
    </div>
  );
}

export default ApiStatusBanner;
