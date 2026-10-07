import {
  API_BASE_URL,
  ApiError,
  REQUEST_TIMEOUT_MS,
  apiFetch,
  handleResponse,
  toApiError,
} from "./http";
import { reportApiConnection } from "./apiStatus";

export { ApiError } from "./http";

/* ---------- Types (mirror the API response models) ---------- */

export type SensorCategory =
  | "Environmental"
  | "PowerConsumption"
  | "Actuator"
  | "Motion"
  | "Connectivity";

export type SensorStatus = "Online" | "Warning" | "Offline";

export type ReadingType =
  | "Temperature"
  | "Humidity"
  | "Pressure"
  | "Power"
  | "Vibration"
  | "Motion";

export type ReadingQuality = "Good" | "Suspect" | "Bad";
export type AlertSeverity = "Info" | "Warning" | "Critical";
export type AlertStatus = "Active" | "Acknowledged" | "Resolved";
export type AlertType =
  | "ThresholdBreach"
  | "DeviceOffline"
  | "DataGap"
  | "LowBattery";
export type AttachmentType = "ConfigFile" | "DeploymentPhoto" | "HardwareLog";

interface TestResponse {
  status: string;
  message: string;
  timestamp: string;
}

export interface EcosystemSummary {
  totalSensors: number;
  onlineCount: number;
  warningCount: number;
  offlineCount: number;
  activeAlertCount: number;
  criticalAlertCount: number;
  totalReadings: number;
  readingsLastHour: number;
  anomaliesLastHour: number;
  meshHealthScore: number;
  averageProcessingMs: number;
  ingestSuccessRate: number;
  generatedUtc: string;
}

export interface SensorListItem {
  id: string;
  name: string;
  nodeId: string;
  macAddress: string;
  category: SensorCategory;
  room: string;
  zone: string;
  status: SensorStatus;
  firmwareVersion: string;
  lastSeenUtc: string;
  isActive: boolean;
  latestValue: number | null;
  latestUnit: string;
  primaryReadingType: ReadingType | null;
  activeAlertCount: number;
  anomalyCountLast24h: number;
  attachmentCount: number;
  sparkline: number[];
}

export interface SensorSeriesPoint {
  timestampUtc: string;
  value: number | null;
  booleanValue: boolean | null;
  isAnomaly: boolean;
  quality: ReadingQuality;
}

export interface SensorSeries {
  sensorProfileId: string;
  sensorName: string;
  nodeId: string;
  readingType: ReadingType;
  unit: string;
  minThreshold: number | null;
  maxThreshold: number | null;
  latestValue: number | null;
  anomalyCount: number;
  isStale: boolean;
  lastReadingUtc: string | null;
  points: SensorSeriesPoint[];
}

export interface SensorProfile {
  id: string;
  macAddress: string;
  serialNumber: string;
  name: string;
  category: SensorCategory;
  room: string;
  zone: string;
  nodeId: string;
  status: SensorStatus;
  firmwareVersion: string;
  registeredUtc: string;
  lastSeenUtc: string;
  isActive: boolean;
}

export interface SensorThreshold {
  id: string;
  sensorProfileId: string;
  readingType: ReadingType;
  minValue: number | null;
  maxValue: number | null;
  severity: AlertSeverity;
  isEnabled: boolean;
}

export interface SensorAttachment {
  id: string;
  sensorProfileId: string;
  fileName: string;
  storedFileName: string;
  contentType: string;
  fileSizeBytes: number;
  attachmentType: AttachmentType;
  uploadedUtc: string;
  uploadedBy: string;
  description: string;
  /** Hex SHA-256 of the original file; the API re-checks it on every download. */
  sha256: string;
  /** True for uploads, which are stored AES-GCM encrypted. Seeded records are not. */
  isEncrypted: boolean;
}

export interface IngestionBatch {
  id: string;
  sensorProfileId: string;
  receivedUtc: string;
  readingCount: number;
  acceptedCount: number;
  rejectedCount: number;
  sourceIpAddress: string;
  processingMs: number;
}

export interface TelemetryReading {
  id: number;
  sensorProfileId: string;
  readingType: ReadingType;
  numericValue: number | null;
  textValue: string | null;
  booleanValue: boolean | null;
  unit: string;
  timestampUtc: string;
  quality: ReadingQuality;
  isAnomaly: boolean;
}

export interface Alert {
  id: string;
  sensorProfileId: string;
  telemetryReadingId: number | null;
  alertType: AlertType;
  severity: AlertSeverity;
  message: string;
  triggeredUtc: string;
  acknowledgedUtc: string | null;
  status: AlertStatus;
}

export interface SensorDetail {
  profile: SensorProfile;
  thresholds: SensorThreshold[];
  attachments: SensorAttachment[];
  recentBatches: IngestionBatch[];
  recentReadings: TelemetryReading[];
  alerts: Alert[];
  series: SensorSeries[];
}

