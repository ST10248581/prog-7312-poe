import { useState } from "react";
import type { ReactNode } from "react";
import { AppStateContext } from "./appState";
import type { AppStateStore } from "./appState";

/** Holds page state above the router so it survives navigation. See appState.ts. */
function AppStateProvider({ children }: { children: ReactNode }) {
  const [store] = useState<AppStateStore>(() => new Map());
  return <AppStateContext.Provider value={store}>{children}</AppStateContext.Provider>;
}

export default AppStateProvider;
