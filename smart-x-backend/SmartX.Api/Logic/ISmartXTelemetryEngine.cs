using SmartX.Api.Models;
using SmartX.Api.Models.Requests;
using SmartX.Api.Models.Responses;
using SmartX.Api.Models.Telemetry;

namespace SmartX.Api.Logic;

/// <summary>
/// The single application-logic surface of the Smart-X API. It absorbs the
/// sensor, telemetry, alert and engagement services so every controller talks to
/// one class, and adds the mesh-level operations — batch ingestion, load
/// aggregation and deployment validation — that span more than one repository.
/// </summary>
public interface ISmartXTelemetryEngine : ISensorService, ITelemetryService, IAlertService, IEngagementService
{
    /// <summary>
    /// Ingests a jagged block of sequential historical batches for one sensor,
    /// wrapping every raw sample in a strongly typed packet before flattening the
    /// lot into the reading collection. Returns null if the sensor is unknown.
    /// </summary>
    TelemetryIngestResult? IngestHistoricalBatches(Guid sensorProfileId, IngestTelemetryRequest request);

    /// <summary>Folds the latest power reading of every named meter into one aggregate.</summary>
    AggregateLoad GetAggregateLoad(IEnumerable<Guid> sensorProfileIds);

    /// <summary>Aggregate draw of every meter deployed in one zone.</summary>
    AggregateLoad GetZoneLoad(string zone);

    /// <summary>
    /// Delta between two meters. Returns null when either meter is unknown or has
    /// never reported power; throws when the two report in incompatible units.
    /// </summary>
    LoadComparison? CompareLoad(Guid leftSensorId, Guid rightSensorId);

    /// <summary>Builds the live deployment tree and validates it recursively.</summary>
    DeploymentValidationReport ValidateDeployment(string? zone = null);

    /// <summary>Validates a proposed, not-yet-deployed configuration profile.</summary>
    DeploymentValidationReport ValidateDeployment(DeploymentNode root);
}