export interface FilterOptions {
  categories: string[];
  statuses: string[];
  readingTypes: string[];
  zones: string[];
  rooms: string[];
}

export interface EngagementState {
  id: string;
  userId: string;
  sensorsRegistered: number;
  readingsIngested: number;
  alertsResolved: number;
  attachmentsUploaded: number;
  meshHealthScore: number;
  lastUpdatedUtc: string;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface SensorFilters {
  categories?: string[];
  statuses?: string[];
  zones?: string[];
  anomaliesOnly?: boolean;
}

export interface UpdateSensorPayloadRequest {
  macAddress: string;
  room: string;
  zone: string;
  nodeId: string;
  category: SensorCategory;
}

export interface CreateSensorRequest {
  name: string;
  macAddress: string;
  room: string;
  zone: string;
  nodeId: string;
  category: SensorCategory;
}

/* ---------- Mesh-level types (deployment, load, ingestion) ---------- */

export type DeploymentTier = "Facility" | "Zone" | "SubZone" | "Node";

export type DeploymentIssueKind =
  | "UnnamedNode"
  | "TierOutOfOrder"
  | "EmptyBranch"
  | "DuplicateSibling"
  | "OrphanedSensor"
  | "DepthExceeded"
  | "UnreachableNode"
  | "CircularReference";

/** Recursive: every tier holds a list of the tier below it. */
export interface DeploymentNode {
  name: string;
  tier: DeploymentTier;
  sensorProfileId: string | null;
  status: SensorStatus | null;
  isActive: boolean;
  children: DeploymentNode[];
}

export interface DeploymentIssue {
  path: string;
  tier: DeploymentTier;
  kind: DeploymentIssueKind;
  severity: AlertSeverity;
  message: string;
}

export interface DeploymentValidationReport {
  root: DeploymentNode | null;
  nodesVisited: number;
  maxDepthReached: number;
  sensorsPlaced: number;
  validPaths: string[];
  issues: DeploymentIssue[];
  isValid: boolean;
  generatedUtc: string;
}

export interface LoadReading {
  sensorProfileId: string;
  name: string;
  nodeId: string;
  value: number;
  unit: string;
  sampleCount: number;
  lastReadingUtc: string | null;
}

export interface AggregateLoad {
  scope: string;
  totalValue: number;
  averageValue: number;
  unit: string;
  meterCount: number;
  contributors: LoadReading[];
  generatedUtc: string;
}

export interface LoadComparison {
  left: LoadReading;
  right: LoadReading;
  delta: number;
  unit: string;
  deltaPercent: number;
  leftIsHeavier: boolean;
  areEquivalent: boolean;
  summary: string;
}

export interface BatchStatistic {
  batchIndex: number;
  sampleCount: number;
  acceptedCount: number;
  rejectedCount: number;
  anomalyCount: number;
  minValue: number;
  maxValue: number;
  meanValue: number;
}

export interface TelemetryIngestResult {
  sensorProfileId: string;
  readingType: ReadingType;
  unit: string;
  /** CLR type the payload was wrapped in, e.g. "Double" or "Boolean". */
  payloadType: string;
  batchCount: number;
  rawSampleCount: number;
  acceptedCount: number;
  rejectedCount: number;
  anomalyCount: number;
  batchStatistics: BatchStatistic[];
  processingMs: number;
  ingestedUtc: string;
}

export type TelemetryPayloadKind = "Float" | "Double" | "Int" | "Bool";

/**
 * A lost sample is sent as the string "NaN" rather than omitted, so positions in
 * a batch stay aligned. JSON has no NaN literal; the API accepts the named form.
 */
export type TelemetrySample = number | "NaN";

export interface IngestTelemetryRequest {
  readingType: ReadingType;
  payloadKind?: TelemetryPayloadKind;
  startUtc?: string;
  intervalSeconds?: number;
  sourceIpAddress?: string;
  /** Jagged: one row per batch, rows may differ in length. */
  batches: TelemetrySample[][];
}

/* ---------- Command stream types ---------- */

export type CommandStatus =
  | "Queued"
  | "Sent"
  | "Acknowledged"
  | "Failed"
  | "Expired"
  /** Withdrawn by an undo before it left the queue. */
  | "Cancelled";

export type CommandOrigin = "Automation" | "Manual" | "Schedule";

export type CommandPriority = "Normal" | "High" | "Immediate";

export type CommandType =
  | "SetThreshold"
  | "Recalibrate"
  | "ToggleActuator"
  | "RestartNode"
  | "FirmwarePush"
  | "RequestSample";

/** The kind of operation a command performs; the API maps each type to one. */
export type OperationCategory =
  | "Configuration"
  | "Maintenance"
  | "Control"
  | "Diagnostics";

/**
 * The worst alert state standing against the node a command targets. `Clear`
 * means no alert was ever raised; `Resolved` means none is outstanding.
 */
export type NodeAlertState = "Clear" | "Resolved" | "Acknowledged" | "Active";

export interface DeviceCommand {
  id: string;
  sensorProfileId: string;
  nodeId: string;
  sensorName: string;
  zone: string;
  commandType: CommandType;
  /** Derived by the API from `commandType` — never sent back on a dispatch. */
  operationCategory: OperationCategory;
  /** Rendered as-is in the stream, e.g. `temp.max=28.5`. */
  parameters: string;
  origin: CommandOrigin;
  priority: CommandPriority;
  status: CommandStatus;
  issuedUtc: string;
  dispatchedUtc: string | null;
  acknowledgedUtc: string | null;
  /** Dispatch to acknowledgement; null while still in flight. */
  roundTripMs: number | null;
  issuedBy: string;
  retries: number;
  isDryRun: boolean;

