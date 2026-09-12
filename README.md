# prog-7312-poe - Smart-X IoT Mesh Ecosystem Dashboard

> Smart-X IoT Mesh Ecosystem dashboard for real-time telemetry monitoring, device status tracking, alerts, and interactive troubleshooting.

## Overview

The Smart-X IoT Mesh Ecosystem is a proposed interactive dashboard designed to help users monitor and manage a high-throughput IoT environment. The dashboard focuses on **real-time visual feedback** to make continuously changing IoT telemetry easier to understand. It allows users to monitor device status, identify abnormal sensor readings, receive alerts and investigate potential device or connectivity problems. The project is based on research into user engagement and dashboard interactivity for high-throughput IoT systems.

## Project Structure

```
prog-7312-poe/
├── smart-x-backend/          # ASP.NET 10 Web API
│   └── SmartX.Api/
│       ├── Controllers/      # API controllers (thin — no business logic)
│       ├── Logic/            # SmartXTelemetryEngine — the central service class
│       ├── Data/             # Repositories + in-memory data store and seeders
│       ├── Models/           # Entities, requests, responses
│       │   └── Telemetry/    # TelemetryPacket<T>, SensorLoad, DeploymentNode
│       ├── Program.cs         # Application entry point
│       └── SmartX.Api.csproj  # Project file (.NET 10)
├── smart-x-front-end/        # React + TypeScript (Vite)
│   ├── src/
│   │   ├── pages/            # Page components
│   │   ├── services/         # API service layer
│   │   ├── App.tsx           # Root component
│   │   └── main.tsx          # Entry point
│   ├── package.json
│   └── vite.config.ts
├── .gitignore
└── README.md
```

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
- [Node.js](https://nodejs.org/) (v18+)
- npm (comes with Node.js)

## Getting Started

### Backend (ASP.NET 10 Web API)

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

### Verify the Connection

1. Start the backend API.
2. Start the frontend dev server.
3. Open http://localhost:5173 in your browser.
4. Click the **Test API Connection** button — you should see a success response from the API.

## Key Features

* **Real-time telemetry** — Displays continuously changing sensor data through interactive visualisations.
* **Device status monitoring** — Shows devices as Online, Warning or Offline.
* **Anomaly detection** — Highlights unusual readings and values outside defined thresholds.
* **Proactive alerts** — Notifies users about important events such as device disconnections or abnormal telemetry.
* **Filtering** — Allows users to focus on specific devices, sensors, statuses or time periods.
* **Guided troubleshooting** — Provides structured information to help investigate device and connectivity issues.
* **Progressive disclosure** — Presents important information first while allowing users to access more detailed diagnostics when required.

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

Centralising the logic matters because the operations that were missing — batch
ingestion, aggregate load and deployment validation — each span more than one
repository. Spreading them across four services would have meant cross-service calls
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

## Research Focus

The project investigates how different user engagement strategies can improve the usability of high-throughput IoT dashboards.

The following strategies were considered:

1. Real-time visual feedback
2. Proactive alert systems
3. Gamification
4. Simplified onboarding and guided workflows

**Real-time visual feedback** was selected as the primary strategy because it is most directly suited to continuously changing IoT telemetry and technical monitoring. Proactive alerts and guided workflows are used as supporting strategies.

## Status

**Proposed Solution / Academic Project**

This project represents the proposed dashboard concept developed from the accompanying research into user engagement and dashboard interactivity for high-throughput IoT systems.
