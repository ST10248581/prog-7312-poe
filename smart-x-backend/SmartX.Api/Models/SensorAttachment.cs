namespace SmartX.Api.Models;

public class SensorAttachment
{
    public Guid Id { get; set; }
    public Guid SensorProfileId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public AttachmentType AttachmentType { get; set; }
    public DateTime UploadedUtc { get; set; }
    public string UploadedBy { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
