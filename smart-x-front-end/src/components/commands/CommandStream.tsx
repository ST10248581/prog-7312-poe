import { formatRelative, formatTime, humanise } from "../../utils/format";
import type { CommandRecord } from "./types";

interface CommandStreamProps {
  commands: CommandRecord[];
  selectedId: string | null;
  live: boolean;
  /** First paint only; refreshes swap rows in place rather than emptying the list. */
  loading: boolean;
  onSelect: (command: CommandRecord) => void;
}

/**
 * The live tail. Newest first, capped by the API rather than the browser, and
 * every row is a jump-off point: selecting one targets the override console at
 * that node so issuing a correction never means retyping the node id.
 */
function CommandStream({
  commands,
  selectedId,
  live,
  loading,
  onSelect,
}: CommandStreamProps) {
  return (
    <section className="command-stream-panel" aria-label="Live command stream">
      <header className="panel-head">
        <h2>Command stream</h2>
        <div className="panel-head-actions">
          <span className={`stream-state${live ? " live" : ""}`}>
            {live ? "streaming" : "paused"}
          </span>
          <span className="panel-head-count">
            {loading ? "loading…" : `${commands.length} in window`}
          </span>
        </div>
      </header>

      {commands.length === 0 ? (
        <p className="panel-empty">
          {loading ? "Loading the command stream…" : "No commands match the current filters."}
        </p>
      ) : (
        <ul className="command-stream">
          {commands.map((command) => (
            <li key={command.id}>
              <button
                type="button"
                className={`command-row cmd-status-${command.status.toLowerCase()}${
                  command.id === selectedId ? " selected" : ""
                }`}
                onClick={() => onSelect(command)}
                aria-pressed={command.id === selectedId}
              >
                <span className="command-row-time">{formatTime(command.issuedUtc)}</span>

                <span className="command-row-node">{command.nodeId}</span>

                <span className="command-row-main">
                  <span className="command-row-type">{humanise(command.commandType)}</span>
                  <code className="command-row-params">{command.parameters}</code>
                </span>

                <span className={`command-origin origin-${command.origin.toLowerCase()}`}>
                  {command.origin}
                </span>

                <span className={`command-status cmd-status-${command.status.toLowerCase()}`}>
                  {command.status}
                </span>

                <span className="command-row-latency">
                  {command.roundTripMs === null ? "—" : `${command.roundTripMs} ms`}
                </span>

                <span className="command-row-age">{formatRelative(command.issuedUtc)}</span>
              </button>
            </li>
          ))}
        </ul>
      )}

      <footer className="command-stream-foot">
        The stream shows the newest page the API returns for the active filters.
        Older commands stay in the history table below rather than growing this list.
      </footer>
    </section>
  );
}

export default CommandStream;
