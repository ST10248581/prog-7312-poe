using SmartX.Api.Models.Requests;
using SmartX.Api.Models.Responses;
using SmartX.Api.Models.Telemetry;

namespace SmartX.Api.Logic;

/// <summary>
/// The single application-logic surface of the Smart-X API. It absorbs the
/// sensor, telemetry, alert and engagement services so every controller talks to
/// one class, and adds the mesh-level operations — batch ingestion, load
/// aggregation and deployment validation — that span more than one repository.
/// <para>
/// Every operation is asynchronous and cancellable. The store is in memory
/// today, so most complete synchronously, but the contract is the one a
/// database-backed store needs: swapping the repositories later does not change
/// a single controller. When a browser abandons a request (its timeout fires or
/// the user navigates away) the token stops the work.
/// </para>
/// </summary>
public interface ISmartXTelemetryEngine : ISensorService, ITelemetryService, IAlertService, IEngagementService
{
    /// <summary>
    /// Ingests a jagged block of sequential historical batches for one sensor,
    /// wrapping every raw sample in a strongly typed packet before flattening the
    /// lot into the reading collection. Returns null if the sensor is unknown.
    /// </summary>
    Task<TelemetryIngestResult?> IngestHistoricalBatchesAsync(Guid sensorProfileId, IngestTelemetryRequest request, CancellationToken cancellationToken = default);

    /// <summary>Folds the latest power reading of every named meter into one aggregate.</summary>
    Task<AggregateLoad> GetAggregateLoadAsync(IEnumerable<Guid> sensorProfileIds, CancellationToken cancellationToken = default);

    /// <summary>Aggregate draw of every meter deployed in one zone.</summary>
    Task<AggregateLoad> GetZoneLoadAsync(string zone, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delta between two meters. Returns null when either meter is unknown or has
    /// never reported power; throws when the two report in incompatible units.
    /// </summary>
    Task<LoadComparison?> CompareLoadAsync(Guid leftSensorId, Guid rightSensorId, CancellationToken cancellationToken = default);

    /// <summary>Builds the live deployment tree and validates it recursively.</summary>
    Task<DeploymentValidationReport> ValidateDeploymentAsync(string? zone = null, CancellationToken cancellationToken = default);

    /// <summary>Validates a proposed, not-yet-deployed configuration profile.</summary>
    Task<DeploymentValidationReport> ValidateDeploymentAsync(DeploymentNode root, CancellationToken cancellationToken = default);
}