  /* Alert context, resolved by the API per request rather than stored on the
     command — a node's alerts move independently of the commands sent to it. */
  nodeAlertState: NodeAlertState;
  /** Severity of the worst open alert on the node; null when there is none. */
  nodeAlertSeverity: AlertSeverity | null;
  nodeOpenAlertCount: number;
}

export interface CommandSummary {
  windowMinutes: number;
  dispatchRate: number;
  inFlightCount: number;
  queuedCount: number;
  retryingCount: number;
  acknowledgedRate: number;
  failedCount: number;
  expiredCount: number;
  manualOverrideCount: number;
  operatorCount: number;
  medianRoundTripMs: number;
  totalCount: number;
  /** Commands in the slice aimed at a node with an unacknowledged alert. */
  alertingCommandCount: number;
  /** Distinct nodes behind `alertingCommandCount`. */
  alertingNodeCount: number;
  /** How the slice splits across the operation categories, keyed by category. */
  categoryCounts: Record<string, number>;
  /** Commands per minute across the window, oldest first. */
  throughput: number[];
  generatedUtc: string;
}

/** One operation category and the command types it groups. */
export interface OperationCategoryOption {
  category: OperationCategory;
  commandTypes: CommandType[];
}

export interface CommandFilterOptions {
  statuses: CommandStatus[];
  origins: CommandOrigin[];
  commandTypes: CommandType[];
  priorities: CommandPriority[];
  zones: string[];
  operationCategories: OperationCategoryOption[];
  /** Node alert states, escalating — Clear through Active. */
  alertStates: NodeAlertState[];
  alertSeverities: AlertSeverity[];
  /** Operational categories of device, for the device-category facet. */
  sensorCategories: SensorCategory[];
  /** Node ids that can be targeted by a manual override. */
  nodes: string[];
  /** Command types each targetable node accepts, keyed by node id. */
  nodeCapabilities: Record<string, CommandType[]>;
}

/**
 * The whole command filter state, sent to the API as one query. Empty arrays
 * mean "no constraint" rather than "match nothing".
 */
export interface CommandQuery {
  statuses?: CommandStatus[];
  origins?: CommandOrigin[];
  commandTypes?: CommandType[];
  /** Combined with `commandTypes` using AND, not OR. */
  operationCategories?: OperationCategory[];
  /** Alert states of the target node to keep. */
  alertStates?: NodeAlertState[];
  /** Lowest severity a node's open alerts must reach to match. */
  minAlertSeverity?: AlertSeverity;
  /** Operational category of the target device. */
  sensorCategories?: SensorCategory[];
  zone?: string;
  search?: string;
  manualOnly?: boolean;
  windowMinutes?: number;
}

export interface DispatchCommandRequest {
  nodeId: string;
  commandType: CommandType;
  parameters: string;
  priority: CommandPriority;
  dryRun: boolean;
  issuedBy?: string;
}

/* ---------- Command engine: undo stack ---------- */

export type UndoOutcome = "Cancelled" | "Reverted" | "Irreversible" | "AlreadyUndone";
export type RedoOutcome = "Redone" | "AlreadyRedone";

/** One manual override on the API's undo stack. */
export interface OverrideHistoryEntry {
  commandId: string;
  nodeId: string;
  sensorName: string;
  commandType: CommandType;
  parameters: string;
  priority: CommandPriority;
  issuedBy: string;
  issuedUtc: string;
  /** The compensating command; null when the override has no inverse. */
  revertCommandType: CommandType | null;
  revertParameters: string | null;
  /** What undo will do, in words. */
  undoDescription: string;
  /** Set when this entry was created by redoing an earlier one. */
  redoOf: string | null;
  /** Current status of the original command. */
  status: CommandStatus | null;
}

export interface UndoResult {
  outcome: UndoOutcome;
  message: string;
  undone: OverrideHistoryEntry;
  revertCommand: DeviceCommand | null;
  remainingDepth: number;
  redoDepth: number;
}

export interface RedoResult {
  outcome: RedoOutcome;
  message: string;
  redone: OverrideHistoryEntry | null;
  command: DeviceCommand | null;
  undoDepth: number;
  redoDepth: number;
}

/** Both stacks, top first. */
export interface OverrideHistory {
  undo: OverrideHistoryEntry[];
  redo: OverrideHistoryEntry[];
}

/* ---------- Command engine: telemetry intake ---------- */

export type PacketLane = "Standard" | "Critical";
export type BreachDirection = "Low" | "High";

export interface StreamAlert {
  id: string;
  nodeId: string;
  sensorName: string;
  zone: string;
  alertType: AlertType;
  readingType: ReadingType | null;
  value: number | null;
  limit: number | null;
  unit: string;
  severity: AlertSeverity;
  lane: PacketLane;
  message: string;
  receivedUtc: string;
  processedUtc: string;
  queueWaitMs: number;
  autoCommandId: string | null;
  autoCommandSummary: string | null;
}

export interface DisconnectedNode {
  nodeId: string;
  sensorName: string;
  zone: string;
  sinceUtc: string | null;
  /** Repeat "link lost" reports the set absorbed for this node. */
  suppressedCount: number;
}

export interface ActiveErrorState {
  nodeId: string;
  alertType: AlertType;
  readingType: ReadingType | null;
  direction: BreachDirection | null;
  severity: AlertSeverity;
  sinceUtc: string;
  /** Repeat breach reports recognised and not re-alerted. */
  suppressedCount: number;
}

/** The disconnected set diffed against the one this client saw last poll. */
export interface PipelineSetChanges {
  compared: boolean;
  newlyDisconnected: string[];
  recovered: string[];
  stillDisconnected: number;
  /** Disconnected ∪ nodes with a critical open breach. */
  needsAttention: string[];
}

export interface PipelineStatus {
  standardQueueDepth: number;
  criticalQueueDepth: number;
  /** Backpressure limit on the standard queue. */
  standardQueueCapacity: number;
  standardBudgetPerTick: number;
  totalReceived: number;
  standardProcessed: number;
  criticalProcessed: number;
  /** Queued standard packets the critical lane has overtaken in total. */
  bypassedStandard: number;
  lastCriticalBypassed: number;
  /** Repeats the error-state and disconnected sets recognised and dropped. */
  duplicatesSuppressed: number;
  dropped: number;
  averageStandardWaitMs: number;
  averageCriticalWaitMs: number;
  registeredDevices: number;
  undoDepth: number;
  redoDepth: number;
  disconnectedNodes: DisconnectedNode[];
  errorStates: ActiveErrorState[];
  setChanges: PipelineSetChanges;
  /** Newest first. */
  recentAlerts: StreamAlert[];
  generatedUtc: string;
}

export interface StreamPacketRequest {
  nodeId?: string;
  macAddress?: string;
  readingType: ReadingType;
  value?: number | null;
  timestampUtc?: string;
  linkUp?: boolean;
}

export interface PacketIntakeResult {
  received: number;
  queuedStandard: number;
  processedCritical: number;
  suppressedDuplicates: number;
  rejected: string[];
  standardQueueDepth: number;
  bypassedStandard: number;
  droppedStandard: number;
  criticalAlerts: StreamAlert[];
}

/* ---------- Command engine: node timeline ---------- */

export type SensorLogKind =
  | "Reading"
  | "Command"
  | "Alert"
  | "Recovered"
  | "Disconnected"
  | "Reconnected";

export interface SensorLogEntry {
  timestampUtc: string;
  kind: SensorLogKind;
  readingType: ReadingType | null;
  value: number | null;
  label: string;
  severity: AlertSeverity | null;
}

export interface TimelineSeries {
  readingType: ReadingType;
  unit: string;
  minThreshold: number | null;
  maxThreshold: number | null;
  /** Already in timestamp order and down-sampled by the API. */
  points: { timestampUtc: string; value: number }[];
}

export interface NodeTimeline {
  nodeId: string;
  sensorName: string;
  zone: string;
  windowMinutes: number;
  fromUtc: string;
  toUtc: string;
  logSize: number;
  isDisconnected: boolean;
  /** Entries inside the window, found by binary search on the sorted log. */
  entriesInWindow: number;
  /** Older entries the range read skipped without visiting. */
  entriesSkipped: number;
  rangeReadMicroseconds: number;
  series: TimelineSeries[];
  /** Commands, alerts and link changes, oldest first. */
  events: SensorLogEntry[];
}

/* ---------- Command engine: live devices ---------- */

/** The device subset of the page filter — everything that describes a device rather than a command. */
export interface DeviceQuery {
  search?: string;
  sensorCategories?: SensorCategory[];
  alertStates?: NodeAlertState[];
  minAlertSeverity?: AlertSeverity;
  zone?: string;
}

export interface LiveReading {
  readingType: ReadingType;
  unit: string;
  value: number | null;
  timestampUtc: string | null;
  minThreshold: number | null;
  maxThreshold: number | null;
  isBoolean: boolean;
  outOfRange: boolean;
  /** Most recent values, oldest first. */
  recent: number[];
}

export interface LiveDevice {
  nodeId: string;
  sensorName: string;
  macAddress: string;
  category: SensorCategory;
  zone: string;
  room: string;
  isDisconnected: boolean;
  alertState: NodeAlertState;
  alertSeverity: AlertSeverity | null;
  openAlertCount: number;
  lastReadingUtc: string | null;
  readings: LiveReading[];
}

/** An exact dictionary lookup by node id or MAC address, timed. */
export interface DeviceLookupResult {
  key: string;
  normalisedKey: string;
  found: boolean;
  matchedBy: "NodeId" | "MacAddress" | null;
  elapsedMicroseconds: number;
  registrySize: number;
  device: LiveDevice | null;
}

export interface LiveDeviceResult {
  /** Every matching device, worst alert first. */
  items: LiveDevice[];
  totalRegistered: number;
  /** Counted with every filter applied except the facet's own. */
  categoryCounts: Record<string, number>;
  alertStateCounts: Record<string, number>;
  generatedUtc: string;
}

/* ---------- Command engine: suggested actions ---------- */

export type SuggestionKind = "PredictedAction" | "NextStep" | "ProblemDevice";

export interface SuggestedAction {
  /** Stable across refreshes, so a dismissal survives the next poll. */
  id: string;
  kind: SuggestionKind;
  title: string;
  reason: string;
  /** 0–1. */
  confidence: number;
  support: number;
  score: number;
  nodeId: string | null;
  sensorName: string | null;
  searchTerm: string | null;
  /** A learned filter: facet ("zone", "sensorCategory", "alertState", "status", "commandType") and value. */
  filterFacet: string | null;
  filterValue: string | null;
  commandType: CommandType | null;
  parameters: string | null;
  priority: CommandPriority | null;
  /** True when the target is unreachable, so the command can only be logged. */
  dryRun: boolean;
  signals: string[];
}

export interface InsightsResponse {
  suggestions: SuggestedAction[];
  observedActions: number;
  learnedAssociations: number;
  activeTriggers: number;
  learning: LearningStats;
  generatedUtc: string;
}

export interface LearnedRule {
  condition: string;
  action: string;
  count: number;
  support: number;
  confidence: number;
}

export interface LearningStats {
  rulesLearned: number;
  transitionsLearned: number;
  topRules: LearnedRule[];
  applied: number;
  dismissed: number;
  /** Applied ÷ (applied + dismissed); null before any feedback. */
  acceptanceRate: number | null;
  recentActivity: { description: string; atUtc: string }[];
}

export type OperatorActivityKind = "Search" | "SelectNode" | "Filter";
export type SuggestionFeedback = "Applied" | "Dismissed";

/* ---------- Plumbing ---------- */

function buildQuery(params: Record<string, unknown>): string {
  const search = new URLSearchParams();

  Object.entries(params).forEach(([key, value]) => {
    if (value === undefined || value === null || value === "") {
      return;
    }
    if (Array.isArray(value)) {
      // The API binds repeated keys into a List<T>.
      value.forEach((entry) => search.append(key, String(entry)));
      return;
    }
    search.append(key, String(value));
  });

  const query = search.toString();
  return query ? `?${query}` : "";
}

/* ---------- Read calls (writes land feature by feature) ---------- */

export async function testConnection(): Promise<TestResponse> {
  const response = await apiFetch(`${API_BASE_URL}/test`);
  return handleResponse<TestResponse>(response);
}

export async function getSummary(signal?: AbortSignal): Promise<EcosystemSummary> {
  const response = await apiFetch(`${API_BASE_URL}/telemetry/summary`, { signal });
  return handleResponse<EcosystemSummary>(response);
}

export async function getSensors(
  filters: SensorFilters = {},
  signal?: AbortSignal
): Promise<SensorListItem[]> {
  const query = buildQuery({
    categories: filters.categories,
    statuses: filters.statuses,
    zones: filters.zones,
    anomaliesOnly: filters.anomaliesOnly,
  });

  const response = await apiFetch(`${API_BASE_URL}/sensors${query}`, { signal });
  return handleResponse<SensorListItem[]>(response);
}

export async function getSensorDetail(id: string): Promise<SensorDetail> {
  const response = await apiFetch(`${API_BASE_URL}/sensors/${id}`);
  return handleResponse<SensorDetail>(response);
}

export async function getFilterOptions(signal?: AbortSignal): Promise<FilterOptions> {
  const response = await apiFetch(`${API_BASE_URL}/sensors/filter-options`, { signal });
  return handleResponse<FilterOptions>(response);
}

export async function getSeries(
  sensorProfileId: string,
  hours = 24,
  maxPoints = 180
): Promise<SensorSeries[]> {
  const query = buildQuery({ hours, maxPoints });
  const response = await apiFetch(
    `${API_BASE_URL}/telemetry/series/${sensorProfileId}${query}`
  );
  return handleResponse<SensorSeries[]>(response);
}

export async function getReadings(
  page = 1,
  pageSize = 50,
  filters: SensorFilters & { sensorProfileIds?: string[] } = {}
): Promise<PagedResult<TelemetryReading>> {
  const query = buildQuery({
    page,
    pageSize,
    sensorProfileIds: filters.sensorProfileIds,
    categories: filters.categories,
    statuses: filters.statuses,
    zones: filters.zones,
    anomaliesOnly: filters.anomaliesOnly,
  });

  const response = await apiFetch(`${API_BASE_URL}/telemetry/readings${query}`);
  return handleResponse<PagedResult<TelemetryReading>>(response);
}

export async function getAlerts(
  status?: AlertStatus,
  take = 25,
  signal?: AbortSignal
): Promise<Alert[]> {
  const query = buildQuery({ status, take });
  const response = await apiFetch(`${API_BASE_URL}/alerts${query}`, { signal });
  return handleResponse<Alert[]>(response);
}

export async function getEngagement(userId?: string, signal?: AbortSignal): Promise<EngagementState> {
  const query = buildQuery({ userId });
  const response = await apiFetch(`${API_BASE_URL}/engagement${query}`, { signal });
  return handleResponse<EngagementState>(response);
}

export async function createSensor(
  request: CreateSensorRequest
): Promise<SensorProfile> {
  const response = await apiFetch(`${API_BASE_URL}/sensors`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(request),
  });
  return handleResponse<SensorProfile>(response);
}

