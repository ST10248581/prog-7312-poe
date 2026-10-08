# prog-7312-poe - Smart-X IoT Mesh Ecosystem Dashboard

> Smart-X IoT Mesh Ecosystem dashboard for real-time telemetry monitoring, device status tracking, alerts, and interactive troubleshooting.

## Overview

The Smart-X IoT Mesh Ecosystem is an interactive dashboard that helps users monitor and manage a high-throughput IoT environment. The dashboard focuses on **real-time visual feedback** to make continuously changing IoT telemetry easier to understand. It allows users to monitor device status, identify abnormal sensor readings, receive alerts and investigate potential device or connectivity problems. The project is based on research into user engagement and dashboard interactivity for high-throughput IoT systems.

It is built as two projects: an **ASP.NET Core Web API on .NET 10** (`smart-x-backend/`) serving an in-memory telemetry store, and a **React 19 + TypeScript** single-page app built with **Vite** (`smart-x-front-end/`) that consumes it.

## Project Structure

```
prog-7312-poe/
├── smart-x-backend/              # ASP.NET Core Web API (.NET 10)
│   └── SmartX.Api/
│       ├── Controllers/          # Thin API controllers — no business logic
│       │   ├── AlertsController.cs
│       │   ├── CommandsController.cs   # Page 2 — command stream, overrides, intake, insights
│       │   ├── EngagementController.cs
│       │   ├── MeshController.cs       # Ingestion, load arithmetic, deployment
│       │   ├── SensorsController.cs
│       │   ├── TelemetryController.cs
│       │   └── TestController.cs       # Connectivity check
│       ├── Configuration/        # Typed options: Frontend (CORS origins), Attachments (limits, key)
│       ├── Logic/                # The two central service classes
│       │   ├── Attachments/      # AttachmentPolicy (file allow-list, magic bytes), AttachmentCipher (AES-GCM)
│       │   ├── ServiceResults.cs # WriteResult<T>: Success / NotFound / Conflict / Invalid
│       │   ├── ISmartXTelemetryEngine.cs
│       │   ├── ISensorService.cs / ITelemetryService.cs / IAlertService.cs / IEngagementService.cs
│       │   ├── SmartXTelemetryEngine.cs    # Page 1 — telemetry
│       │   ├── ISmartXCommandEngine.cs
│       │   ├── SmartXCommandEngine.cs      # Page 2 — command stream (Part 2 data structures)
│       │   ├── CommandDispatchSimulator.cs # 2 s timer that drives the command engine
│       │   └── CommandGenerator.cs         # Builds and advances simulated commands
│       ├── Data/                 # Repositories + in-memory data store (with indexes)
│       │   ├── Collections/      # RingBuffer<T> — the custom fixed-capacity collection
│       │   └── Seeding/          # Demo-data seeders (one per entity)
│       ├── Models/               # Entities, requests, responses
│       │   ├── Requests/         # CreateSensorRequest, IngestTelemetryRequest, ... (validated)
│       │   ├── Validation/       # SensorRules (shared regexes), MacAddress normalisation
│       │   ├── Responses/        # EcosystemSummary, SensorDetail, LoadComparison, ...
│       │   ├── Stream/           # StreamPacket, PipelineStatus, NodeTimeline, SuggestedAction, ...
│       │   └── Telemetry/        # TelemetryPacket<T>, SensorLoad, DeploymentNode
│       ├── Properties/
│       │   └── launchSettings.json     # http profile — port 5127
│       ├── appsettings.json      # Frontend:AllowedOrigins, Attachments:* settings
│       ├── Program.cs            # Entry point, DI registration, options, CORS, JSON options
│       └── SmartX.Api.csproj     # Project file (net10.0)
├── smart-x-front-end/            # React 19 + TypeScript (Vite)
│   ├── src/
│   │   ├── pages/                # Route-level pages
│   │   │   ├── TelemetryPage.tsx       # Page 1 — the telemetry dashboard
│   │   │   ├── HomePage.tsx            # Overview landing page (brand link)
│   │   │   ├── CommandsPage.tsx        # Page 2 — command stream and history
│   │   │   └── NotFoundPage.tsx        # 404 for unknown routes
│   │   ├── components/
│   │   │   ├── Navbar.tsx              # Module links with live badge counts
│   │   │   ├── RouteErrorBoundary.tsx  # Per-page crash containment
│   │   │   ├── toast/                  # Toast notifications (proactive suggestions)
│   │   │   ├── ApiStatusBanner.tsx     # Startup check, "API offline" banner, retry/back-off
│   │   │   ├── commands/         # Page 2 — CommandStream, OverrideConsole (undo),
│   │   │   │                     # SuggestedActions, IngestPipeline, NodeTimeline, ...
│   │   │   └── telemetry/        # Dashboard widgets — SensorCard, LiveChart,
│   │   │                         # AlertFeed, FilterBar, MeshInsights,
│   │   │                         # DeploymentTree, TroubleshootingGuide,
│   │   │                         # RegistrationFields (validated form), ...
│   │   ├── hooks/
│   │   │   ├── usePolling.ts           # Non-overlapping, cancellable, visibility-aware polling
│   │   │   └── useRegistrationForm.ts  # Form state, inline errors, server field errors
│   │   ├── state/                # AppStateProvider + usePersistentState: page state across navigation
│   │   ├── services/
│   │   │   ├── apiService.ts     # Typed API client + shared response types
│   │   │   ├── http.ts           # Base URL from env, timeout, retry, ApiError decoding
│   │   │   └── apiStatus.ts      # Shared online/offline state
│   │   ├── styles/               # Global styling system: tokens, base, responsive
│   │   ├── utils/format.ts       # Number/date formatting helpers
│   │   ├── utils/validation.ts   # Client mirror of the API's validation rules
│   │   ├── assets/               # Images and SVGs
│   │   ├── App.tsx               # Root component + react-router routes
│   │   └── main.tsx              # Entry point
│   ├── public/
│   ├── .env.development          # VITE_API_BASE_URL for `npm run dev`
│   ├── .env.example              # Template for .env.local / .env.production
│   ├── package.json
│   └── vite.config.ts
├── .gitattributes
├── .gitignore
└── README.md
```

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) — the project
  targets `net10.0`; an older SDK fails the build with `NETSDK1045`
