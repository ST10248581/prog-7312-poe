import type { CommandRecord } from "./types";

interface NodeAlertBadgeProps {
  command: CommandRecord;
}

/**
 * Marks a command aimed at a node that is currently alerting.
 *
 * The state comes back on the command itself, resolved by the API against the
 * live alert log, so the badge and the alert filter can never disagree about
 * which nodes are alerting. Nodes with nothing outstanding carry no badge —
 * a marker on every row would mark nothing.
 */
function NodeAlertBadge({ command }: NodeAlertBadgeProps) {
  if (command.nodeAlertState !== "Active" && command.nodeAlertState !== "Acknowledged") {
    return null;
  }

  const severity = command.nodeAlertSeverity ?? "Info";

  return (
    <span
      className={`node-alert node-alert-${command.nodeAlertState.toLowerCase()} severity-${severity.toLowerCase()}`}
      title={`${command.nodeOpenAlertCount} open alert${
        command.nodeOpenAlertCount === 1 ? "" : "s"
      } on ${command.nodeId} — worst is ${severity}, ${command.nodeAlertState.toLowerCase()}`}
    >
      <span className="node-alert-dot" aria-hidden="true" />
      {command.nodeOpenAlertCount}
    </span>
  );
}

export default NodeAlertBadge;