export async function updateSensorPayload(
  id: string,
  payload: UpdateSensorPayloadRequest
): Promise<SensorProfile> {
  const response = await apiFetch(`${API_BASE_URL}/sensors/${id}/payload`, {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(payload),
  });
  return handleResponse<SensorProfile>(response);
}

export interface UploadOptions {
  /** Called as the body is sent, with a fraction from 0 to 1. */
  onProgress?: (fraction: number) => void;
  signal?: AbortSignal;
}

/**
 * Uploads a file as multipart/form-data. Uses XMLHttpRequest rather than
 * fetch because only XHR reports upload progress. The timeout restarts with
 * every progress event, so a large file on a slow link is not cut off while
 * bytes are still moving, but a stalled upload is.
 */
export function uploadAttachment(
  sensorId: string,
  file: File,
  attachmentType: AttachmentType,
  description: string,
  { onProgress, signal }: UploadOptions = {}
): Promise<SensorAttachment> {
  const formData = new FormData();
  formData.append("file", file);
  formData.append("attachmentType", attachmentType);
  formData.append("description", description);

  return new Promise((resolve, reject) => {
    const xhr = new XMLHttpRequest();
    let stallTimer = 0;

    const armStallTimer = () => {
      window.clearTimeout(stallTimer);
      stallTimer = window.setTimeout(() => xhr.abort(), REQUEST_TIMEOUT_MS * 3);
    };

    const fail = (error: Error) => {
      window.clearTimeout(stallTimer);
      signal?.removeEventListener("abort", onAbort);
      reject(error);
    };

    const onAbort = () => xhr.abort();
    signal?.addEventListener("abort", onAbort);

    xhr.upload.onprogress = (event) => {
      armStallTimer();
      if (event.lengthComputable) {
        onProgress?.(event.loaded / event.total);
      }
    };

    xhr.onload = async () => {
      window.clearTimeout(stallTimer);
      signal?.removeEventListener("abort", onAbort);
      reportApiConnection("online");

      const response = new Response(xhr.responseText || null, {
        status: xhr.status,
        statusText: xhr.statusText,
        headers: { "Content-Type": xhr.getResponseHeader("Content-Type") ?? "application/json" },
      });

      if (!response.ok) {
        reject(await toApiError(response));
        return;
      }
      resolve((await response.json()) as SensorAttachment);
    };

    xhr.onerror = () => {
      reportApiConnection("offline");
      fail(new ApiError("The Smart-X API could not be reached.", "network"));
    };

    xhr.onabort = () => {
      fail(
        signal?.aborted
          ? new DOMException("Upload cancelled.", "AbortError")
          : new ApiError("The upload stalled and was stopped.", "timeout")
      );
    };

    xhr.open("POST", `${API_BASE_URL}/sensors/${sensorId}/attachments`);
    armStallTimer();
    xhr.send(formData);
  });
}

