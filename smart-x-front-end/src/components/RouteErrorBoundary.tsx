import { Component } from "react";
import type { ErrorInfo, ReactNode } from "react";
import { Link } from "react-router-dom";
import "./StatusPage.css";

interface RouteErrorBoundaryProps {
  /** Shown in the message, e.g. "Command Stream". */
  pageName: string;
  children: ReactNode;
}

interface RouteErrorBoundaryState {
  error: Error | null;
}

/**
 * Contains a crash to the page it happened on. Without it, one bad render
 * unmounts the whole app; with it the navbar, the other pages and their kept
 * state are untouched, and this page can be retried in place.
 *
 * App.tsx keys each boundary by route, so leaving a crashed page and coming
 * back starts it clean.
 */
class RouteErrorBoundary extends Component<RouteErrorBoundaryProps, RouteErrorBoundaryState> {
  state: RouteErrorBoundaryState = { error: null };

  static getDerivedStateFromError(error: Error): RouteErrorBoundaryState {
    return { error };
  }

  componentDidCatch(error: Error, info: ErrorInfo) {
    console.error(`${this.props.pageName} crashed`, error, info.componentStack);
  }

  render() {
    const { error } = this.state;
    if (!error) {
      return this.props.children;
    }

    return (
      <div className="status-page" role="alert">
        <div className="status-page-icon" aria-hidden="true">⚠</div>
        <h1>{this.props.pageName} hit a problem</h1>
        <p>
          This page stopped rendering. The rest of the dashboard is unaffected, and your
          filters on other pages are kept.
        </p>
        <code className="status-page-detail">{error.message}</code>
        <div className="status-page-actions">
          <button type="button" className="status-page-btn primary" onClick={() => this.setState({ error: null })}>
            Try again
          </button>
          <Link to="/" className="status-page-btn">
            Back to overview
          </Link>
        </div>
      </div>
    );
  }
}

export default RouteErrorBoundary;
