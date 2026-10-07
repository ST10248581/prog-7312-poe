import { useState } from "react";
import { formatRelative, humanise } from "../../utils/format";
import { COMMAND_PRIORITIES, COMMAND_TYPES, PRIORITY_HINTS } from "./types";
import type { CommandPriority, CommandRecord, CommandType } from "./types";
import type {
  CommandFilterOptions,
  DeviceCommand,
  DispatchCommandRequest,
  OverrideHistory,
  RedoResult,
  UndoResult,
} from "../../services/apiService";

/**
 * A command handed to the console from outside, e.g. a suggested action.
 * `key` changes per hand-off, so preparing the same suggestion twice still
 * refills the form.
 */
export interface OverrideDraft {
  key: string;
  commandType: CommandType;
  parameters: string;
  priority: CommandPriority;
  dryRun: boolean;
  /** Why the draft is there, shown above the form. */
  source: string;
}

interface OverrideConsoleProps {
  /** Straight from `/api/commands/filter-options`; null until it arrives. */
  options: CommandFilterOptions | null;
  /** Node id taken from the stream selection; the operator can still change it. */
  targetNode: string;
  pending: CommandRecord[];
  /** The API's undo and redo stacks, top first. */
  history: OverrideHistory;
  draft: OverrideDraft | null;
  onTargetChange: (nodeId: string) => void;
  /** Resolves with the queued command, or rejects with the API's reason. */
  onDispatch: (request: DispatchCommandRequest) => Promise<DeviceCommand>;
  /**
   * Pops the top of the undo stack. The id of the entry the operator is
   * looking at is sent with it, so a double click cannot undo two overrides.
   */
  onUndo: (expectedCommandId: string) => Promise<UndoResult>;
  /** Re-applies the top of the redo stack, idempotent in the same way. */
  onRedo: (expectedCommandId: string) => Promise<RedoResult>;
}

/** How many undo entries are listed under the one that will be undone. */
const HISTORY_PREVIEW = 4;

/** Hint text per command so the parameter field is never a blank guess. */
const PARAMETER_HINTS: Record<CommandType, string> = {
  SetThreshold: "temp.max=28.5",
  Recalibrate: "offset=auto",
  ToggleActuator: "relay=1,state=on",
  RestartNode: "mode=soft",
  FirmwarePush: "v2.4.1",
  RequestSample: "count=5",
};

/**
 * Manual override, deliberately a two-step action. The operator composes the
 * command, then confirms it against a written summary of what will be sent —
 * a dispatch to live hardware should not be one stray click.
 *
 * Submitting POSTs to `/api/commands`. The API validates the target and either
 * queues the command, where the dispatch simulator picks it up and it appears
 * in the stream alongside automated traffic, or rejects it with a reason that
 * is shown here rather than swallowed.
 */