export function getAttachmentDownloadUrl(
  sensorId: string,
  attachmentId: string
): string {
  return `${API_BASE_URL}/sensors/${sensorId}/attachments/${attachmentId}/download`;
}

/* ---------- Mesh-level calls ---------- */

/** Validated deployment tree. Pass a zone to validate one branch of the mesh. */
export async function getDeployment(
  zone?: string
): Promise<DeploymentValidationReport> {
  const query = buildQuery({ zone });
  const response = await apiFetch(`${API_BASE_URL}/mesh/deployment${query}`);
  return handleResponse<DeploymentValidationReport>(response);
}

export async function getAggregateLoad(
  sensorIds: string[]
): Promise<AggregateLoad> {
  const query = buildQuery({ sensorIds });
  const response = await apiFetch(`${API_BASE_URL}/mesh/load${query}`);
  return handleResponse<AggregateLoad>(response);
}

export async function getZoneLoad(zone: string): Promise<AggregateLoad> {
  const response = await apiFetch(
    `${API_BASE_URL}/mesh/load/zone/${encodeURIComponent(zone)}`
  );
  return handleResponse<AggregateLoad>(response);
}

export async function compareLoad(
  left: string,
  right: string
): Promise<LoadComparison> {
  const query = buildQuery({ left, right });
  const response = await apiFetch(`${API_BASE_URL}/mesh/load/compare${query}`);
  return handleResponse<LoadComparison>(response);
}

