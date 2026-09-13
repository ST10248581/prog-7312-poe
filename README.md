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
│       │   ├── EngagementController.cs
│       │   ├── MeshController.cs       # Ingestion, load arithmetic, deployment
│       │   ├── SensorsController.cs
│       │   ├── TelemetryController.cs
│       │   └── TestController.cs       # Connectivity check
│       ├── Logic/                # SmartXTelemetryEngine — the central service class
│       │   ├── ISmartXTelemetryEngine.cs
│       │   ├── ISensorService.cs / ITelemetryService.cs / IAlertService.cs / IEngagementService.cs
│       │   └── SmartXTelemetryEngine.cs
│       ├── Data/                 # Repositories + in-memory data store
│       │   └── Seeding/          # Demo-data seeders (one per entity)
│       ├── Models/               # Entities, requests, responses
│       │   ├── Requests/         # CreateSensorRequest, IngestTelemetryRequest, ...
│       │   ├── Responses/        # EcosystemSummary, SensorDetail, LoadComparison, ...
│       │   └── Telemetry/        # TelemetryPacket<T>, SensorLoad, DeploymentNode
│       ├── Properties/
│       │   └── launchSettings.json     # http profile — port 5127
│       ├── Program.cs            # Entry point, DI registration, CORS, JSON options
│       └── SmartX.Api.csproj     # Project file (net10.0)
├── smart-x-front-end/            # React 19 + TypeScript (Vite)
│   ├── src/
│   │   ├── pages/                # Route-level pages
│   │   │   ├── TelemetryPage.tsx       # The dashboard (the implemented route)
│   │   │   ├── CommandsPage.tsx        # Placeholder (ComingSoon)
│   │   │   ├── TopologyPage.tsx        # Placeholder (ComingSoon)
│   │   │   └── TestPage.tsx            # Standalone API connection check (not routed)
│   │   ├── components/
│   │   │   ├── Navbar.tsx, ComingSoon.tsx
│   │   │   └── telemetry/        # Dashboard widgets — SensorCard, LiveChart,
│   │   │                         # AlertFeed, FilterBar, MeshInsights,
│   │   │                         # DeploymentTree, TroubleshootingGuide, ...
│   │   ├── services/
│   │   │   └── apiService.ts     # Typed API client + shared response types
│   │   ├── utils/format.ts       # Number/date formatting helpers
│   │   ├── assets/               # Images and SVGs
│   │   ├── App.tsx               # Root component + react-router routes
│   │   └── main.tsx              # Entry point
│   ├── public/
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

Both ports are fixed rather than incidental: `Properties/launchSettings.json` pins the
API to 5127, and `Program.cs` registers a CORS policy (`AllowFrontend`) that allows
exactly `http://localhost:5173`. The frontend hard-codes `API_BASE_URL` as
`http://localhost:5127/api` in `src/services/apiService.ts`. If either port changes,
both sides must be updated.

### Verify the Connection

1. Start the backend API.
2. Start the frontend dev server.
3. Open http://localhost:5173 — `/` redirects to `/telemetry`, and the dashboard
   loads live data from the API. Sensor tiles, the ecosystem summary and the alert
   feed populating is itself confirmation that the connection works.
4. To check the API on its own, call the connectivity endpoint directly:
   `GET http://localhost:5127/api/test`.

> `src/pages/TestPage.tsx` contains a standalone **Test API Connection** button that
> calls the same endpoint, but it is not currently wired into a route in `App.tsx`.

### Routes

| Route | Page | State |
| --- | --- | --- |
| `/` | — | Redirects to `/telemetry` |
| `/telemetry` | `TelemetryPage` | Implemented — the dashboard |
| `/commands` | `CommandsPage` | Placeholder (`ComingSoon`), disabled in the navbar |
| `/topology` | `TopologyPage` | Placeholder (`ComingSoon`), disabled in the navbar |

## API Reference

All routes are prefixed with `/api`. Enums are serialised as names (for example
`"Warning"` rather than `1`), and `JsonNumberHandling.AllowNamedFloatingPointLiterals`
is enabled so a gateway can send a lost sample as the string `"NaN"`.

