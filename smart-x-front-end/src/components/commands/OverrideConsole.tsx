import { useState } from "react";
import { formatRelative, humanise } from "../../utils/format";
import { COMMAND_TYPES } from "./types";
import type { CommandRecord, CommandType } from "./types";

interface OverrideConsoleProps {
  nodes: string[];
  /** Node id taken from the stream selection; the operator can still change it. */
  targetNode: string;
  pending: CommandRecord[];
  onTargetChange: (nodeId: string) => void;
}

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
 * Layout stage: the form holds its own state and dispatch is disabled. The
 * submit handler is where `POST /api/commands` will go.
 */
function OverrideConsole({
  nodes,
  targetNode,
  pending,
  onTargetChange,
}: OverrideConsoleProps) {
  const [commandType, setCommandType] = useState<CommandType>("SetThreshold");
  const [parameters, setParameters] = useState("");
  const [priority, setPriority] = useState("Normal");
  const [dryRun, setDryRun] = useState(true);
  const [confirmed, setConfirmed] = useState(false);

  const ready = targetNode !== "" && parameters.trim() !== "" && confirmed;

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

      <form
        className="override-form"
        onSubmit={(event) => {
          // Layout stage: nothing is dispatched. POST /api/commands lands here.
          event.preventDefault();
        }}
      >
        <label className="override-field">
          <span className="override-label">Target node</span>
          <select
            className="override-input"
            value={targetNode}
            onChange={(event) => onTargetChange(event.target.value)}
          >
            <option value="">Select a node…</option>
            {nodes.map((node) => (
              <option key={node} value={node}>
                {node}
              </option>
            ))}
          </select>
        </label>

        <label className="override-field">
          <span className="override-label">Command</span>
          <select
            className="override-input"
            value={commandType}
            onChange={(event) => setCommandType(event.target.value as CommandType)}
          >
            {COMMAND_TYPES.map((type) => (
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
            onChange={(event) => setPriority(event.target.value)}
          >
            <option value="Normal">Normal — queued behind automation</option>
            <option value="High">High — jumps the queue</option>
            <option value="Immediate">Immediate — pre-empts in-flight work</option>
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
                ? "Dispatch is not wired up yet"
                : "Choose a target, enter parameters and confirm"
            }
          >
            Queue override
          </button>
          <button
            type="button"
            className="override-btn override-btn-reset"
            onClick={() => {
              setParameters("");
              setConfirmed(false);
              setPriority("Normal");
              setDryRun(true);
              onTargetChange("");
            }}
          >
            Reset
          </button>
        </div>

        <p className="override-pending-note">
          Dispatch is not connected yet — this form is the planned layout only.
        </p>
      </form>

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
