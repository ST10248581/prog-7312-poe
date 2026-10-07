import type { ReactNode } from "react";
import { BrowserRouter, Routes, Route, useLocation } from "react-router-dom";
import Navbar from "./components/Navbar";
import ApiStatusBanner from "./components/ApiStatusBanner";
import RouteErrorBoundary from "./components/RouteErrorBoundary";
import ToastProvider from "./components/toast/ToastProvider";
import AppStateProvider from "./state/AppStateProvider";
import HomePage from "./pages/HomePage";
import TelemetryPage from "./pages/TelemetryPage";
import CommandsPage from "./pages/CommandsPage";
import NotFoundPage from "./pages/NotFoundPage";
import "./App.css";

/**
 * Wraps a page in its own error boundary. Keyed by path, so a crashed page
 * starts clean when the user comes back to it, and a crash never reaches the
 * navbar or the other pages.
 */
function Guarded({ name, children }: { name: string; children: ReactNode }) {
  const { pathname } = useLocation();
  return (
    <RouteErrorBoundary key={pathname} pageName={name}>
      {children}
    </RouteErrorBoundary>
  );
}

/**
 * The providers sit above the router: AppStateProvider keeps each page's
 * filters, selection and last data while the page itself is unmounted, so
 * moving between modules never loses where you were.
 */
function App() {
  return (
    <AppStateProvider>
      <ToastProvider>
        <BrowserRouter>
          <Navbar />
          <ApiStatusBanner />
          <main className="app-content">
            <Routes>
              <Route path="/" element={<Guarded name="Overview"><HomePage /></Guarded>} />
              <Route path="/telemetry" element={<Guarded name="Telemetry"><TelemetryPage /></Guarded>} />
              <Route path="/commands" element={<Guarded name="Command Stream"><CommandsPage /></Guarded>} />
              <Route path="*" element={<NotFoundPage />} />
            </Routes>
          </main>
        </BrowserRouter>
      </ToastProvider>
    </AppStateProvider>
  );
}

export default App;