| Method | Route | Purpose |
| --- | --- | --- |
| `GET` | `/api/test` | Connectivity check |
| `GET` | `/api/telemetry/summary` | Ecosystem summary tiles |
| `GET` | `/api/telemetry/series/{sensorProfileId}` | Time series for one sensor |
| `GET` | `/api/telemetry/readings` | Paged raw readings |
| `GET` | `/api/sensors` | Paged, filtered sensor list |
| `GET` | `/api/sensors/{id}` | Sensor detail |
| `GET` | `/api/sensors/filter-options` | Values available to the filter bar |
| `POST` | `/api/sensors` | Register a sensor |
| `PUT` | `/api/sensors/{id}/payload` | Update the sensor payload encoding |
| `POST` | `/api/sensors/{sensorId}/attachments` | Upload an attachment |
| `GET` | `/api/sensors/{sensorId}/attachments/{attachmentId}/download` | Download an attachment |
| `GET` | `/api/alerts` | Alert feed |
| `GET` | `/api/engagement` | Engagement / configuration-progress state |
| `POST` | `/api/mesh/sensors/{sensorId}/ingest` | Gateway batch ingestion |
| `GET` | `/api/mesh/load` | Aggregate load of the selected meters |
| `GET` | `/api/mesh/load/zone/{zone}` | Aggregate load of one zone |
| `GET` | `/api/mesh/load/compare` | Delta between two meters |
| `GET` | `/api/mesh/deployment` | Validate the live deployment tree |
| `POST` | `/api/mesh/deployment/validate` | Validate a proposed configuration profile |

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
| **Sensor registration** | Adds a sensor to the register from the dashboard | `RegisterSensorModal.tsx` ← `POST /api/sensors` |
| **Configuration progress** | Engagement state showing how completely the mesh is configured | `ConfigurationProgress.tsx` ← `GET /api/engagement` |

### Data storage

There is no database. `SmartXDataStore` is an in-memory singleton, populated at
startup by `IDataSeeder.SeedAll()` (`Program.cs`) from the seeders in
`Data/Seeding/`. Everything written at runtime — ingested batches, registered
sensors, uploaded attachments — lives only for the lifetime of the process and is
reset on restart.

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

## Code Attributions and Reference List

The four advanced object-oriented C# concepts were implemented with reference to the
sources listed below. Each source is also cited as a comment in the file(s) where the
technique is used, using the same reference number as this list.

### Reference list

**Generics — `TelemetryPacket<T>`**

1. Microsoft. 2026. *Generic types and methods – C#*. [Online]. Available at:
   <https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/types/generics>
   [Accessed 13 September 2026].
2. Microsoft. 2025. *Constraints on type parameters – C#*. [Online]. Available at:
   <https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/generics/constraints-on-type-parameters>
   [Accessed 13 September 2026].
3. Microsoft. 2025. *Boxing and Unboxing – C#*. [Online]. Available at:
   <https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/types/boxing-and-unboxing>
   [Accessed 13 September 2026].
4. Microsoft. 2025. *Unsafe.As Method (System.Runtime.CompilerServices)*. [Online]. Available at:
   <https://learn.microsoft.com/en-us/dotnet/api/system.runtime.compilerservices.unsafe.as>
   [Accessed 13 September 2026].

**Operator Overloading — `SensorLoad`**

5. Microsoft. 2026. *Operator overloading – Define unary, arithmetic, equality, and comparison
   operators – C# reference*. [Online]. Available at:
   <https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/operator-overloading>
   [Accessed 13 September 2026].
6. Microsoft. 2008. *Operator Overloads – Framework Design Guidelines*. [Online]. Available at:
   <https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/operator-overloads>
   [Accessed 13 September 2026].
7. Microsoft. 2025. *IEquatable\<T\> Interface*. [Online]. Available at:
   <https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1>
   [Accessed 13 September 2026].
