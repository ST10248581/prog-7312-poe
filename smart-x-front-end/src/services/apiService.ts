const API_BASE_URL = "http://localhost:5127/api";

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

/* ---------- Plumbing ---------- */

async function handleResponse<T>(response: Response): Promise<T> {
  if (!response.ok) {
    throw new Error(`API error: ${response.status} ${response.statusText}`);
  }
  return response.json() as Promise<T>;
}

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
  const response = await fetch(`${API_BASE_URL}/test`);
  return handleResponse<TestResponse>(response);
}

export async function getSummary(): Promise<EcosystemSummary> {
  const response = await fetch(`${API_BASE_URL}/telemetry/summary`);
  return handleResponse<EcosystemSummary>(response);
}

export async function getSensors(
  filters: SensorFilters = {}
): Promise<SensorListItem[]> {
  const query = buildQuery({
    categories: filters.categories,
    statuses: filters.statuses,
    zones: filters.zones,
    anomaliesOnly: filters.anomaliesOnly,
  });

  const response = await fetch(`${API_BASE_URL}/sensors${query}`);
  return handleResponse<SensorListItem[]>(response);
}

export async function getSensorDetail(id: string): Promise<SensorDetail> {
  const response = await fetch(`${API_BASE_URL}/sensors/${id}`);
  return handleResponse<SensorDetail>(response);
}

export async function getFilterOptions(): Promise<FilterOptions> {
  const response = await fetch(`${API_BASE_URL}/sensors/filter-options`);
  return handleResponse<FilterOptions>(response);
}

export async function getSeries(
  sensorProfileId: string,
  hours = 24,
  maxPoints = 180
): Promise<SensorSeries[]> {
  const query = buildQuery({ hours, maxPoints });
  const response = await fetch(
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

  const response = await fetch(`${API_BASE_URL}/telemetry/readings${query}`);
  return handleResponse<PagedResult<TelemetryReading>>(response);
}

export async function getAlerts(
  status?: AlertStatus,
  take = 25
): Promise<Alert[]> {
  const query = buildQuery({ status, take });
  const response = await fetch(`${API_BASE_URL}/alerts${query}`);
  return handleResponse<Alert[]>(response);
}

export async function getEngagement(userId?: string): Promise<EngagementState> {
  const query = buildQuery({ userId });
  const response = await fetch(`${API_BASE_URL}/engagement${query}`);
  return handleResponse<EngagementState>(response);
}

export async function createSensor(
  request: CreateSensorRequest
): Promise<SensorProfile> {
  const response = await fetch(`${API_BASE_URL}/sensors`, {
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
  const response = await fetch(`${API_BASE_URL}/sensors/${id}/payload`, {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(payload),
  });
  return handleResponse<SensorProfile>(response);
}

export async function uploadAttachment(
  sensorId: string,
  file: File,
  attachmentType: AttachmentType,
  description: string
): Promise<SensorAttachment> {
  const formData = new FormData();
  formData.append("file", file);
  formData.append("attachmentType", attachmentType);
  formData.append("description", description);

  const response = await fetch(
    `${API_BASE_URL}/sensors/${sensorId}/attachments`,
    { method: "POST", body: formData }
  );
  return handleResponse<SensorAttachment>(response);
}

export function getAttachmentDownloadUrl(
  sensorId: string,
  attachmentId: string
): string {
  return `${API_BASE_URL}/sensors/${sensorId}/attachments/${attachmentId}/download`;
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
};
