import { useState } from "react";
import { formatRelative } from "../../utils/format";
import type { InsightsResponse } from "../../services/apiService";

interface LearningPanelProps {
  insights: InsightsResponse | null;
  /** Clears everything learned, to show the engine learning from nothing. */
  onReset: () => Promise<void>;
}

/**
 * What the action engine has learned, and how its suggestions are received.
 *
 * Rules are condition ⇒ action associations with their support (how often the
 * condition was seen with an operator present) and confidence (how often the
 * action followed). Transitions are the "what comes next" model over operator
 * sessions. Applied and dismissed counts are the feedback loop that re-ranks
 * suggestions, and the activity list is the query history feeding all of it.
 */
function LearningPanel({ insights, onReset }: LearningPanelProps) {
  const [confirming, setConfirming] = useState(false);
  const [resetting, setResetting] = useState(false);

  const learning = insights?.learning;
  const acceptance = learning?.acceptanceRate;

  const reset = async () => {
    setResetting(true);
    try {
      await onReset();
    } finally {
      setResetting(false);
      setConfirming(false);
    }
  };

  return (
    <section className="learning-panel" aria-label="What the engine has learned">
      <header className="panel-head">
        <h2>What the engine has learned</h2>
        <span className="panel-head-count">{insights ? `${insights.observedActions} actions observed` : "loading…"}</span>
      </header>

      <div className="learning-stats">
        <div className="learning-stat">
          <span className="learning-stat-value">{learning?.rulesLearned ?? "—"}</span>
          <span className="learning-stat-label">rules learned</span>
        </div>
        <div className="learning-stat">
          <span className="learning-stat-value">{learning?.transitionsLearned ?? "—"}</span>
          <span className="learning-stat-label">next-step links</span>
        </div>
        <div className="learning-stat">
          <span className="learning-stat-value">
            {learning ? `${learning.applied} / ${learning.dismissed}` : "—"}
          </span>
          <span className="learning-stat-label">applied / dismissed</span>
        </div>
        <div className="learning-stat">
          <span className="learning-stat-value">
            {acceptance === null || acceptance === undefined ? "—" : `${Math.round(acceptance * 100)}%`}
          </span>
          <span className="learning-stat-label">acceptance</span>
          <div className="learning-acceptance" aria-hidden="true">
            <div style={{ width: `${Math.round((acceptance ?? 0) * 100)}%` }} />
          </div>
        </div>
      </div>

      <h3 className="learning-subtitle">Strongest rules</h3>
      {learning && learning.topRules.length > 0 ? (
        <ol className="learning-rules">
          {learning.topRules.map((rule) => (
            <li key={`${rule.condition}|${rule.action}`} className="learning-rule">
              <span className="learning-rule-text">
                When <strong>{rule.condition}</strong>, operators <strong>{rule.action}</strong>
              </span>
              <span className="learning-rule-numbers" title={`${rule.count} of ${rule.support} times`}>
                {Math.round(rule.confidence * 100)}% · {rule.count}/{rule.support}
              </span>
            </li>
          ))}
        </ol>
      ) : (
        <p className="pipeline-empty">
          No rule is strong enough yet. Respond to the same kind of alert the same way a few
          times and one will appear here.
        </p>
      )}

      <h3 className="learning-subtitle">Your recent activity</h3>
      {learning && learning.recentActivity.length > 0 ? (
        <ul className="learning-activity">
          {learning.recentActivity.map((entry) => (
            <li key={`${entry.atUtc}|${entry.description}`}>
              <span>{entry.description}</span>
              <span className="learning-activity-age">{formatRelative(entry.atUtc)}</span>
            </li>
          ))}
        </ul>
      ) : (
        <p className="pipeline-empty">Searches, filters, inspections, overrides and undos you make are listed here.</p>
      )}

      <div className="learning-reset">
        {confirming ? (
          <>
            <span>Forget every rule, sequence and piece of feedback?</span>
            <button type="button" className="override-btn override-btn-undo" onClick={reset} disabled={resetting}>
              {resetting ? "Resetting…" : "Yes, reset"}
            </button>
            <button type="button" className="override-btn override-btn-reset" onClick={() => setConfirming(false)}>
              Cancel
            </button>
          </>
        ) : (
          <button
            type="button"
            className="override-btn override-btn-reset"
            onClick={() => setConfirming(true)}
            title="Start learning from an empty history, e.g. to demonstrate a habit being picked up"
          >
            Reset learning
          </button>
        )}
      </div>
    </section>
  );
}

export default LearningPanel;