8. Microsoft. 2026. *Structure types – C# reference*. [Online]. Available at:
   <https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/struct>
   [Accessed 13 September 2026].

**Advanced Arrays and Lists — the ingestion pipeline**

9. Microsoft. 2026. *The array reference type – C# reference* (multidimensional and jagged
   arrays). [Online]. Available at:
   <https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/arrays>
   [Accessed 13 September 2026].
10. Microsoft. 2025. *Array.GetLength(Int32) Method*. [Online]. Available at:
    <https://learn.microsoft.com/en-us/dotnet/api/system.array.getlength>
    [Accessed 13 September 2026].
11. Microsoft. 2025. *List\<T\> Constructors* (the `List<T>(Int32)` capacity overload).
    [Online]. Available at:
    <https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.list-1.-ctor>
    [Accessed 13 September 2026].

**Recursion — deployment tree validation**

12. Microsoft. 2021. *Iterate Through All Nodes of TreeView Control – Windows Forms*
    (recursive approach). [Online]. Available at:
    <https://learn.microsoft.com/en-us/dotnet/desktop/winforms/controls/how-to-iterate-through-all-nodes-of-a-windows-forms-treeview-control>
    [Accessed 13 September 2026].
13. Microsoft. 2025. *ReferenceEqualityComparer Class*. [Online]. Available at:
    <https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.referenceequalitycomparer>
    [Accessed 13 September 2026].

### Where each reference is cited in the code

All paths are relative to `smart-x-backend/SmartX.Api/`.

| File | References cited | What the reference supports |
| --- | --- | --- |
| `Models/Telemetry/TelemetryPacket.cs` | 1, 2, 3, 4 | Generic class declaration, the `where T : struct` constraint, why the payload is held as `T` rather than `object`, and the `Unsafe.As<TFrom,TTo>` reinterpret-cast used by `TryGetNumeric` / `TryGetBoolean` |
| `Models/Telemetry/SensorLoad.cs` | 5, 6, 7, 8 | `public static` operator declarations, overloading comparison operators in pairs, the explicit (lossy) conversion to `double`, `Equals`/`GetHashCode`/`IEquatable<T>`/`IComparable<T>` consistency, and the `readonly struct` declaration |
| `Models/Requests/IngestTelemetryRequest.cs` | 9 | The `double[][]` jagged array carrying ragged gateway batches |
| `Models/Telemetry/DeploymentNode.cs` | 12 | The self-referencing node shape (a node holding a list of nodes) that makes the validation walk recursive |
| `Logic/SmartXTelemetryEngine.cs` | 1, 3, 5, 6, 9, 10, 11, 12, 13 | Header block lists all; section comments cite 9/10/11 (+1, 3) on `IngestHistoricalBatches` and `ProjectStatistics`, 5/6 on `GetAggregateLoad` / `CompareLoad`, and 12/13 on `ValidateDeployment` / `ValidateNode` |

## Research Focus

The project investigates how different user engagement strategies can improve the usability of high-throughput IoT dashboards.

The following strategies were considered:

1. Real-time visual feedback
2. Proactive alert systems
3. Gamification
4. Simplified onboarding and guided workflows

**Real-time visual feedback** was selected as the primary strategy because it is most directly suited to continuously changing IoT telemetry and technical monitoring. Proactive alerts and guided workflows are used as supporting strategies.

## Status

**Academic Project — Part 1 implemented**

The dashboard concept developed from the accompanying research is implemented and
running end to end:

* **Backend** — complete for the scope above: six controllers over a central
  `SmartXTelemetryEngine`, backed by an in-memory store seeded at startup. All four
  required advanced C# concepts are exercised by live endpoints.
* **Frontend** — the `/telemetry` dashboard is built and consumes the API. `/commands`
  and `/topology` are deliberate `ComingSoon` placeholders, disabled in the navbar.

Known gaps, recorded rather than hidden:

* No automated tests in either project.
* No persistence layer — all runtime data is lost on restart.
* `src/pages/TestPage.tsx` is no longer reachable; the dashboard replaced it as the
  connectivity check.
