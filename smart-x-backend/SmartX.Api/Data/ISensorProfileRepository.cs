using SmartX.Api.Models;
using SmartX.Api.Models.Requests;
using SmartX.Api.Models.Responses;

namespace SmartX.Api.Data;

public interface ISensorProfileRepository
{
    List<SensorListItem> GetAll(TelemetryQuery query);

    /// <summary>Raw profiles, for logic that needs the entity rather than the list view.</summary>
    List<SensorProfile> GetProfiles();

    /// <summary>O(1) lookups through the store's indexes.</summary>
    SensorProfile? GetById(Guid id);
    SensorProfile? GetByMacAddress(string macAddress);
    SensorProfile? GetByNodeId(string nodeId);

    SensorDetail? GetDetail(Guid id);
    FilterOptions GetFilterOptions();
    SensorProfile? UpdatePayload(Guid id, UpdateSensorPayloadRequest request);
    SensorProfile Create(CreateSensorRequest request);

    /// <summary>Records an attachment together with its encrypted payload.</summary>
    SensorAttachment AddAttachment(Guid sensorId, SensorAttachment attachment, byte[] sealedPayload);

    /// <summary>
    /// The attachment and its encrypted payload. The payload is null for seeded
    /// attachments, which describe files that were never uploaded.
    /// </summary>
    (SensorAttachment Attachment, byte[]? SealedPayload)? GetAttachment(Guid sensorId, Guid attachmentId);
}
