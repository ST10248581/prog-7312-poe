/// <reference types="vite/client" />

interface ImportMetaEnv {
  /** Base URL of the Smart-X API, including the /api prefix. Set in .env.development. */
  readonly VITE_API_BASE_URL?: string;
  /** Milliseconds before a request is abandoned. Defaults to 10 000. */
  readonly VITE_API_TIMEOUT_MS?: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}
