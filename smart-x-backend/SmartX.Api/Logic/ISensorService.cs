using SmartX.Api.Models;
using SmartX.Api.Models.Requests;
using SmartX.Api.Models.Responses;

namespace SmartX.Api.Logic;

public interface ISensorService
{
    Task<List<SensorListItem>> GetSensorsAsync(TelemetryQuery query, CancellationToken cancellationToken = default);
    Task<SensorDetail?> GetSensorDetailAsync(Guid id, CancellationToken cancellationToken = default);
    Task<FilterOptions> GetFilterOptionsAsync(CancellationToken cancellationToken = default);
    Task<WriteResult<SensorProfile>> UpdateSensorPayloadAsync(Guid id, UpdateSensorPayloadRequest request, CancellationToken cancellationToken = default);
    Task<WriteResult<SensorProfile>> CreateSensorAsync(CreateSensorRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates, hashes and encrypts an uploaded file in a single streaming pass,
    /// then attaches it to the sensor.
    /// </summary>
    Task<WriteResult<SensorAttachment>> UploadAttachmentAsync(
        Guid sensorId,
        IFormFile file,
        AttachmentType attachmentType,
        string description,
        CancellationToken cancellationToken = default);

    /// <summary>Decrypts an attachment and verifies its SHA-256 before it is served. Null if not found.</summary>
    Task<AttachmentDownload?> DownloadAttachmentAsync(Guid sensorId, Guid attachmentId, CancellationToken cancellationToken = default);
}
