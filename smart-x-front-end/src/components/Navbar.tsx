import { useEffect, useRef, useState } from "react";
import { NavLink } from "react-router-dom";
import { getCommandSummary, getSummary } from "../services/apiService";
import { usePolling } from "../hooks/usePolling";
import { usePersistentState } from "../state/appState";
import "./Navbar.css";

/** The badge counts are a glance, not a feed: a slower poll than the pages'. */
const BADGE_REFRESH_MS = 15_000;

interface NavCounts {
  activeAlerts: number;
  criticalAlerts: number;
  inFlight: number;
}

const navItems = [
  { label: "Overview", path: "/", end: true },
  { label: "Sensor Data Ingestion and Telemetry", path: "/telemetry", end: false },
  { label: "Real-Time Command Stream and History", path: "/commands", end: false },
];

function Navbar() {
  // Below the navigation breakpoint the links live in a drop-down menu.
  const [menuOpen, setMenuOpen] = useState(false);
  const navRef = useRef<HTMLElement>(null);

  // Live counts on the module links, so the state of each module is visible
  // from anywhere in the app. Kept across navigation like page state.
  const [counts, setCounts] = usePersistentState<NavCounts | null>("nav.counts", null);

  usePolling(
    async (signal) => {
      const [telemetry, commands] = await Promise.allSettled([
        getSummary(signal),
        getCommandSummary({}, signal),
      ]);
      if (signal.aborted) return;

      setCounts((current) => ({
        activeAlerts: telemetry.status === "fulfilled" ? telemetry.value.activeAlertCount : current?.activeAlerts ?? 0,
        criticalAlerts: telemetry.status === "fulfilled" ? telemetry.value.criticalAlertCount : current?.criticalAlerts ?? 0,
        inFlight: commands.status === "fulfilled" ? commands.value.inFlightCount : current?.inFlight ?? 0,
      }));
    },
    { intervalMs: BADGE_REFRESH_MS }
  );

  // Following any link closes the menu.
  const closeMenu = () => setMenuOpen(false);

  // While open, Escape or a click outside the bar closes it.
  useEffect(() => {
    if (!menuOpen) {
      return;
    }

    const handleKey = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        setMenuOpen(false);
      }
    };

    const handlePointer = (event: PointerEvent) => {
      if (navRef.current && !navRef.current.contains(event.target as Node)) {
        setMenuOpen(false);
      }
    };

    document.addEventListener("keydown", handleKey);
    document.addEventListener("pointerdown", handlePointer);

    return () => {
      document.removeEventListener("keydown", handleKey);
      document.removeEventListener("pointerdown", handlePointer);
    };
  }, [menuOpen]);

  const badgeFor = (path: string) => {
    if (!counts) return null;

    if (path === "/telemetry" && counts.activeAlerts > 0) {
      return (
        <span
          className={`nav-badge${counts.criticalAlerts > 0 ? " critical" : ""}`}
          title={`${counts.activeAlerts} active alerts, ${counts.criticalAlerts} critical`}
        >
          {counts.activeAlerts > 99 ? "99+" : counts.activeAlerts}
          <span className="visually-hidden"> active alerts</span>
        </span>
      );
    }

    if (path === "/commands" && counts.inFlight > 0) {
      return (
        <span className="nav-badge info" title={`${counts.inFlight} commands awaiting acknowledgement`}>
          {counts.inFlight}
          <span className="visually-hidden"> commands in flight</span>
        </span>
      );
    }

    return null;
  };

  return (
    <nav ref={navRef} className={`navbar${menuOpen ? " menu-open" : ""}`} aria-label="Main">
      <NavLink to="/" className="navbar-brand" onClick={closeMenu} aria-label="Smart-X overview">
        <span className="navbar-brand-icon" aria-hidden="true">⬡</span>
        <span>Smart<span className="navbar-brand-accent">-X</span></span>
        <span className="navbar-brand-tag">IoT</span>
      </NavLink>

      <button
        type="button"
        className="navbar-toggle"
        aria-expanded={menuOpen}
        aria-controls="navbar-links"
        aria-label={menuOpen ? "Close navigation menu" : "Open navigation menu"}
        onClick={() => setMenuOpen((open) => !open)}
      >
        <span className="navbar-toggle-bar" />
        <span className="navbar-toggle-bar" />
        <span className="navbar-toggle-bar" />
      </button>

      <ul id="navbar-links" className="navbar-nav">
        {navItems.map((item) => (
          <li key={item.path}>
            <NavLink
              to={item.path}
              end={item.end}
              onClick={closeMenu}
              className={({ isActive }) => `nav-link${isActive ? " active" : ""}`}
            >
              <span className="nav-status-dot" aria-hidden="true" />
              {item.label}
              {badgeFor(item.path)}
            </NavLink>
          </li>
        ))}
      </ul>
    </nav>
  );
}

export default Navbar;
