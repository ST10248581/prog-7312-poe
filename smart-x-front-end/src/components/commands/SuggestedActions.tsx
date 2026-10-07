import { useState } from "react";
import { humanise } from "../../utils/format";
import type {
  InsightsResponse,
  SuggestedAction,
  SuggestionKind,
} from "../../services/apiService";

interface SuggestedActionsProps {
  /** Straight from `/api/commands/insights`; null until it arrives. */
  insights: InsightsResponse | null;
  /** Suggestion ids dismissed this session; hidden here, and down-ranked by the API. */
  dismissed: string[];
  /** One click: send the command, run the search, apply the filter or inspect the node. */
  onApply: (suggestion: SuggestedAction) => Promise<void>;
  /** Put the suggested command into the override console to adjust before sending. */
  onPrepare: (suggestion: SuggestedAction) => void;
  onDismiss: (suggestion: SuggestedAction) => void;
  onInspect: (nodeId: string) => void;
}

const KIND_LABELS: Record<SuggestionKind, string> = {
  PredictedAction: "Predicted",
  NextStep: "Next step",
  ProblemDevice: "Problem device",
};

const KIND_HINTS: Record<SuggestionKind, string> = {
  PredictedAction: "Learned from what operators did after a condition like the one active now",
  NextStep: "Learned from what usually follows your last action",
  ProblemDevice: "Flagged by anomaly scoring of the node's recent readings and commands",
};

/** The label of the one-click button, which says exactly what it will do. */
function applyLabel(suggestion: SuggestedAction): string | null {
  if (suggestion.commandType && suggestion.nodeId) {
    return `Apply: send ${humanise(suggestion.commandType)}${suggestion.dryRun ? " (dry run)" : ""}`;
  }
  if (suggestion.searchTerm) {
    return `Apply: search “${suggestion.searchTerm}”`;
  }
  if (suggestion.filterFacet && suggestion.filterValue) {
    return `Apply filter: ${humanise(suggestion.filterValue)}`;
  }
  if (suggestion.nodeId) {
    return `Apply: inspect ${suggestion.nodeId}`;
  }
  return null;
}

/**
 * Suggested actions and automated insights, from the API's action engine.
 *
 * Each card says what the engine suggests, why ("Because …", from the rule or
 * score behind it), and how sure it is. Apply does it in one click — a sent
 * command lands on the undo stack, so it can be reversed straight away — and
 * tells the engine the suggestion was taken. Dismiss tells it the opposite, and
 * the API ranks that suggestion lower from then on.
 */
function SuggestedActions({ insights, dismissed, onApply, onPrepare, onDismiss, onInspect }: SuggestedActionsProps) {
  const [applying, setApplying] = useState<string | null>(null);

  const hidden = new Set(dismissed);
  const suggestions = (insights?.suggestions ?? []).filter((suggestion) => !hidden.has(suggestion.id));

  const apply = async (suggestion: SuggestedAction) => {
    setApplying(suggestion.id);
    try {
      await onApply(suggestion);
    } finally {
      setApplying(null);
    }
  };

  return (
    <section className="suggested-actions" aria-label="Suggested actions and automated insights">
      <header className="panel-head">
        <h2>Suggested Actions &amp; Automated Insights</h2>
        <span className="panel-head-count">
          {insights
            ? `${insights.activeTriggers} live conditions · ${insights.learnedAssociations} learned patterns`
            : "loading…"}
        </span>
      </header>

      {suggestions.length === 0 ? (
        <p className="panel-empty">
          {insights
            ? "Nothing to suggest right now. Suggestions appear as conditions arise that match learned habits, or as a node starts to look faulty."
            : "Loading automated insights…"}
        </p>
      ) : (
        <ul className="suggestion-list">
          {suggestions.map((suggestion) => {
            const label = applyLabel(suggestion);
            const confidence = Math.round(suggestion.confidence * 100);

            return (
              <li
                key={suggestion.id}
                className={`suggestion suggestion-${suggestion.kind.toLowerCase()}`}
              >
                <div className="suggestion-head">
                  <span className="suggestion-kind" title={KIND_HINTS[suggestion.kind]}>
                    {KIND_LABELS[suggestion.kind]}
                  </span>
                  <strong className="suggestion-title">{suggestion.title}</strong>
                  <span
                    className="suggestion-confidence"
                    title={`Based on ${suggestion.support} observation${suggestion.support === 1 ? "" : "s"}`}
                  >
                    {confidence}%
                  </span>
                </div>

                <div className="suggestion-confidence-bar" aria-hidden="true">
                  <div style={{ width: `${confidence}%` }} />
                </div>

                <p className="suggestion-reason">
                  <span className="suggestion-because">Because</span>{" "}
                  {suggestion.reason.charAt(0).toLowerCase() + suggestion.reason.slice(1)}
                </p>

                {suggestion.signals.length > 0 && (
                  <div className="suggestion-signals">
                    {suggestion.signals.map((signal) => (
                      <span key={signal} className="suggestion-signal">
                        {signal}
                      </span>
                    ))}
                  </div>
                )}

                <div className="suggestion-actions">
                  {label && (
                    <button
                      type="button"
                      className="suggestion-btn suggestion-btn-primary"
                      onClick={() => apply(suggestion)}
                      disabled={applying !== null}
                      title={
                        suggestion.commandType
                          ? "Sends it now. It goes on the undo stack, so it can be reversed."
                          : undefined
                      }
                    >
                      {applying === suggestion.id ? "Applying…" : label}
                    </button>
                  )}

                  {suggestion.commandType && suggestion.nodeId && (
                    <button
                      type="button"
                      className="suggestion-btn"
                      onClick={() => onPrepare(suggestion)}
                      title="Fill in the override console to adjust it before sending"
                    >
                      Edit first
                    </button>
                  )}

                  {suggestion.nodeId && !label?.startsWith("Apply: inspect") && (
                    <button
                      type="button"
                      className="suggestion-btn"
                      onClick={() => onInspect(suggestion.nodeId!)}
                    >
                      Inspect {suggestion.nodeId}
                    </button>
                  )}

                  <button
                    type="button"
                    className="suggestion-btn suggestion-btn-dismiss"
                    onClick={() => onDismiss(suggestion)}
                    aria-label={`Dismiss ${suggestion.title}`}
                    title="Hide it and rank it lower from now on"
                  >
                    Dismiss
                  </button>
                </div>
              </li>
            );
          })}
        </ul>
      )}

      <footer className="suggested-actions-foot">
        Learned from {insights?.observedActions ?? 0} operator actions (searches, filters,
        node selections, overrides and undos). Every action you take here refines it.
      </footer>
    </section>
  );
}

export default SuggestedActions;
