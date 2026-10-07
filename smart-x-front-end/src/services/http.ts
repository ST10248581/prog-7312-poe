import { reportApiConnection } from "./apiStatus";

/**
 * Transport for every API call: configuration, timeouts, retries and error
 * decoding live here so apiService.ts only describes endpoints.
 */

/** Base URL of the API, from VITE_API_BASE_URL (.env.development). */
export const API_BASE_URL = (
  import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5127/api"
).replace(/\/+$/, "");

/** A request still pending after this long is abandoned rather than left to hang the UI. */
export const REQUEST_TIMEOUT_MS = Number(import.meta.env.VITE_API_TIMEOUT_MS) || 10_000;

/** Reads are retried this many times after a network failure, timeout or 5xx. */
const READ_RETRIES = 1;
const RETRY_DELAY_MS = 600;

export type ApiErrorKind = "network" | "timeout" | "http";

/**
 * A failed API call. `fieldErrors` carries the per-field messages from an
 * ASP.NET ValidationProblemDetails response (400 or 409), keyed by the
 * camelCase field name so a form can show each one beside its input.
 */
export class ApiError extends Error {
  readonly kind: ApiErrorKind;
  readonly status: number | null;
  readonly fieldErrors: Record<string, string>;

  constructor(
    message: string,
    kind: ApiErrorKind,
    status: number | null = null,
    fieldErrors: Record<string, string> = {}
  ) {
    super(message);
    this.name = "ApiError";
    this.kind = kind;
    this.status = status;
    this.fieldErrors = fieldErrors;
  }

  /** True when the API could not be reached at all, as opposed to rejecting the request. */
  get isUnreachable(): boolean {
    return this.kind !== "http";
  }
}

export interface ApiFetchOptions extends RequestInit {
  /** Overrides the default timeout for slow operations such as uploads. */
  timeoutMs?: number;
}

const sleep = (ms: number) => new Promise((resolve) => setTimeout(resolve, ms));

/**
 * fetch with a timeout, one retry for idempotent reads, and connection
 * reporting. A caller's own AbortSignal (e.g. a page unmounting) still cancels
 * the request immediately and is never retried.
 */
export async function apiFetch(url: string, options: ApiFetchOptions = {}): Promise<Response> {
  const { timeoutMs = REQUEST_TIMEOUT_MS, signal: callerSignal, ...init } = options;
  const method = (init.method ?? "GET").toUpperCase();
  const retries = method === "GET" ? READ_RETRIES : 0;

  for (let attempt = 0; ; attempt++) {
    const timeout = AbortSignal.timeout(timeoutMs);
    const signal = callerSignal ? AbortSignal.any([callerSignal, timeout]) : timeout;

    try {
      const response = await fetch(url, { ...init, signal });
      reportApiConnection("online");

      // A 5xx on a read is often transient (e.g. the API restarting): try once more.
      if (response.status >= 500 && attempt < retries) {
        await sleep(RETRY_DELAY_MS * (attempt + 1));
        continue;
      }

      return response;
    } catch (error) {
      if (callerSignal?.aborted) {
        throw error;
      }

      const timedOut = timeout.aborted;
      if (attempt < retries) {
        await sleep(RETRY_DELAY_MS * (attempt + 1));
        continue;
      }

      reportApiConnection("offline");
      throw timedOut
        ? new ApiError(`The Smart-X API did not respond within ${timeoutMs / 1000} s.`, "timeout")
        : new ApiError("The Smart-X API could not be reached.", "network");
    }
  }
}

interface ProblemBody {
  title?: string;
  detail?: string;
  error?: string;
  errors?: Record<string, string[]>;
}

/** "MacAddress" -> "macAddress", matching the request objects the forms send. */
const camelCase = (key: string) => (key ? key[0].toLowerCase() + key.slice(1) : key);

/** Turns a non-2xx response into an ApiError, keeping any per-field messages. */
export async function toApiError(response: Response): Promise<ApiError> {
  const body = (await response.json().catch(() => null)) as ProblemBody | string | null;

  if (typeof body === "string") {
    return new ApiError(body, "http", response.status);
  }

  const fieldErrors: Record<string, string> = {};
  for (const [key, messages] of Object.entries(body?.errors ?? {})) {
    if (messages.length > 0) {
      fieldErrors[camelCase(key)] = messages[0];
    }
  }

  const fieldSummary = Object.values(fieldErrors)[0];
  const message =
    body?.error ??
    fieldSummary ??
    body?.detail ??
    body?.title ??
    `API error: ${response.status} ${response.statusText}`;

  return new ApiError(message, "http", response.status, fieldErrors);
}

export async function handleResponse<T>(response: Response): Promise<T> {
  if (!response.ok) {
    throw await toApiError(response);
  }
  return response.json() as Promise<T>;
}
