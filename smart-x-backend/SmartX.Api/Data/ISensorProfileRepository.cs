using SmartX.Api.Models;
using SmartX.Api.Models.Requests;
using SmartX.Api.Models.Responses;

namespace SmartX.Api.Data;

public interface ISensorProfileRepository
{
    List<SensorListItem> GetAll(TelemetryQuery query);
    SensorDetail? GetDetail(Guid id);
    FilterOptions GetFilterOptions();
    SensorProfile? UpdatePayload(Guid id, UpdateSensorPayloadRequest request);
    SensorProfile Create(CreateSensorRequest request);
    SensorAttachment AddAttachment(Guid sensorId, SensorAttachment attachment, byte[] fileData);
    (SensorAttachment attachment, byte[] fileData)? GetAttachmentFile(Guid sensorId, Guid attachmentId);
}
