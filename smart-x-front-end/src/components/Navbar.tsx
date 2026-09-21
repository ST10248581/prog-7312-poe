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
  return (
    <nav className="navbar">
      <NavLink to="/" className="navbar-brand">
        <span className="navbar-brand-icon">⬡</span>
        Smart<span className="navbar-brand-accent">-X</span>
        <span className="navbar-brand-tag">IoT</span>
      </NavLink>

      <ul className="navbar-nav">
        {navItems.map((item) => (
          <li key={item.path}>
            <NavLink
              to={item.path}
              className={({ isActive }) =>
                `nav-link${isActive ? " active" : ""}${item.disabled ? " disabled" : ""}`
              }
            >
              <span
                className={`nav-status-dot${item.disabled ? " offline" : ""}`}
              />
              {item.label}
              {item.disabled && <span className="nav-link-lock">🔒</span>}
            </NavLink>
          </li>
        ))}
      </ul>
    </nav>
  );
}

export default Navbar;
