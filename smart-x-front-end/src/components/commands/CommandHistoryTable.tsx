import { formatDateTime, humanise } from "../../utils/format";
import NodeAlertBadge from "./NodeAlertBadge";
import type { CommandRecord } from "./types";

interface CommandHistoryTableProps {
  commands: CommandRecord[];
  page: number;
  pageSize: number;
  totalCount: number;
  onPageChange: (page: number) => void;
}

/**
 * The audit trail behind the stream: same filter, full detail, paged.
 *
 * Paging is a page number sent to the API, not a slice of an array held here —
 * the history is expected to outgrow anything worth keeping in the browser.
 */
function CommandHistoryTable({
  commands,
  page,
  pageSize,
  totalCount,
  onPageChange,
}: CommandHistoryTableProps) {
  const lastPage = Math.max(Math.ceil(totalCount / pageSize), 1);
  const firstRow = totalCount === 0 ? 0 : (page - 1) * pageSize + 1;
  const lastRow = Math.min(page * pageSize, totalCount);

  return (
    <section className="command-history" aria-label="Command history">
      <header className="panel-head">
        <h2>Command history</h2>
        <span className="panel-head-count">{totalCount} matching</span>
      </header>

      <div className="command-history-scroll">
        <table className="data-table">
          <thead>
            <tr>
              <th>Issued</th>
              <th>Node</th>
              <th>Category</th>
              <th>Command</th>
              <th>Parameters</th>
              <th>Origin</th>
              <th>Priority</th>
              <th>Issued by</th>
              <th>Status</th>
              <th>Retries</th>
              <th>Round trip</th>
            </tr>
          </thead>
          <tbody>
            {commands.map((command) => (
              <tr
                key={command.id}
                className={command.status === "Failed" ? "row-anomaly" : undefined}
              >
                <td className="mono">{formatDateTime(command.issuedUtc)}</td>
                <td className="mono">
                  {command.nodeId}
                  <NodeAlertBadge command={command} />
                </td>
                <td>
                  <span
                    className={`command-category category-${command.operationCategory.toLowerCase()}`}
                  >
                    {command.operationCategory}
                  </span>
                </td>
                <td>{humanise(command.commandType)}</td>
                <td className="mono">
                  {command.parameters}
                  {/* A dry run never reached the node, so the row should not
                      read as evidence that it did. */}
                  {command.isDryRun && <span className="command-dry-run">dry run</span>}
                </td>
                <td>
                  <span className={`command-origin origin-${command.origin.toLowerCase()}`}>
                    {command.origin}
                  </span>
                </td>
                <td>{command.priority}</td>
                <td>{command.issuedBy}</td>
                <td>
                  <span
                    className={`command-status cmd-status-${command.status.toLowerCase()}`}
                  >
                    {command.status}
                  </span>
                </td>
                <td className="mono">{command.retries}</td>
                <td className="mono">
                  {command.roundTripMs === null ? "—" : `${command.roundTripMs} ms`}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {commands.length === 0 && (
        <p className="panel-empty">No history for the current filters.</p>
      )}

      <footer className="command-history-foot">
        <span className="command-history-range">
          {firstRow}–{lastRow} of {totalCount}
        </span>
        <div className="command-history-pager">
          <button
            type="button"
            className="pager-btn"
            disabled={page <= 1}
            onClick={() => onPageChange(page - 1)}
          >
            Previous
          </button>
          <span className="pager-position">
            Page {page} of {lastPage}
          </span>
          <button
            type="button"
            className="pager-btn"
            disabled={page >= lastPage}
            onClick={() => onPageChange(page + 1)}
          >
            Next
          </button>
        </div>
      </footer>
    </section>
  );
}

export default CommandHistoryTable;
