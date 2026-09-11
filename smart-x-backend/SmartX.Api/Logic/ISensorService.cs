using SmartX.Api.Models;
using SmartX.Api.Models.Requests;
using SmartX.Api.Models.Responses;

namespace SmartX.Api.Logic;

public interface ISensorService
{
    List<SensorListItem> GetSensors(TelemetryQuery query);
    SensorDetail? GetSensorDetail(Guid id);
    FilterOptions GetFilterOptions();
    SensorProfile? UpdateSensorPayload(Guid id, UpdateSensorPayloadRequest request);
    SensorProfile CreateSensor(CreateSensorRequest request);
    SensorAttachment? UploadAttachment(Guid sensorId, IFormFile file, AttachmentType attachmentType, string description);
    (SensorAttachment attachment, byte[] fileData)? DownloadAttachment(Guid sensorId, Guid attachmentId);
}