function OverrideConsole({
  options,
  targetNode,
  pending,
  history,
  draft,
  onTargetChange,
  onDispatch,
  onUndo,
  onRedo,
}: OverrideConsoleProps) {
  const [commandType, setCommandType] = useState<CommandType>("SetThreshold");
  const [parameters, setParameters] = useState("");
  const [priority, setPriority] = useState<CommandPriority>("Normal");
  const [dryRun, setDryRun] = useState(true);
  const [confirmed, setConfirmed] = useState(false);

  const [sending, setSending] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [sent, setSent] = useState<DeviceCommand | null>(null);

  // One flag for both directions: while either request is out, neither button
  // can fire again.
  const [stackBusy, setStackBusy] = useState<"undo" | "redo" | null>(null);
  const [stackResult, setStackResult] = useState<{ ok: boolean; title: string; message: string } | null>(null);

  // A new draft fills the form once. Adjusted during render rather than in an
  // effect, so the form never paints a frame with the old values. The
  // confirmation tick is cleared: a suggestion still has to be checked.
  const [appliedDraft, setAppliedDraft] = useState<string | null>(null);
  if (draft && draft.key !== appliedDraft) {
    setAppliedDraft(draft.key);
    setCommandType(draft.commandType);
    setParameters(draft.parameters);
    setPriority(draft.priority);
    setDryRun(draft.dryRun);
    setConfirmed(false);
    setError(null);
    setSent(null);
  }

  const nodes = options?.nodes ?? [];
  const priorities = options?.priorities ?? COMMAND_PRIORITIES;

  // Only the commands the target's hardware accepts, so the API's capability
  // check is something the operator never has to run into.
  const capable = targetNode ? options?.nodeCapabilities?.[targetNode] : undefined;
  const commandTypes = capable ?? options?.commandTypes ?? COMMAND_TYPES;
  if (capable && capable.length > 0 && !capable.includes(commandType)) {
    setCommandType(capable[0]);
  }

  const [top, ...older] = history.undo;
  const redoTop = history.redo[0];

  const handleUndo = async () => {
    if (!top || stackBusy) return;
    setStackBusy("undo");
    setStackResult(null);

    try {
      const result = await onUndo(top.commandId);
      setStackResult({
        ok: result.outcome !== "Irreversible",
        title: result.outcome === "AlreadyUndone" ? "Already undone" : result.outcome,
        message: result.message,
      });
    } catch (err) {
      setStackResult({ ok: false, title: "Not undone", message: err instanceof Error ? err.message : "Undo failed." });
    } finally {
      setStackBusy(null);
    }
  };

  const handleRedo = async () => {
    if (!redoTop || stackBusy) return;
    setStackBusy("redo");
    setStackResult(null);

    try {
      const result = await onRedo(redoTop.commandId);
      setStackResult({
        ok: true,
        title: result.outcome === "AlreadyRedone" ? "Already redone" : "Redone",
        message: result.message,
      });
    } catch (err) {
      setStackResult({ ok: false, title: "Not redone", message: err instanceof Error ? err.message : "Redo failed." });
    } finally {
      setStackBusy(null);
    }
  };

  const ready = targetNode !== "" && parameters.trim() !== "" && confirmed && !sending;

  // Confirmation is per dispatch: the tick clears after each send so the next
  // command has to be checked on its own terms.
  const resetConfirmation = () => {
    setParameters("");
    setConfirmed(false);
  };

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();

    if (!ready) {
      return;
    }

    setSending(true);
    setError(null);
    setSent(null);

    try {
      const command = await onDispatch({
        nodeId: targetNode,
        commandType,
        parameters: parameters.trim(),
        priority,
        dryRun,
      });

      setSent(command);
      resetConfirmation();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Dispatch failed.");
      setConfirmed(false);
    } finally {
      setSending(false);
    }
  };

  return (
    <section className="override-console" aria-label="Manual override">
      <header className="panel-head">
        <h2>Manual override</h2>
        <span className="panel-head-count">{pending.length} pending</span>
      </header>

      <p className="override-intro">
        Issues a command straight to a node, bypassing the automation rules.
        Overrides are logged against your operator id and appear in the stream
        alongside automated traffic.
      </p>

      {draft && draft.key === appliedDraft && (
        <p className="override-draft" role="status">
          <strong>Prepared from a suggestion.</strong> {draft.source} Check it and confirm
          below before it is sent.
        </p>
      )}

      <form className="override-form" onSubmit={handleSubmit}>
        <label className="override-field">
          <span className="override-label">Target node</span>
          <select
            className="override-input"
            value={targetNode}
            onChange={(event) => onTargetChange(event.target.value)}
          >
            <option value="">
              {nodes.length === 0 ? "No reachable nodes" : "Select a node…"}
            </option>
            {nodes.map((node) => (
              <option key={node} value={node}>
                {node}
              </option>
            ))}
          </select>
          <span className="override-hint">
            Only nodes the mesh can currently reach are listed.
          </span>
        </label>

        <label className="override-field">
          <span className="override-label">Command</span>
          <select
            className="override-input"
            value={commandType}
            onChange={(event) => setCommandType(event.target.value as CommandType)}
          >
            {commandTypes.map((type) => (
              <option key={type} value={type}>
                {humanise(type)}
              </option>
            ))}
          </select>
        </label>

        <label className="override-field">
          <span className="override-label">Parameters</span>
          <input
            type="text"
            className="override-input"
            placeholder={PARAMETER_HINTS[commandType]}
            value={parameters}
            onChange={(event) => setParameters(event.target.value)}
          />
          <span className="override-hint">
            Validated server-side against the node's capability profile.
          </span>
        </label>

        <label className="override-field">
          <span className="override-label">Priority</span>
          <select
            className="override-input"
            value={priority}
            onChange={(event) => setPriority(event.target.value as CommandPriority)}
          >
            {priorities.map((value) => (
              <option key={value} value={value}>
                {PRIORITY_HINTS[value] ?? value}
              </option>
            ))}
          </select>
        </label>

        <label className="override-check">
          <input
            type="checkbox"
            checked={dryRun}
            onChange={(event) => setDryRun(event.target.checked)}
          />
          <span>
            Dry run — validate and log without dispatching to the node
          </span>
        </label>

        {/* The summary restates the dispatch in words so the confirmation is
            read rather than reflexively ticked. */}
        <div className="override-summary">
          <span className="override-summary-label">Will send</span>
          <code className="override-summary-body">
            {humanise(commandType)} → {targetNode || "no target"}
            {parameters.trim() ? ` (${parameters.trim()})` : ""} · {priority}
            {dryRun ? " · dry run" : ""}
          </code>
        </div>

        <label className="override-check">
          <input
            type="checkbox"
            checked={confirmed}
            onChange={(event) => setConfirmed(event.target.checked)}
          />
          <span>I have checked the target node and parameters</span>
        </label>

        <div className="override-actions">
          <button
            type="submit"
            className="override-btn override-btn-send"
            disabled={!ready}
            title={
              ready
                ? "Queue this command on the node"
                : "Choose a target, enter parameters and confirm"
            }
          >
            {sending ? "Queueing…" : "Queue override"}
          </button>
          <button
            type="button"
            className="override-btn override-btn-reset"
            onClick={() => {
              setParameters("");
              setConfirmed(false);
              setPriority("Normal");
              setDryRun(true);
              setError(null);
              setSent(null);
              onTargetChange("");
            }}
          >
            Reset
          </button>
        </div>

        {/* Outcome of the last dispatch, stated rather than implied — an
            override that quietly did nothing is the worst case here. */}
        {error && (
          <p className="override-result override-result-error" role="alert">
            <strong>Rejected.</strong> {error}
          </p>
        )}

        {sent && !error && (
          <p className="override-result override-result-ok" role="status">
            <strong>Queued.</strong> {humanise(sent.commandType)} on {sent.nodeId}
            {sent.isDryRun ? " as a dry run" : ""} — watch for it in the stream.
          </p>
        )}
      </form>

      {/* The undo stack. Undo always acts on the top entry, the most recent
          override, and the API says in advance what undoing it will do. */}
      <div className="override-undo">
        <div className="override-undo-head">
          <h3 className="override-pending-title">
            Undo history <span className="override-undo-depth">{history.undo.length}</span>
          </h3>
          <div className="override-undo-controls">
            <button
              type="button"
              className="override-btn override-btn-undo"
              onClick={handleUndo}
              disabled={!top || stackBusy !== null}
              title={top ? `Undo ${humanise(top.commandType)} on ${top.nodeId}` : "Nothing to undo"}
            >
              {stackBusy === "undo" ? "Undoing…" : "↶ Undo"}
            </button>
            <button
              type="button"
              className="override-btn override-btn-redo"
              onClick={handleRedo}
              disabled={!redoTop || stackBusy !== null}
              title={redoTop ? `Redo ${humanise(redoTop.commandType)} on ${redoTop.nodeId}` : "Nothing to redo"}
            >
              {stackBusy === "redo" ? "Redoing…" : `↷ Redo${history.redo.length > 0 ? ` (${history.redo.length})` : ""}`}
            </button>
          </div>
        </div>

        {top ? (
          <>
            <div className="override-undo-top">
              <div className="override-undo-what">
                <span className="override-pending-node">{top.nodeId}</span>
                <span className="override-pending-type">
                  {humanise(top.commandType)} <code>{top.parameters}</code>
                </span>
                {top.status && (
                  <span className={`command-status cmd-status-${top.status.toLowerCase()}`}>
                    {top.status}
                  </span>
                )}
              </div>
              <p className="override-undo-plan">
                Undo will{" "}
                {top.status === "Queued"
                  ? "cancel it before it leaves the queue."
                  : `${top.undoDescription.charAt(0).toLowerCase()}${top.undoDescription.slice(1)}.`}
              </p>
            </div>

            {older.length > 0 && (
              <ul className="override-undo-list">
                {older.slice(0, HISTORY_PREVIEW).map((entry) => (
                  <li key={entry.commandId} className="override-undo-item">
                    <span className="override-pending-node">{entry.nodeId}</span>
                    <span className="override-pending-type">{humanise(entry.commandType)}</span>
                    <span className="override-pending-age">{formatRelative(entry.issuedUtc)}</span>
                  </li>
                ))}
                {older.length > HISTORY_PREVIEW && (
                  <li className="override-undo-more">+{older.length - HISTORY_PREVIEW} older</li>
                )}
              </ul>
            )}
          </>
        ) : (
          <p className="panel-empty">
            No overrides to undo. Every live (non-dry-run) override you queue is stacked here.
          </p>
        )}

        {redoTop && (
          <p className="override-redo-next">
            Redo will re-send <strong>{humanise(redoTop.commandType)}</strong>{" "}
            <code>{redoTop.parameters}</code> to {redoTop.nodeId}.
          </p>
        )}

        {stackResult && (
          <p
            className={`override-result ${stackResult.ok ? "override-result-ok" : "override-result-error"}`}
            role={stackResult.ok ? "status" : "alert"}
          >
            <strong>{stackResult.title}.</strong> {stackResult.message}
          </p>
        )}
      </div>

      <div className="override-pending">
        <h3 className="override-pending-title">In flight</h3>
        {pending.length === 0 ? (
          <p className="panel-empty">Nothing awaiting acknowledgement.</p>
        ) : (
          <ul className="override-pending-list">
            {pending.map((command) => (
              <li key={command.id} className="override-pending-item">
                <span className="override-pending-node">{command.nodeId}</span>
                <span className="override-pending-type">
                  {humanise(command.commandType)}
                </span>
                <span
                  className={`command-status cmd-status-${command.status.toLowerCase()}`}
                >
                  {command.status}
                </span>
                <span className="override-pending-age">
                  {formatRelative(command.issuedUtc)}
                </span>
              </li>
            ))}
          </ul>
        )}
      </div>
    </section>
  );
}

export default OverrideConsole;