export async function ingestBatches(
  sensorId: string,
  request: IngestTelemetryRequest
): Promise<TelemetryIngestResult> {
  const response = await apiFetch(
    `${API_BASE_URL}/mesh/sensors/${sensorId}/ingest`,
    {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(request),
    }
  );
  return handleResponse<TelemetryIngestResult>(response);
}

/* ---------- Command stream calls ---------- */

/** Every command endpoint takes the same filter, so the query is built once. */
function buildCommandQuery(query: CommandQuery, extra: Record<string, unknown> = {}): string {
  return buildQuery({
    statuses: query.statuses,
    origins: query.origins,
    commandTypes: query.commandTypes,
    operationCategories: query.operationCategories,
    alertStates: query.alertStates,
    minAlertSeverity: query.minAlertSeverity,
    sensorCategories: query.sensorCategories,
    zone: query.zone,
    search: query.search,
    manualOnly: query.manualOnly,
    windowMinutes: query.windowMinutes,
    ...extra,
  });
}

/** Audit trail: the filtered history, newest first, one page at a time. */
export async function getCommands(
  query: CommandQuery = {},
  page = 1,
  pageSize = 25,
  signal?: AbortSignal
): Promise<PagedResult<DeviceCommand>> {
  const search = buildCommandQuery(query, { page, pageSize });
  const response = await apiFetch(`${API_BASE_URL}/commands${search}`, { signal });
  return handleResponse<PagedResult<DeviceCommand>>(response);
}

