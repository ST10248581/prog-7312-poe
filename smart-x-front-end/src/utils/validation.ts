import type { AttachmentType, SensorCategory } from "../services/apiService";

/**
 * Client-side mirror of the API's validation rules
 * (SmartX.Api/Models/Validation/SensorRules.cs and
 * Logic/Attachments/AttachmentPolicy.cs). Forms check these as the user types
 * so mistakes are caught before a request is sent; the API applies the same
 * rules again, because the client can always be bypassed.
 */

export const MAC_ADDRESS_PATTERN = /^([0-9A-Fa-f]{2}[:-]){5}[0-9A-Fa-f]{2}$/;
export const NODE_ID_PATTERN = /^[A-Za-z]{2,5}-[0-9]{3,4}$/;
const NAME_PATTERN = /^[A-Za-z0-9][A-Za-z0-9 ._\-()]*$/;
const ROOM_PATTERN = /^[A-Za-z0-9][A-Za-z0-9 .\-/]*$/;
const ZONE_PATTERN = /^Zone [A-Z]$/;

export const SENSOR_CATEGORIES: SensorCategory[] = [
  "Environmental",
  "PowerConsumption",
  "Actuator",
  "Motion",
  "Connectivity",
];

/** Used until the API's filter options arrive, and if they never do. */
export const DEFAULT_ZONES = ["Zone A", "Zone B", "Zone C", "Zone D", "Zone E"];

export interface RegistrationFields {
  name?: string;
  macAddress: string;
  room: string;
  zone: string;
  nodeId: string;
  category: SensorCategory;
}

export type FieldErrors<T> = Partial<Record<keyof T, string>>;

/** Upper-cases and swaps hyphens for colons once the address is complete. */
export function normaliseMacAddress(value: string): string {
  const trimmed = value.trim();
  return MAC_ADDRESS_PATTERN.test(trimmed) ? trimmed.replace(/-/g, ":").toUpperCase() : value;
}

/**
 * Every rule a registration must pass, as one message per field. `name` is
 * only checked when present, so the same function serves the edit form, which
 * has no name field.
 */
export function validateRegistration<T extends RegistrationFields>(form: T): FieldErrors<T> {
  const errors: FieldErrors<RegistrationFields> = {};

  if (form.name !== undefined) {
    const name = form.name.trim();
    if (!name) errors.name = "Device name is required.";
    else if (name.length < 3 || name.length > 60) errors.name = "Device name must be 3 to 60 characters.";
    else if (!NAME_PATTERN.test(name)) errors.name = "Use letters, digits, spaces and . _ - ( ) only.";
  }

  const mac = form.macAddress.trim();
  if (!mac) errors.macAddress = "MAC address is required.";
  else if (!MAC_ADDRESS_PATTERN.test(mac)) errors.macAddress = "Six hex pairs separated by : or -, e.g. 5C:A1:2A:2C:3C:6F.";

  const room = form.room.trim();
  if (!room) errors.room = "Room is required.";
  else if (room.length < 2 || room.length > 40) errors.room = "Room must be 2 to 40 characters.";
  else if (!ROOM_PATTERN.test(room)) errors.room = "Use letters, digits, spaces and . - / only.";

  if (!form.zone) errors.zone = "Choose the zone the device is installed in.";
  else if (!ZONE_PATTERN.test(form.zone)) errors.zone = "Choose one of the mesh zones.";

  const nodeId = form.nodeId.trim();
  if (!nodeId) errors.nodeId = "Node ID is required.";
  else if (!NODE_ID_PATTERN.test(nodeId)) errors.nodeId = "A 2-5 letter prefix and a 3-4 digit number, e.g. ENV-041.";

  if (!SENSOR_CATEGORIES.includes(form.category)) errors.category = "Choose a sensor category.";

  return errors as FieldErrors<T>;
}

export const hasErrors = (errors: object) => Object.keys(errors).length > 0;

/* ---------- Attachments ---------- */

/** Must not exceed the API's Attachments:MaxFileSizeBytes. */
export const MAX_ATTACHMENT_BYTES = 10 * 1024 * 1024;

const ALLOWED_EXTENSIONS: Record<AttachmentType, string[]> = {
  ConfigFile: [".json", ".yaml", ".yml", ".xml", ".ini", ".conf", ".cfg", ".txt"],
  DeploymentPhoto: [".jpg", ".jpeg", ".png", ".webp"],
  HardwareLog: [".log", ".txt", ".csv", ".json"],
};

export function allowedExtensions(type: AttachmentType): string[] {
  return ALLOWED_EXTENSIONS[type];
}

/** The `accept` attribute for the file picker, so the OS dialog filters too. */
export function acceptAttribute(type: AttachmentType): string {
  return ALLOWED_EXTENSIONS[type].join(",");
}

/** Checks name and size before upload. Content is checked by the API. */
export function validateAttachment(file: File, type: AttachmentType): string | null {
  const dot = file.name.lastIndexOf(".");
  const extension = dot >= 0 ? file.name.slice(dot).toLowerCase() : "";
  const allowed = ALLOWED_EXTENSIONS[type];

  if (!allowed.includes(extension)) {
    return `${extension ? `${extension} files` : "Files without an extension"} can't be attached as ${type
      .replace(/([a-z])([A-Z])/g, "$1 $2")
      .toLowerCase()}. Allowed: ${allowed.join(", ")}.`;
  }
  if (file.size === 0) {
    return "The file is empty.";
  }
  if (file.size > MAX_ATTACHMENT_BYTES) {
    return `The file is ${(file.size / (1024 * 1024)).toFixed(1)} MB; the limit is ${MAX_ATTACHMENT_BYTES / (1024 * 1024)} MB.`;
  }
  return null;
}
