using SmartX.Api.Data;
using SmartX.Api.Models;
using SmartX.Api.Models.Requests;
using SmartX.Api.Models.Responses;

namespace SmartX.Api.Logic;

public class SensorService : ISensorService
{
    private const int DetailSeriesMaxPoints = 240;

    private readonly ISensorProfileRepository _sensorProfileRepository;
    private readonly ITelemetryRepository _telemetryRepository;

    public SensorService(
        ISensorProfileRepository sensorProfileRepository,
        ITelemetryRepository telemetryRepository)
    {
        _sensorProfileRepository = sensorProfileRepository;
        _telemetryRepository = telemetryRepository;
    }

    public List<SensorListItem> GetSensors(TelemetryQuery query)
    {
        return _sensorProfileRepository.GetAll(query);
    }

    public SensorDetail? GetSensorDetail(Guid id)
    {
        var detail = _sensorProfileRepository.GetDetail(id);
        if (detail is null)
        {
            return null;
        }

        detail.Series = _telemetryRepository.GetSeries(
            id,
            DateTime.UtcNow.AddHours(-24),
            DetailSeriesMaxPoints);

        return detail;
    }

    public FilterOptions GetFilterOptions()
    {
        return _sensorProfileRepository.GetFilterOptions();
    }

    public SensorProfile? UpdateSensorPayload(Guid id, UpdateSensorPayloadRequest request)
    {
        return _sensorProfileRepository.UpdatePayload(id, request);
    }

    public SensorProfile CreateSensor(CreateSensorRequest request)
    {
        return _sensorProfileRepository.Create(request);
    }

    public SensorAttachment? UploadAttachment(Guid sensorId, IFormFile file, AttachmentType attachmentType, string description)
    {
        var sensor = _sensorProfileRepository.GetDetail(sensorId);
        if (sensor is null)
        {
            return null;
        }

        using var memoryStream = new MemoryStream();
        file.CopyTo(memoryStream);
        var fileData = memoryStream.ToArray();

        var attachment = new SensorAttachment
        {
            Id = Guid.NewGuid(),
            SensorProfileId = sensorId,
            FileName = file.FileName,
            StoredFileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}",
            ContentType = file.ContentType,
            FileSizeBytes = file.Length,
            AttachmentType = attachmentType,
            UploadedUtc = DateTime.UtcNow,
            UploadedBy = "user",
            Description = description
        };

        return _sensorProfileRepository.AddAttachment(sensorId, attachment, fileData);
    }

    public (SensorAttachment attachment, byte[] fileData)? DownloadAttachment(Guid sensorId, Guid attachmentId)
    {
        return _sensorProfileRepository.GetAttachmentFile(sensorId, attachmentId);
    }
}
