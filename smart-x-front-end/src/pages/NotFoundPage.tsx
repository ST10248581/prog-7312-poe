import { Link, useLocation } from "react-router-dom";
import "../components/StatusPage.css";

/** Any route the app does not define. */
function NotFoundPage() {
  const location = useLocation();

  return (
    <div className="status-page">
      <div className="status-page-code" aria-hidden="true">404</div>
      <h1>No page at this address</h1>
      <p>
        <code>{location.pathname}</code> is not part of the Smart-X dashboard.
      </p>
      <div className="status-page-actions">
        <Link to="/" className="status-page-btn primary">
          Overview
        </Link>
        <Link to="/telemetry" className="status-page-btn">
          Telemetry
        </Link>
        <Link to="/commands" className="status-page-btn">
          Command stream
        </Link>
      </div>
    </div>
  );
}

export default NotFoundPage;