/** Live tail: the newest commands in the window, capped by the API. */
export async function getCommandStream(
  query: CommandQuery = {},
  take = 40,
  signal?: AbortSignal
): Promise<DeviceCommand[]> {
  const search = buildCommandQuery(query, { take });
  const response = await apiFetch(`${API_BASE_URL}/commands/stream${search}`, { signal });
  return handleResponse<DeviceCommand[]>(response);
}

export async function getCommandSummary(query: CommandQuery = {}, signal?: AbortSignal): Promise<CommandSummary> {
  const search = buildCommandQuery(query);
  const response = await apiFetch(`${API_BASE_URL}/commands/summary${search}`, { signal });
  return handleResponse<CommandSummary>(response);
}

export async function getCommandFilterOptions(signal?: AbortSignal): Promise<CommandFilterOptions> {
  const response = await apiFetch(`${API_BASE_URL}/commands/filter-options`, { signal });
  return handleResponse<CommandFilterOptions>(response);
}

/** Queue a manual override. Rejections come back as an error message. */
export async function dispatchCommand(
  request: DispatchCommandRequest
): Promise<DeviceCommand> {
  const response = await apiFetch(`${API_BASE_URL}/commands`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(request),
  });
  return handleResponse<DeviceCommand>(response);
}

/** The undo and redo stacks, top first. */
export async function getOverrideHistory(signal?: AbortSignal): Promise<OverrideHistory> {
  const response = await apiFetch(`${API_BASE_URL}/commands/overrides`, { signal });
  return handleResponse<OverrideHistory>(response);
}

/** Pops the most recent override: cancels it if still queued, otherwise sends its inverse. */
export async function undoLastOverride(
  expectedCommandId?: string,
  issuedBy = "operator"
): Promise<UndoResult> {
  const response = await apiFetch(`${API_BASE_URL}/commands/overrides/undo`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    // Naming the entry makes the request idempotent: if it was already undone,
    // the API reports that instead of undoing the next one down.
    body: JSON.stringify({ issuedBy, expectedCommandId }),
  });
  return handleResponse<UndoResult>(response);
}