- [Node.js](https://nodejs.org/) — `^20.19.0 || >=22.12.0`, as required by Vite 8
- npm (comes with Node.js)

## Getting Started

### Backend (ASP.NET Core Web API on .NET 10)

```bash
cd smart-x-backend/SmartX.Api
dotnet run
```

The API will start on **http://localhost:5127**.

### Frontend (React + TypeScript)

```bash
cd smart-x-front-end
npm install
npm run dev
```

The dev server will start on **http://localhost:5173**.

### Configuration

Neither side hard-codes the other's address; both read it from configuration.

| Setting | Where | Default | Purpose |
| --- | --- | --- | --- |
| `VITE_API_BASE_URL` | `smart-x-front-end/.env.development` | `http://localhost:5127/api` | Where the dashboard sends requests |
| `VITE_API_TIMEOUT_MS` | `.env.*` (optional) | `10000` | How long a request may take before it is abandoned |
| `Frontend:AllowedOrigins` | `SmartX.Api/appsettings.json` | `["http://localhost:5173"]` | CORS allow-list |
| `Attachments:MaxFileSizeBytes` | `appsettings.json` | `10485760` (10 MB) | Largest accepted upload; also sets the multipart limit |
| `Attachments:ChunkSizeBytes` | `appsettings.json` | `65536` | AES-GCM chunk size for streaming encryption |
| `Attachments:EncryptionKey` | user-secrets / env var only | generated per run | Base64 AES-256 key — never commit it |

`Properties/launchSettings.json` pins the API to port 5127. To move either side,
change `VITE_API_BASE_URL` and `Frontend:AllowedOrigins` — no code changes. Copy
`.env.example` to `.env.local` to override the frontend for one machine, and set
any backend value with an environment variable (`Frontend__AllowedOrigins__0`,
`Attachments__EncryptionKey`) or user-secrets:

```bash
cd smart-x-backend/SmartX.Api
dotnet user-secrets init
dotnet user-secrets set "Attachments:EncryptionKey" "$(openssl rand -base64 32)"
```

Without a key the API generates one at startup. Attachments live in the in-memory
store, so none outlive the key. Options are validated at startup, so a malformed
setting stops the API immediately instead of failing on the first request.

### Verify the Connection

1. Start the backend API.
2. Start the frontend dev server.
3. Open http://localhost:5173 — `/` redirects to `/telemetry`, and the dashboard
   loads live data from the API. Sensor tiles, the ecosystem summary and the alert
   feed populating is itself confirmation that the connection works.
4. To check the API on its own, call the connectivity endpoint directly:
   `GET http://localhost:5127/api/test`.

**If the API is not running**, the dashboard says so instead of failing silently.
`ApiStatusBanner` checks `/api/test` at startup. When the API is unreachable it
shows an **API offline** banner with the command to start the backend, a
countdown to the next automatic retry (3 s, doubling to 30 s) and a **Retry now**
button. Once the API answers, the banner reports **Reconnected** and both pages
reload their data. Every request also has a timeout (`VITE_API_TIMEOUT_MS`), and
reads are retried once after a network error or 5xx, so a slow or restarting API
never leaves the UI hanging.

### Routes

| Route | Page | State |
| --- | --- | --- |
| `/` | `HomePage` | Overview — mesh health, live counts, and where each module resumes |
| `/telemetry` | `TelemetryPage` | Implemented — the dashboard |
| `/commands` | `CommandsPage` | Implemented — command stream, manual overrides with undo, telemetry intake, Suggested Actions & Automated Insights |
| `*` | `NotFoundPage` | 404 with links back to each module |

## API Reference

All routes are prefixed with `/api`. Enums are serialised as names (for example
`"Warning"` rather than `1`), and `JsonNumberHandling.AllowNamedFloatingPointLiterals`
is enabled so a gateway can send a lost sample as the string `"NaN"`.

Every Page 1 action is `async Task<IActionResult>` and takes the request's
`CancellationToken`, so work stops when the browser abandons a request. Validation
failures return a `ValidationProblemDetails` body whose `errors` map names each
field, for example `{"errors": {"MacAddress": ["MAC address must be six hex pairs…"]}}`.
The dashboard shows each message under the input it belongs to.

| Method | Route | Purpose |
| --- | --- | --- |
| `GET` | `/api/test` | Connectivity check |
| `GET` | `/api/telemetry/summary` | Ecosystem summary tiles |
| `GET` | `/api/telemetry/series/{sensorProfileId}` | Time series for one sensor |
| `GET` | `/api/telemetry/readings` | Paged raw readings |
| `GET` | `/api/sensors` | Paged, filtered sensor list |
| `GET` | `/api/sensors/{id}` | Sensor detail |
| `GET` | `/api/sensors/filter-options` | Values available to the filter bar |
| `POST` | `/api/sensors` | Register a sensor — 201, 400 (field errors), 409 (duplicate MAC / node id) |
| `PUT` | `/api/sensors/{id}/payload` | Update the registration — 200, 400, 404, 409 |
| `POST` | `/api/sensors/{sensorId}/attachments` | Upload an attachment (multipart) — validated, hashed, AES-GCM encrypted |
| `GET` | `/api/sensors/{sensorId}/attachments/{attachmentId}/download` | Download — decrypted and SHA-256 verified |
| `GET` | `/api/alerts` | Alert feed |
| `GET` | `/api/engagement` | Engagement / configuration-progress state |
| `POST` | `/api/mesh/sensors/{sensorId}/ingest` | Gateway batch ingestion |
| `GET` | `/api/mesh/load` | Aggregate load of the selected meters |
| `GET` | `/api/mesh/load/zone/{zone}` | Aggregate load of one zone |
| `GET` | `/api/mesh/load/compare` | Delta between two meters |
| `GET` | `/api/mesh/deployment` | Validate the live deployment tree |
| `POST` | `/api/mesh/deployment/validate` | Validate a proposed configuration profile |
| `GET` | `/api/commands` | Paged, filtered command history |
| `GET` | `/api/commands/stream` | Live tail of the command stream |
| `GET` | `/api/commands/summary` | Dispatch health and throughput |
| `GET` | `/api/commands/filter-options` | Filter values, targetable nodes and their capabilities |
| `POST` | `/api/commands` | Queue a manual override |
| `GET` | `/api/commands/overrides` | The undo stack, most recent first |
| `POST` | `/api/commands/overrides/undo` | Undo the most recent override |
| `POST` | `/api/commands/packets` | Telemetry intake (standard queue / critical lane) |
| `GET` | `/api/commands/pipeline` | Queue depths, error states, disconnected nodes, recent alerts |
| `GET` | `/api/commands/nodes/{nodeId}/timeline` | A node's sorted log, ready to chart |
| `GET` | `/api/commands/devices` | Every device matching category / alert state / severity / zone / search, with its latest readings |
| `GET` | `/api/commands/insights` | Suggested Actions & Automated Insights |
| `POST` | `/api/commands/activity` | Record a search or node selection for the action engine |

## Key Features

| Feature | What it does | Where it lives |
| --- | --- | --- |
| **Real-time telemetry** | Continuously changing sensor data, polled on a 10-second refresh interval | `TelemetryPage.tsx` (`REFRESH_MS`), `LiveChart.tsx`, `Sparkline.tsx` |
| **Device status monitoring** | Shows devices as Online, Warning or Offline | `SensorCard.tsx`, `StatTile.tsx` |
| **Anomaly detection** | Flags readings outside the thresholds defined per reading type | `ReadingTypeProfile.cs`, `SmartXTelemetryEngine` |
| **Proactive alerts** | Alert feed for disconnections and abnormal telemetry | `AlertFeed.tsx` ← `GET /api/alerts` |
| **Filtering** | Narrows the view by device, sensor type, status or time period | `FilterBar.tsx` ← `GET /api/sensors/filter-options` |
| **Guided troubleshooting** | Structured next steps for investigating a device | `TroubleshootingGuide.tsx` |
| **Progressive disclosure** | Summary tiles and cards first; full diagnostics behind a detail modal | `StatTile.tsx` → `SensorCard.tsx` → `SensorDetailModal.tsx` → `SensorDetailPanel.tsx` |
| **Mesh insights** | Aggregate load and the validated deployment tree | `MeshInsights.tsx`, `DeploymentTree.tsx` ← `/api/mesh/*` |
| **Sensor registration** | Adds a sensor with inline, as-you-type validation (MAC, node id, name, room, zone and category dropdowns). Submit stays disabled until the form is valid, and server-side 400/409 errors appear under the offending field | `RegisterSensorModal.tsx`, `RegistrationFields.tsx`, `utils/validation.ts` ← `POST /api/sensors` |
| **Secure attachments** | Drag-and-drop upload with a progress bar and cancel. Type, size and magic bytes are checked on both sides; files are streamed through SHA-256 and AES-256-GCM in 64 KB chunks and verified on download | `SensorDetailPanel.tsx` ← `AttachmentPolicy.cs`, `AttachmentCipher.cs` |
| **Startup gateway** | Detects an offline API, explains how to start it, retries with back-off, and reloads the pages on reconnect | `ApiStatusBanner.tsx`, `services/http.ts`, `services/apiStatus.ts` |
| **Configuration progress** | Engagement state showing how completely the mesh is configured | `ConfigurationProgress.tsx` ← `GET /api/engagement` |

### Page 2 — Real-Time Command Stream and History

| Feature | What it does | Where it lives |
| --- | --- | --- |
| **Suggested Actions & Automated Insights** | Predictive recommendations learned from operator searches, selections and overrides, plus devices flagged as likely faulty — shown before the operator searches for them | `SuggestedActions.tsx` ← `GET /api/commands/insights`, `POST /api/commands/activity` |
| **Live command stream** | Newest commands first; selecting a row targets the override console at that node | `CommandStream.tsx` ← `GET /api/commands/stream` |
| **Command history** | The paged audit trail behind the stream, using the same filter | `CommandHistoryTable.tsx` ← `GET /api/commands` |
| **Filtering and search** | Narrows the stream, history and devices by status, origin, category, alert state, zone or free text | `CommandFilterBar.tsx` ← `GET /api/commands/filter-options` |
| **Manual overrides with undo** | Queues a command against a node; undo reverses the most recent override | `OverrideConsole.tsx` ← `POST /api/commands`, `/api/commands/overrides/*` |
| **Telemetry intake** | Routine FIFO lane and critical priority lane, with duplicate-alert suppression | `IngestPipeline.tsx` ← `GET /api/commands/pipeline` |
| **Live devices** | Every matching device with its latest readings, worst first | `LiveDevicePanel.tsx` ← `GET /api/commands/devices` |
| **Node timeline** | A node's readings, limits, commands and alerts over the last hour | `NodeTimeline.tsx` ← `GET /api/commands/nodes/{nodeId}/timeline` |
| **Throughput** | Dispatch rate per minute over the selected window | `ThroughputStrip.tsx` ← `GET /api/commands/summary` |

### Data storage

There is no database. `SmartXDataStore` is an in-memory singleton, populated at
startup by `IDataSeeder.SeedAll()` (`Program.cs`) from the seeders in
`Data/Seeding/`. Everything written at runtime — ingested batches, registered
sensors, uploaded attachments — lives only for the lifetime of the process and is
reset on restart.

The lists act as tables. Beside them the store keeps indexes, so the hot paths
never scan a whole table:

| Index | Type | Answers |
| --- | --- | --- |
| Sensor by id / MAC / node id | `Dictionary<…, SensorProfile>` | O(1) lookups, and duplicate detection at registration |
| Readings by sensor | `Dictionary<Guid, List<TelemetryReading>>` | A sensor's series without touching other sensors' readings |
| Recent readings per sensor and type | `RingBuffer<TelemetryReading>` (64) | Sparklines and latest values |
| Recent ingest batches per sensor | `RingBuffer<IngestionBatch>` (10) | The detail view's ingestion history |

`RingBuffer<T>` (`Data/Collections/RingBuffer.cs`) is the custom collection. It is a
fixed-capacity circular buffer implementing `IReadOnlyList<T>`:

* **O(1) append, no allocation.** When full, the oldest item is overwritten in place.
* **O(1) access.** Indexer, `Latest` and `Oldest` are constant time.
* **Cheap tails.** `TakeLatest(n)` and `NewestFirst()` cost O(n), however long the sensor has been reporting.
* **Safe enumeration.** A version check rejects modification during enumeration, as the BCL collections do.

Ingestion writes through it (`TelemetryRepository.AppendReadings` →
`AddReading` / `AddIngestionBatch`), and the sensor grid and detail view read
from it. Building the sensor grid previously scanned all ~68 000 readings once per
sensor; it now reads each sensor's own index and a 64-slot window.

## Proposed Dashboard

The dashboard follows the principle of:

> **Overview → Filter → Investigate → Troubleshoot**

Users can first view the overall health of the IoT ecosystem before filtering to a specific device or sensor and accessing detailed telemetry and diagnostic information.

## Architecture — Centralised Application Logic

The API is layered `Controllers → Logic → Data`. All application logic is centralised
in a single service class:

**`smart-x-backend/SmartX.Api/Logic/SmartXTelemetryEngine.cs`**

This class replaced the four thin pass-through services (`SensorService`,
`TelemetryService`, `AlertService`, `EngagementService`) that previously did little
more than forward calls to a repository. It implements
`ISmartXTelemetryEngine`, which inherits the four original domain interfaces, so the
controllers still depend on narrow contracts while one class owns the behaviour:

```csharp
public interface ISmartXTelemetryEngine
    : ISensorService, ITelemetryService, IAlertService, IEngagementService
```

`Program.cs` registers the single instance behind every interface:

```csharp
builder.Services.AddScoped<SmartXTelemetryEngine>();
builder.Services.AddScoped<ISensorService>(p => p.GetRequiredService<SmartXTelemetryEngine>());
// ...ITelemetryService, IAlertService, IEngagementService, ISmartXTelemetryEngine
```

The one exception is `ITestService` / `TestService`, which backs the `/api/test`
connectivity endpoint. It is registered separately and holds no domain logic, so it
is deliberately left outside the engine.

Centralising the logic matters because the operations that were missing — batch
ingestion, aggregate load and deployment validation — each span more than one
repository. Spreading them across four domain services would have meant cross-service calls
or duplicated rules.

## Technical and Language Requirements

The four required advanced object-oriented C# concepts are used to do real work in
`SmartXTelemetryEngine`, not as isolated demonstrations.

### 1. Generics — `TelemetryPacket<T>`

**Supporting type:** `Models/Telemetry/TelemetryPacket.cs`
**Used in:** `SmartXTelemetryEngine.IngestHistoricalBatches` / `CreatePacket`

A reusable generic wrapper handles disparate incoming data structures uniformly. The
type parameter is constrained to `struct` and stored in a strongly typed field, so the
payload is never boxed onto the heap:

```csharp
public sealed class TelemetryPacket<T> : ITelemetryPacket where T : struct
{
    public T Value { get; }   // a T field — not an object field
}
```

The mesh is deliberately mixed hardware, and the engine closes a different generic
instantiation per encoding — exactly the disparate cases the requirement describes:

| Metric | Payload | Instantiation |
| --- | --- | --- |
| Temperature, Humidity, Pressure, Vibration | 32-bit float from low-power nodes | `TelemetryPacket<float>` |
| Power | fractional kW | `TelemetryPacket<double>` |
| Whole-unit counters | integer | `TelemetryPacket<int>` |
| Smart-switch trigger | single bit | `TelemetryPacket<bool>` |

```csharp
private static ITelemetryPacket CreatePacket(..., TelemetryPayloadKind payloadKind, ...)
    => payloadKind switch
    {
        TelemetryPayloadKind.Bool   => TelemetryPacket.Create(..., raw >= 0.5d, ...),
        TelemetryPayloadKind.Int    => TelemetryPacket.Create(..., (int)Math.Round(raw), ...),
        TelemetryPayloadKind.Double => TelemetryPacket.Create(..., raw, ...),
        _                           => TelemetryPacket.Create(..., (float)raw, ...)
    };
```

**Avoiding boxing.** Two specific decisions keep the payload off the heap, which is the
point of the requirement for resource-constrained gateways:

* `TelemetryPacket<T>` is a **reference type holding a value-type field**. Packets of
  different `T` can therefore share one `List<ITelemetryPacket>` — the interface
  reference points at the wrapper, so the `T` inside is never boxed.
* Reading the payload back uses JIT-folded type tests rather than `Convert`/casting
  through `object`:

  ```csharp
  public bool TryGetNumeric(out double numeric)
  {
      var value = Value;
      if (typeof(T) == typeof(float)) { numeric = Unsafe.As<T, float>(ref value); return true; }
      // ...
  }
  ```

  The JIT compiles a separate body per value-type instantiation, so each `typeof(T) ==`
  test folds to a constant and the branches disappear. `Convert.ToDouble((object)Value)`
  would have allocated once per sample — tens of thousands of allocations per batch flush.

### 2. Operator Overloading — `SensorLoad`

**Supporting type:** `Models/Telemetry/SensorLoad.cs`
**Used in:** `SmartXTelemetryEngine.GetAggregateLoad`, `GetZoneLoad`, `CompareLoad`

`SensorLoad` is a `readonly struct` carrying a value, its unit and the number of meters
folded into it. Aggregating meters is expressed as arithmetic rather than as a helper
method, so `Meter3 = Meter1 + Meter2` reads literally:

```csharp
public static SensorLoad operator +(SensorLoad left, SensorLoad right)  // aggregate load
public static SensorLoad operator -(SensorLoad left, SensorLoad right)  // delta comparison
public static SensorLoad operator -(SensorLoad load)                    // negation
public static SensorLoad operator *(SensorLoad load, double factor)     // scaling
public static bool operator >(...), <(...), >=(...), <=(...), ==(...), !=(...)
public static explicit operator double(SensorLoad load)
```

The engine aggregates a zone by folding with `+`, and compares two smart meters with
`-` and the relational operators:

```csharp
var total = SensorLoad.Zero;
foreach (var id in sensorProfileIds) { total += load; }   // operator +

var delta          = leftLoad - rightLoad;   // operator -
var leftIsHeavier  = leftLoad > rightLoad;   // operator >
var areEquivalent  = leftLoad == rightLoad;  // operator ==
```

The unit travels with the value and the operators refuse to combine mismatched units —
adding kW to mm/s throws `InvalidOperationException` rather than silently producing a
meaningless number. `SensorLoad.Zero` acts as the additive identity so the fold starts
cleanly and adopts the unit of the first real meter. `Equals`, `GetHashCode`,
`IEquatable<T>` and `IComparable<T>` are implemented alongside the operators so equality
and ordering stay consistent.

### 3. Advanced Arrays and Lists — the ingestion pipeline

**Used in:** `SmartXTelemetryEngine.IngestHistoricalBatches` / `ProjectStatistics`

A gateway flushes its buffer as several sequential historical batches. Batches are
ragged by nature — a gateway sends whatever it captured between flushes — so the raw
telemetry arrives as a **jagged array**, which preserves the batch boundaries and costs
nothing for rows of differing length:

```csharp
double[][] rawBatches = request.Batches;   // one ragged row per batch
```

Reduction happens in a **rectangular (multi-dimensional) array**: one row per batch, one
column per statistic. Fixed shape and contiguous storage mean the whole reduction
allocates once instead of once per batch:

```csharp
var statistics = new double[rawBatches.Length, StatColumnCount];
// columns: Samples | Accepted | Rejected | Anomalies | Min | Max | Sum
statistics[batchIndex, StatAccepted]++;
statistics[batchIndex, StatSum] += raw;
```

Only once every sample has been classified is the data **transferred into optimised
`List<T>` collections**, each pre-sized so the list never has to grow and re-copy:

```csharp
var packets  = new List<ITelemetryPacket>(totalSamples);
var readings = new List<TelemetryReading>(packets.Count);
foreach (var packet in packets) { readings.Add(packet.ToReading()); }
```

`ProjectStatistics` then lifts the rectangular matrix into a `List<BatchStatistic>` for
the API response — multi-dimensional arrays cannot be serialised to JSON, which is
another reason the array form stays internal to the computation.

Lost samples are kept meaningful: a gateway that drops a reading sends `NaN` rather than
omitting the slot, so sequence positions stay aligned. The engine counts those as
rejected, and flags values outside the reading type thresholds as anomalies.

### 4. Recursion — deployment tree validation

**Supporting types:** `Models/Telemetry/DeploymentNode.cs`
**Used in:** `SmartXTelemetryEngine.ValidateDeployment` / `ValidateNode` / `ValidateLeaf`

The mesh is deployed as a nested hierarchy — `Facility A -> Zone 1 -> Sub-Zone B -> NODE-07`
— and `DeploymentNode` is a recursive structure: each node holds a list of nodes. Because
the depth is not known in advance, validation is a method that calls itself once per
child rather than a fixed ladder of loops:

```csharp
private static void ValidateNode(DeploymentNode node, DeploymentTier expectedTier,
    string? parentPath, int depth, DeploymentValidationReport report,
    HashSet<DeploymentNode> ancestors)
{
    // ...checks for this node...
    foreach (var child in node.Children)
    {
        ValidateNode(child, node.Tier + 1, path, depth + 1, report, ancestors);
    }
}
```

Each call passes the child the tier it is *required* to occupy (`node.Tier + 1`), which is
how the walk verifies that a node is safely configured within `Sub-Zone B -> Zone 1 ->
Facility A` rather than attached at the wrong level. The accumulated `path` gives every
issue and every valid sensor a fully qualified location.

Rules enforced on the way down:

| Issue | Severity |
| --- | --- |
| `TierOutOfOrder` — child is not exactly one tier below its parent, or a leaf has children | Critical |
| `OrphanedSensor` — a Node tier not bound to a registered sensor profile | Critical |
| `DepthExceeded` — nesting beyond the 8-tier limit | Critical |
| `CircularReference` — a profile referencing one of its own ancestors | Critical |
| `DuplicateSibling` — two siblings share a name, making the path ambiguous | Warning |
| `EmptyBranch` — a zone or sub-zone with no child deployments | Warning |
| `UnnamedNode` / `UnreachableNode` | Warning |

**Termination.** Two base cases stop the recursion on malformed input rather than
overflowing the stack: a depth guard (`MaxDeploymentDepth`), and an `ancestors` set of
the nodes on the current path, which detects a profile that loops back on itself. Leaf
nodes return without recursing.

Two entry points use the same walk: `ValidateDeployment(zone)` projects the live sensor
register into a tree and validates what is actually deployed, while
`ValidateDeployment(DeploymentNode root)` validates a proposed configuration profile
before it is rolled out.

### Endpoints exercising this logic

`Controllers/MeshController.cs` exposes the mesh-level operations:

| Method | Route | Concept exercised |
| --- | --- | --- |
| `POST` | `/api/mesh/sensors/{sensorId}/ingest` | Generics + jagged/multi-dimensional arrays |
| `GET` | `/api/mesh/load?sensorIds=...` | Operator overloading (`+`) |
| `GET` | `/api/mesh/load/zone/{zone}` | Operator overloading (`+`) |
| `GET` | `/api/mesh/load/compare?left=&right=` | Operator overloading (`-`, `>`, `==`) |
| `GET` | `/api/mesh/deployment?zone=` | Recursion |
| `POST` | `/api/mesh/deployment/validate` | Recursion |

Example ingest body — three sequential batches of differing length, with one lost sample:

```json
{
  "readingType": "Power",
  "intervalSeconds": 300,
  "batches": [[1.2, 1.4, 1.9], [2.1, "NaN", 9.9, 1.7], [1.1]]
}
```

## Part 2 — Command Stream Engine and Data Structures

All logic for the **Real-Time Command Stream and History** page lives in one service:

**`smart-x-backend/SmartX.Api/Logic/SmartXCommandEngine.cs`**

It replaces the `CommandService`, `CommandRepository` and the tick logic that used to
live in `CommandDispatchSimulator`. The simulator is now only a 2-second timer that
calls `RunDispatchCycle()`.

Telemetry is **not** generated inside the engine's tick. A second hosted service,
`DeviceTelemetrySimulator`, emulates the ESP32 nodes, smart plugs and gateways: every
2 seconds it sends one HTTP `POST /api/commands/packets` per reporting device
(identified by its MAC address), plus gateway link-loss reports and occasional buffer
flushes. Simulated data therefore reaches the API exactly as a real device's would —
model binding, MAC lookup, lane classification and the critical lane all run on every
packet. Set `DeviceSimulator:Enabled` to `false` to switch it off, or
`DeviceSimulator:BaseUrl` to point it at another host.

The engine is registered as a **singleton**, because its
queues, undo stack, registry, logs and sets are live state shared between requests
and the dispatch loop. All of that state sits behind one lock (the store's
`CommandsSyncRoot`), so the command log and the structures built around it are never
seen half-updated.

### Stacks, queues and priority queues

| Requirement | Structure | Where | What it does |
| --- | --- | --- | --- |
| Message queue | `Queue<StreamPacket> _standardLane` | `Enqueue`, `DrainStandardLane` | Routine telemetry packets are processed first in, first out, at a budget of 20 per tick. A gateway burst backs the queue up and it drains over the next ticks. **Backpressure:** the queue holds at most 1 000 packets; past that the oldest are shed and counted (`Dropped`), so a flood cannot grow memory or make every later packet wait longer. The critical lane is never shed. |
| Priority queue | `PriorityQueue<StreamPacket, (int Rank, long Ticks)> _criticalLane` | `Classify`, `Enqueue`, `DrainCriticalLane` | A packet is classified before it is queued. A severe power spike, a moisture crash (≥ 25 % of the span past the limit, or a threshold marked critical) or a lost link goes to the priority queue, which is drained **immediately**: in the same request for posted packets, and ahead of the FIFO on every tick. The worst breach is dequeued first, with ties in arrival order. Each critical packet records how many queued standard packets it overtook (`BypassedStandard`). |
| Stack (undo) | `Stack<OverrideHistoryEntry> _overrideHistory` | `Dispatch`, `UndoLastOverride` | Every live manual override is pushed with a revert plan worked out at issue time (the value it replaced). Undo pops the top: a queued command is cancelled; a sent one gets its inverse at Immediate priority (for example, "shut down valves" becomes "open them again", and a threshold or firmware change is restored to the previous value). A restart or a sample request is reported as irreversible. |
| Stack (redo) | `Stack<OverrideHistoryEntry> _redoHistory` | `UndoLastOverride`, `RedoLastUndo`, `Dispatch` | A cancelled or reverted override is pushed here. Redo pops it, re-sends the same command, and pushes it back onto the undo stack with a fresh revert plan. A new manual override clears the redo stack, as in any editor. |
| Idempotency | `ExpectedCommandId` on both requests | `UndoLastOverride`, `RedoLastUndo` | The console sends the id of the entry it shows on top. If that entry was already undone (a double click, or a retried request), the API answers `AlreadyUndone` and changes nothing, instead of undoing the next override down. The buttons are disabled while a request is out, and when their stack is empty. |

### Hash tables, dictionaries and sorted dictionaries

| Requirement | Structure | Where | What it does |
| --- | --- | --- | --- |
| Dictionary | `Dictionary<string, SensorProfile>` keyed by **node id** and by **MAC address**, plus one by profile id | `ResolveDevice`, `Dispatch`, `Process`, `RefreshRegistry`, `LookupDevice` | The live device registry. Every incoming packet and every dispatch resolves its device in O(1). MAC keys are stored in one canonical form (`AA:BB:CC:DD:EE:FF`), so `aa-bb-…` and `AA:BB:…` reach the same entry and can never create duplicates. The registry rebuilds itself when page 1 registers a new sensor. **Instant lookup** on the live device panel calls `GET /api/commands/devices/lookup?key=`, which probes these dictionaries directly and reports the time taken (typically a few microseconds, whatever the registry size). |
| Sorted list (a sorted dictionary) | `SortedList<DateTime, SensorLogEntry>` per node | `AppendLog`, `LowerBound`, `GetNodeTimeline` | Each node's historical log (readings, commands, alerts, link changes) keyed by timestamp. Late packets are slotted into place on insert, so the timeline reads out already in order. A `SortedList` rather than a `SortedDictionary`: both keep their keys sorted, but `SortedList` stores them in an array, so `LowerBound` can **binary-search** to the first entry in a time window and read forward from there. A window read costs O(log n + k) instead of a walk of the whole log. Readings arrive almost entirely in time order, so inserts land at the end and stay cheap. The timeline shows how many entries the range read skipped, how many it read, and the time taken; its 5 min / 15 min / 1 h / 3 h buttons change the window. |
| Dictionary + bounded queue | `Dictionary<(string NodeId, ReadingType), Queue<TimelinePoint>> _recentReadings` | `TrackRecent`, `GetLiveDevices` | The last 24 values per node and metric. The live device panel reads every device's latest readings and sparkline with one hash probe each, rather than walking each node's full sorted log. |

### Sets

| Requirement | Structure | Where | What it does |
| --- | --- | --- | --- |
| Hash set — disconnected nodes | `HashSet<string> _disconnectedNodes` | `Enqueue`, `Process` | A repeated "link lost" packet for a node already in the set is dropped at intake with one hash probe. The node is alerted once, not on every tick of the outage, and its chip shows how many repeats were suppressed ("×12 suppressed"). |
| Hash set — error states | `HashSet<ErrorStateKey> _activeErrorStates` (a `record struct` of node, alert type and metric) | `Process` | `Add` returning false means the breach is a repeat, so the reading is logged but no second alert is raised. The state clears when the value returns in range. An escalation from Warning to Critical still alerts. Each open state counts the repeat warnings it absorbed, shown on its chip. |
| Set algebra | `ExceptWith`, `IntersectWith`, `UnionWith` | `CompareDisconnected` | The page sends the disconnected set it saw on its last poll (`GET /api/commands/pipeline?known=…`). The API returns **newly disconnected** = current ∖ known, **recovered** = known ∖ current, **still down** = current ∩ known, and **needs attention** = disconnected ∪ nodes with a critical breach. Nothing has to be stored per client on the server. |

The **Telemetry intake** panel on page 2 shows:

* both lane depths, with the standard lane's fill against its capacity;
* average waits, and how many queued packets the critical lane has bypassed;
* the duplicate-suppression and backpressure counters;
* the two sets with per-entry repeat counts, and what changed since the last refresh;
* the recent pipeline alerts.

**Simulate power spike** posts 20 routine packets followed by one spike, so you can
watch the spike get processed before the routine packets queued ahead of it.
**Flood standard lane** posts more routine packets than the queue holds, so the
oldest are shed and counted.

`ErrorStateKey` is a `readonly record struct`. The compiler generates value-based
`Equals` and `GetHashCode` over its three fields (node, alert type, metric), which is
what makes it safe and fast as a `HashSet` key.

### Predictive Action and Recommendation Engine — Suggested Actions & Automated Insights

`GetInsights` merges three sources and keeps the best eight with a bounded min-heap
(`PriorityQueue<SuggestedAction, double>`):

1. **Pattern analysis: association rules** (`RegisterTrigger`, `RecordAction`,
   `AddRuleSuggestions`). Every condition that starts (a breach entering the
   error-state set, or a node dropping off) opens a 10-minute association window.
   Every operator action (a search, a node selection or a manual override) is
   credited to the conditions still in the window, both for the specific node
   (*"after ENV-001's humidity drops below its limit, operators searched for
   'actuator node 03'"*) and as a general rule (*"after any node's power spikes,
   operators request a sample from it"*). A rule fires when a matching condition is
   active now, with support ≥ 3 and confidence ≥ 40 % [20]. Conditions that fired
   while no operator was present don't count against a rule.
2. **Algorithmic suggestion: next step** (`AddNextStepSuggestions`). A first-order
   Markov chain over each operator's action sequence. After your last action, it
   suggests what usually follows it, with P(next | last) ≥ 30 % [21].
3. **Problem devices** (`AddProblemDeviceSuggestions`). Each node is scored on open
   breaches, out-of-range readings, failed or expired commands, link flapping and
   drift (the z-score of the latest reading against a running mean and variance,
   computed in one pass with Welford's method [22]). High scorers are flagged with
   the command most likely to help, for example recalibrating a drifting node or
   rolling a failing beta firmware back to stable.

The engine starts with two days of seeded operator habits. The flaky nodes the
simulator keeps breaching reproduce those habits, so the rules fire on real
conditions from the start. Every search, node selection and override on the page
feeds back in.

**Presentation.** The **Suggested Actions & Automated Insights** panel
(`SuggestedActions.tsx`) sits at the top of the `/commands` page, above the filter,
so recommendations are visible before the operator searches. Each card shows:

* its type: **Predicted** (a learned rule), **Next step** (the Markov chain)
  or **Problem device** (anomaly scoring);
* the reason in plain words, the confidence, and evidence chips such as
  "3 failed commands" or "Humidity drifting (z = 3.1)";
* buttons to act on it:
  * **Apply** does it in one click: sends the command, runs the search, applies the
    filter or inspects the node. A sent command lands on the undo stack, and the
    toast offers **Undo**.
  * **Edit first** puts the command in the override console instead.
  * **Dismiss** hides the card.

**Feedback re-ranks suggestions.** Apply and Dismiss are posted to
`POST /api/commands/insights/feedback`. A dismissal multiplies that suggestion's score
by 0.4 per dismissal for 30 minutes. Each application lifts it by 15 % (up to four
times). Both show as evidence chips on the card.

**Query history feeds the engine.** The page records every search, every filter value
switched on (`zone:Zone B`, `alertState:Active`, …) and every node inspected
(`POST /api/commands/activity`). Overrides, undos and redos are recorded by the API
as they happen. Learned filters come back as suggestions ("Filter to Zone B"). The
history lives in the engine for the API's lifetime, so it persists across page
changes and reloads.

**Proactive alerts.** The first time a Predicted or Next-step suggestion reaches 75 %
confidence, a toast announces it with **Apply** and **Dismiss** buttons, wherever the
operator is on the page.

**What the engine has learned.** Under the suggestions, the learning panel shows:

* rules learned and next-step links;
* applied versus dismissed, with the acceptance rate;
* the five strongest rules, with confidence and count/support;
* the operator's recent activity.

**Reset learning** (`POST /api/commands/insights/reset`) empties all of it, so the
engine can be shown picking up a habit from nothing.

The list can be empty for the first few seconds after the API starts, until the
simulator raises the first breach or disconnection.

### Navigation and preserved state

* **Overview at `/`.** The brand link opens a landing page with mesh health, alerts,
  commands in flight and "needs attention". It has a card per module that says where
  that module will resume ("Resume on ENV-001 with filters →").
* **Live nav badges.** Active alerts on the telemetry link and commands in flight on
  the command-stream link, refreshed every 15 seconds.
* **State survives navigation.** `AppStateProvider` (`src/state/`) holds each page's
  state above the router, and `usePersistentState` replaces `useState` for anything
  that should outlive the page:
  * filters, page number, live toggle, target node, timeline window, the open sensor
    and dismissed suggestions;
  * the last data fetched, so a page you return to renders at once and refreshes in
    place.

  The operator's choices are also mirrored to `sessionStorage`, so a reload keeps
  them too.
* **No UI freezing.** `usePolling` (`src/hooks/usePolling.ts`) replaces
  `setInterval`:
  * it schedules the next poll only after the current one settles, so requests never
    overlap;
  * it aborts the request in flight when filters change or the page unmounts
    (`AbortController`);
  * it pauses while the browser tab is hidden and refreshes immediately on return.
* **Crash containment.** Each route is wrapped in `RouteErrorBoundary`. A rendering
  error shows a retry panel for that page only, and unknown URLs show a 404 page.

### Tests

`smart-x-backend/SmartX.Api.Tests` is an xUnit project (14 tests) that runs the real
engine over a freshly seeded store:

```bash
cd smart-x-backend/SmartX.Api.Tests
dotnet test
```

* **Queues.** A critical packet is processed while 10 earlier standard packets are
  still queued. The largest breach is served before a lost link that arrived first.
  The standard queue sheds exactly the overflow past its capacity.
* **Sets.** Repeat disconnects are suppressed and counted. The set difference reports
  a newly disconnected node. MAC lookup works in any notation.
* **Undo/redo.** Undo cancels a queued override and moves it to the redo stack. Undo
  restores the previous setting. Undo and redo are idempotent. A new override clears
  redo.
* **RingBuffer.** Overwrite order, tail reads, and the modification check.

## Code Attributions and Reference List

The advanced object-oriented C# concepts (Part 1) and the data structures and
recommendation engine (Part 2) were implemented with reference to the sources
listed below. Each source is also cited in a `// Code Attribution [n]` comment block
(author, year, title, type, URL, access date, modifications and full reference) at
the top of the file(s) where the technique is used, using the same reference number
as this list. Section comments inside those files point back to the same numbers.

### Reference list

**Generics — `TelemetryPacket<T>`**

1. Microsoft, 2026. Generic types and methods – C# [Source code] Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/types/generics> [Accessed 13 September 2026].
2. Microsoft, 2025. Constraints on type parameters – C# [Source code] Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/generics/constraints-on-type-parameters> [Accessed 13 September 2026].
3. Microsoft, 2025. Boxing and Unboxing – C# [Source code] Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/types/boxing-and-unboxing> [Accessed 13 September 2026].
4. Microsoft, 2025. Unsafe.As Method (System.Runtime.CompilerServices) [Source code] Available at: <https://learn.microsoft.com/en-us/dotnet/api/system.runtime.compilerservices.unsafe.as> [Accessed 13 September 2026].

**Operator Overloading — `SensorLoad`**

5. Microsoft, 2026. Operator overloading – Define unary, arithmetic, equality, and comparison operators – C# reference [Source code] Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/operator-overloading> [Accessed 13 September 2026].
6. Microsoft, 2008. Operator Overloads – Framework Design Guidelines [Source code] Available at: <https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/operator-overloads> [Accessed 13 September 2026].
7. Microsoft, 2025. IEquatable\<T\> Interface [Source code] Available at: <https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1> [Accessed 13 September 2026].
8. Microsoft, 2026. Structure types – C# reference [Source code] Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/struct> [Accessed 13 September 2026].

**Advanced Arrays and Lists — the ingestion pipeline**

9. Microsoft, 2026. The array reference type – C# reference [Source code] Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/arrays> [Accessed 13 September 2026].
10. Microsoft, 2025. Array.GetLength(Int32) Method [Source code] Available at: <https://learn.microsoft.com/en-us/dotnet/api/system.array.getlength> [Accessed 13 September 2026].
11. Microsoft, 2025. List\<T\> Constructors [Source code] Available at: <https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.list-1.-ctor> [Accessed 13 September 2026].

**Recursion — deployment tree validation**

12. Microsoft, 2021. Iterate Through All Nodes of TreeView Control – Windows Forms [Source code] Available at: <https://learn.microsoft.com/en-us/dotnet/desktop/winforms/controls/how-to-iterate-through-all-nodes-of-a-windows-forms-treeview-control> [Accessed 13 September 2026].
13. Microsoft, 2025. ReferenceEqualityComparer Class [Source code] Available at: <https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.referenceequalitycomparer> [Accessed 13 September 2026].

**Part 2 — Stacks, queues and priority queues**

14. Microsoft, 2025. Queue\<T\> Class (System.Collections.Generic) [Source code] Available at: <https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.queue-1> [Accessed 29 September 2026].
15. Microsoft, 2025. PriorityQueue\<TElement,TPriority\> Class [Source code] Available at: <https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.priorityqueue-2> [Accessed 29 September 2026].
16. Microsoft, 2025. Stack\<T\> Class (System.Collections.Generic) [Source code] Available at: <https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.stack-1> [Accessed 29 September 2026].

**Part 2 — Hash tables, dictionaries and sorted lists**

17. Microsoft, 2025. Dictionary\<TKey,TValue\> Class [Source code] Available at: <https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.dictionary-2> [Accessed 29 September 2026].
18. Microsoft, 2025. SortedList\<TKey,TValue\> Class (System.Collections.Generic) [Source code] Available at: <https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.sortedlist-2> [Accessed 8 October 2026].

**Part 2 — Sets**

19. Microsoft, 2025. HashSet\<T\> Class (System.Collections.Generic) [Source code] Available at: <https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.hashset-1> [Accessed 29 September 2026].

**Part 2 — Predictive action and recommendation engine**

20. Agrawal, R., Imieliński, T. and Swami, A., 1993. Mining association rules between sets of items in large databases [Source code] Available at: <https://doi.org/10.1145/170035.170072> [Accessed 29 September 2026].
21. Jurafsky, D. and Martin, J.H., 2025. Speech and Language Processing, Chapter 3: N-gram Language Models (3rd edition draft) [Source code] Available at: <https://web.stanford.edu/~jurafsky/slp3/> [Accessed 29 September 2026].
22. Welford, B.P., 1962. Note on a method for calculating corrected sums of squares and products [Source code] Available at: <https://doi.org/10.1080/00401706.1962.10490022> [Accessed 29 September 2026].

**Sensor attachments — encryption at rest**

23. Microsoft, 2025. AesGcm Class (System.Security.Cryptography) [Source code] Available at: <https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.aesgcm> [Accessed 8 October 2026].
24. Microsoft, 2025. IncrementalHash Class (System.Security.Cryptography) [Source code] Available at: <https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.incrementalhash> [Accessed 8 October 2026].
25. Hoang, V.T., Reyhanitabar, R., Rogaway, P. and Vizár, D., 2015. Online Authenticated-Encryption and its Nonce-Reuse Misuse-Resistance [Source code] Available at: <https://eprint.iacr.org/2015/189> [Accessed 8 October 2026].

### Where each reference is cited in the code

All paths are relative to `smart-x-backend/SmartX.Api/`.

| File | References cited | What the reference supports |
| --- | --- | --- |
| `Models/Telemetry/TelemetryPacket.cs` | 1, 2, 3, 4 | Generic class declaration, the `where T : struct` constraint, why the payload is held as `T` rather than `object`, and the `Unsafe.As<TFrom,TTo>` reinterpret-cast used by `TryGetNumeric` / `TryGetBoolean` |
| `Models/Telemetry/SensorLoad.cs` | 5, 6, 7, 8 | `public static` operator declarations, overloading comparison operators in pairs, the explicit (lossy) conversion to `double`, `Equals`/`GetHashCode`/`IEquatable<T>`/`IComparable<T>` consistency, and the `readonly struct` declaration |
| `Models/Requests/IngestTelemetryRequest.cs` | 9 | The `double[][]` jagged array carrying ragged gateway batches |
| `Models/Telemetry/DeploymentNode.cs` | 12 | The self-referencing node shape (a node holding a list of nodes) that makes the validation walk recursive |
| `Logic/SmartXTelemetryEngine.cs` | 1, 3, 5, 6, 9, 10, 11, 12, 13 | Header block lists all; section comments cite 9/10/11 (+1, 3) on `IngestHistoricalBatches` and `ProjectStatistics`, 5/6 on `GetAggregateLoad` / `CompareLoad`, and 12/13 on `ValidateDeployment` / `ValidateNode` |
| `Logic/SmartXCommandEngine.cs` | 14–22 | Header block lists all; field comments and `Code attribution:` section comments cite 14/15 on the two intake lanes, 16 on the undo stack, 17 on the registry dictionaries, 18 on the sorted sensor logs, 19 on the disconnected-node and error-state sets, 20 on `AddRuleSuggestions`, 21 on `AddNextStepSuggestions`, and 22 on `RunningStats` / `AddProblemDeviceSuggestions` |
| `Models/Stream/StreamPacket.cs` | 14, 15 | The element type of the FIFO standard lane and the critical priority queue, and the `PacketLane` routing flag |
| `Models/Stream/OverrideHistoryEntry.cs` | 16 | The element pushed onto the undo and redo stacks, with its revert plan and undo/redo outcomes |
| `Models/Stream/SensorLogEntry.cs` | 18 | The value stored in each node's timestamp-keyed `SortedList` |
| `Models/Stream/SuggestedAction.cs` | 20, 21, 22 | The suggestion kinds produced by the association-rule, next-step and problem-device engines |
| `Logic/Attachments/AttachmentCipher.cs` | 23, 24, 25 | AES-GCM chunk sealing, the incremental SHA-256 hash, and the STREAM-style nonce and authenticated-data framing |

## Research Focus

The project investigates how different user engagement strategies can improve the usability of high-throughput IoT dashboards.

The following strategies were considered:

1. Real-time visual feedback
2. Proactive alert systems
3. Gamification
4. Simplified onboarding and guided workflows

**Real-time visual feedback** was selected as the primary strategy because it is most directly suited to continuously changing IoT telemetry and technical monitoring. Proactive alerts and guided workflows are used as supporting strategies.

## Status

**Academic Project — Parts 1 and 2 implemented**

The dashboard concept developed from the accompanying research is implemented and
running end to end:

* **Backend** — seven controllers over two central engines, backed by an in-memory
  store seeded at startup. `SmartXTelemetryEngine` exercises the four Part 1 C#
  concepts; `SmartXCommandEngine` exercises the Part 2 data structures and the
  recommendation engine.
* **Frontend** — an overview at `/`, and `/telemetry` and `/commands`, all consuming
  the API, with page state kept across navigation. The Network Topology module is
  not in the navbar until it is built.

Known gaps, recorded rather than hidden:

* Backend tests cover the Part 2 data structures and `RingBuffer<T>` (`SmartX.Api.Tests`); the frontend has no automated tests.
* No persistence layer — all runtime data is lost on restart.
* `src/pages/TestPage.tsx` has been removed; `ApiStatusBanner` now performs the
  connectivity check on every load.
