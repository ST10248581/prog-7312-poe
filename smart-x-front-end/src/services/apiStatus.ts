/**
 * Shared connection state for the Smart-X API.
 *
 * Every request reports its outcome here (see http.ts): a response of any kind
 * means the API is up, a network failure or timeout means it is not. The
 * status banner and the pages subscribe through useSyncExternalStore, so one
 * failed call anywhere is enough for the whole app to show "API offline".
 */

export type ApiConnection = "checking" | "online" | "offline";

type Listener = () => void;

let connection: ApiConnection = "checking";
const listeners = new Set<Listener>();
const reconnectListeners = new Set<Listener>();

export function getApiConnection(): ApiConnection {
  return connection;
}

export function subscribeApiConnection(listener: Listener): () => void {
  listeners.add(listener);
  return () => listeners.delete(listener);
}

/** Runs `listener` each time the API comes back after being offline. */
export function onApiReconnected(listener: Listener): () => void {
  reconnectListeners.add(listener);
  return () => reconnectListeners.delete(listener);
}

export function reportApiConnection(next: Exclude<ApiConnection, "checking">): void {
  if (next === connection) {
    return;
  }

  const wasOffline = connection === "offline";
  connection = next;
  listeners.forEach((listener) => listener());

  if (wasOffline && next === "online") {
    reconnectListeners.forEach((listener) => listener());
  }
}