/** Re-applies the most recently undone override. Idempotent in the same way. */
export async function redoLastUndo(
  expectedCommandId?: string,
  issuedBy = "operator"
): Promise<RedoResult> {
  const response = await apiFetch(`${API_BASE_URL}/commands/overrides/redo`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ issuedBy, expectedCommandId }),
  });
  return handleResponse<RedoResult>(response);
}

/**
 * The intake state. `known` is the disconnected set this client saw last time;
 * the API answers with what changed since (set difference).
 */
export async function getPipelineStatus(
  known?: string[],
  signal?: AbortSignal
): Promise<PipelineStatus> {
  // An empty set is still a comparison: send a blank entry so the API sees it.
  const query = known === undefined ? "" : buildQuery({ known: known.length > 0 ? known : [""] });
  const response = await apiFetch(`${API_BASE_URL}/commands/pipeline${query}`, { signal });
  return handleResponse<PipelineStatus>(response);
}

/** Exact O(1) lookup by node id or MAC address (any notation), with the probe time. */
export async function lookupDevice(key: string, signal?: AbortSignal): Promise<DeviceLookupResult> {
  const query = buildQuery({ key });
  const response = await apiFetch(`${API_BASE_URL}/commands/devices/lookup${query}`, { signal });
  return handleResponse<DeviceLookupResult>(response);
}

/** Posts packets to the intake. Critical ones are processed before this resolves. */
export async function ingestStreamPackets(
  packets: StreamPacketRequest[]
): Promise<PacketIntakeResult> {
  const response = await apiFetch(`${API_BASE_URL}/commands/packets`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(packets),
  });
  return handleResponse<PacketIntakeResult>(response);
}

export async function getNodeTimeline(
  nodeId: string,
  minutes = 60,
  maxPoints = 120,
  signal?: AbortSignal
): Promise<NodeTimeline> {
  const query = buildQuery({ minutes, maxPoints });
  const response = await apiFetch(
    `${API_BASE_URL}/commands/nodes/${encodeURIComponent(nodeId)}/timeline${query}`, { signal }
  );
  return handleResponse<NodeTimeline>(response);
}

/** Every registered device matching the filter, with its latest readings. */
export async function getLiveDevices(query: DeviceQuery = {}, signal?: AbortSignal): Promise<LiveDeviceResult> {
  const search = buildQuery({
    search: query.search,
    sensorCategories: query.sensorCategories,
    alertStates: query.alertStates,
    minAlertSeverity: query.minAlertSeverity,
    zone: query.zone,
  });
  const response = await apiFetch(`${API_BASE_URL}/commands/devices${search}`, { signal });
  return handleResponse<LiveDeviceResult>(response);
}

export async function getInsights(issuedBy = "operator", signal?: AbortSignal): Promise<InsightsResponse> {
  const query = buildQuery({ issuedBy });
  const response = await apiFetch(`${API_BASE_URL}/commands/insights${query}`, { signal });
  return handleResponse<InsightsResponse>(response);
}

/**
 * Tells the action engine what the operator just did, so it can learn habits.
 * Fire-and-forget: a failure here must never get in the operator's way.
 */
export async function recordActivity(
  kind: OperatorActivityKind,
  value: string,
  issuedBy = "operator"
): Promise<void> {
  await apiFetch(`${API_BASE_URL}/commands/activity`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ kind, value, issuedBy }),
  }).catch(() => undefined);
}

/** Applied or dismissed: the engine re-ranks that suggestion from now on. */
export async function sendSuggestionFeedback(
  suggestionId: string,
  outcome: SuggestionFeedback,
  issuedBy = "operator"
): Promise<void> {
  await apiFetch(`${API_BASE_URL}/commands/insights/feedback`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ suggestionId, outcome, issuedBy }),
  }).catch(() => undefined);
}

/** Clears everything the action engine has learned, to demonstrate learning from nothing. */
export async function resetLearning(): Promise<void> {
  const response = await apiFetch(`${API_BASE_URL}/commands/insights/reset`, { method: "POST" });
  if (!response.ok) {
    throw await toApiError(response);
  }
}

export default {
  testConnection,
  getSummary,
  getSensors,
  getSensorDetail,
  getFilterOptions,
  getSeries,
  getReadings,
  getAlerts,
  getEngagement,
  createSensor,
  updateSensorPayload,
  uploadAttachment,
  getAttachmentDownloadUrl,
  getDeployment,
  getAggregateLoad,
  getZoneLoad,
  compareLoad,
  ingestBatches,
  getCommands,
  getCommandStream,
  getCommandSummary,
  getCommandFilterOptions,
  dispatchCommand,
  getOverrideHistory,
  undoLastOverride,
  redoLastUndo,
  getPipelineStatus,
  lookupDevice,
  sendSuggestionFeedback,
  resetLearning,
  ingestStreamPackets,
  getNodeTimeline,
  getLiveDevices,
  getInsights,
  recordActivity,
};
