import { useEffect, useRef, useState } from "react";
import { NavLink } from "react-router-dom";
import "./Navbar.css";

const navItems = [
  {
    label: "Sensor Data Ingestion and Telemetry",
    path: "/telemetry",
    disabled: false,
  },
  {
    label: "Real-Time Command Stream and History",
    path: "/commands",
    disabled: false,
  },
  {
    label: "Network Topology and Mesh Routing",
    path: "/topology",
    disabled: true,
  },
];

function Navbar() {
  // Below the navigation breakpoint the links live in a drop-down menu.
  const [menuOpen, setMenuOpen] = useState(false);
  const navRef = useRef<HTMLElement>(null);

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

  return (
    <nav ref={navRef} className={`navbar${menuOpen ? " menu-open" : ""}`} aria-label="Main">
      <NavLink to="/" className="navbar-brand" onClick={closeMenu}>
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
              aria-disabled={item.disabled || undefined}
              onClick={closeMenu}
              className={({ isActive }) =>
                `nav-link${isActive ? " active" : ""}${item.disabled ? " disabled" : ""}`
              }
            >
              <span
                className={`nav-status-dot${item.disabled ? " offline" : ""}`}
                aria-hidden="true"
              />
              {item.label}
              {item.disabled && <span className="nav-link-lock" aria-label="Coming soon">🔒</span>}
            </NavLink>
          </li>
        ))}
      </ul>
    </nav>
  );
}

export default Navbar;
