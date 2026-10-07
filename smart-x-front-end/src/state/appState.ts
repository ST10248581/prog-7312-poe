import { createContext, useContext, useEffect, useState } from "react";
import type { Dispatch, SetStateAction } from "react";

/**
 * App-wide state that outlives a page.
 *
 * React Router unmounts a page when you navigate away, which throws away every
 * useState inside it — filters, the selected node, the page number, the last
 * data fetched. AppStateProvider holds one Map above the router instead, and
 * usePersistentState reads from and writes to it, so a page that remounts picks
 * up exactly where it left off and shows its last data at once while it
 * refreshes. Values marked `session` are also mirrored to sessionStorage, so
 * they survive a reload of the tab too.
 */
export type AppStateStore = Map<string, unknown>;

export const AppStateContext = createContext<AppStateStore | null>(null);

const SESSION_PREFIX = "smartx:";

interface PersistentStateOptions {
  /** Also keep the value in sessionStorage, so a page reload restores it. Must be JSON-serialisable. */
  session?: boolean;
}

function readSession<T>(key: string): T | undefined {
  try {
    const raw = window.sessionStorage.getItem(SESSION_PREFIX + key);
    return raw === null ? undefined : (JSON.parse(raw) as T);
  } catch {
    return undefined;
  }
}

/**
 * useState whose value survives the component unmounting. `key` must be unique
 * app-wide; by convention it is "page.field", e.g. "commands.filters".
 */
export function usePersistentState<T>(
  key: string,
  initial: T | (() => T),
  options: PersistentStateOptions = {}
): [T, Dispatch<SetStateAction<T>>] {
  const store = useContext(AppStateContext);
  const { session = false } = options;

  const [value, setValue] = useState<T>(() => {
    if (store?.has(key)) {
      return store.get(key) as T;
    }
    if (session) {
      const restored = readSession<T>(key);
      if (restored !== undefined) {
        return restored;
      }
    }
    return typeof initial === "function" ? (initial as () => T)() : initial;
  });

  useEffect(() => {
    store?.set(key, value);
    if (session) {
      try {
        window.sessionStorage.setItem(SESSION_PREFIX + key, JSON.stringify(value));
      } catch {
        // Storage full or blocked (private mode): the in-memory copy still works.
      }
    }
  }, [store, key, value, session]);

  return [value, setValue];
}

/** Reads a value another page stored, without owning it. Undefined when that page has not run yet. */
export function usePeekState<T>(key: string, options: PersistentStateOptions = {}): T | undefined {
  const store = useContext(AppStateContext);
  if (store?.has(key)) {
    return store.get(key) as T;
  }
  return options.session ? readSession<T>(key) : undefined;
}
