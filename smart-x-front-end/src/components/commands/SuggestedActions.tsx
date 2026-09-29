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
  /** Put the suggested command into the override console for the operator to confirm. */
  onPrepare: (suggestion: SuggestedAction) => void;
  onSearch: (term: string) => void;
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

/**
 * Suggested actions and automated insights, from the API's action engine.
 *
 * The panel surfaces the next move before the operator goes looking for it:
 * the command a condition usually prompts, the step that usually follows the
 * last one, and the nodes that look faulty. Nothing here dispatches on its
 * own. A command suggestion only fills in the override console, where it goes
 * through the same confirmation as a typed one.
 */
function SuggestedActions({ insights, onPrepare, onSearch, onInspect }: SuggestedActionsProps) {
  // Dismissals are keyed by the API's stable id, so a dismissed card stays
  // gone across polls but a genuinely new suggestion still appears.
  const [dismissed, setDismissed] = useState<Set<string>>(() => new Set());

  const suggestions = (insights?.suggestions ?? []).filter(
    (suggestion) => !dismissed.has(suggestion.id),
  );

  const dismiss = (id: string) =>
    setDismissed((current) => new Set(current).add(id));

  return (
    <section className="suggested-actions" aria-label="Suggested actions">
      <header className="panel-head">
        <h2>Suggested actions</h2>
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
          {suggestions.map((suggestion) => (
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
                  {Math.round(suggestion.confidence * 100)}%
                </span>
              </div>

              <p className="suggestion-reason">{suggestion.reason}</p>

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
                {suggestion.commandType && suggestion.nodeId && (
                  <button
                    type="button"
                    className="suggestion-btn suggestion-btn-primary"
                    onClick={() => onPrepare(suggestion)}
                    title="Fill in the override console. You still confirm before it is sent."
                  >
                    Prepare {humanise(suggestion.commandType)}
                    {suggestion.dryRun ? " (dry run)" : ""}
                  </button>
                )}

                {suggestion.searchTerm && (
                  <button
                    type="button"
                    className="suggestion-btn suggestion-btn-primary"
                    onClick={() => onSearch(suggestion.searchTerm!)}
                  >
                    Search “{suggestion.searchTerm}”
                  </button>
                )}

                {suggestion.nodeId && (
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
                  onClick={() => dismiss(suggestion.id)}
                  aria-label={`Dismiss ${suggestion.title}`}
                >
                  Dismiss
                </button>
              </div>
            </li>
          ))}
        </ul>
      )}

      <footer className="suggested-actions-foot">
        Learned from {insights?.observedActions ?? 0} operator actions (searches,
        node selections and overrides). Every action you take here refines it.
      </footer>
    </section>
  );
}

export default SuggestedActions;
