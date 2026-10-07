using SmartX.Api.Models;

namespace SmartX.Api.Logic;

public enum WriteStatus
{
    Success,
    NotFound,

    /// <summary>The value collides with another record (duplicate MAC address or node id).</summary>
    Conflict,

    /// <summary>The input passed model validation but failed a deeper check (e.g. file contents).</summary>
    Invalid
}

/// <summary>
/// The outcome of a write, so the controller can choose the status code
/// (201, 404, 409, 400) without the service knowing about HTTP.
/// </summary>
public sealed record WriteResult<T>(WriteStatus Status, T? Value, string? Field = null, string? Error = null)
{
    public static WriteResult<T> Ok(T value) => new(WriteStatus.Success, value);
    public static WriteResult<T> Missing() => new(WriteStatus.NotFound, default);
    public static WriteResult<T> Conflicting(string field, string error) => new(WriteStatus.Conflict, default, field, error);
    public static WriteResult<T> Rejected(string field, string error) => new(WriteStatus.Invalid, default, field, error);
}

/// <summary>A decrypted, integrity-checked attachment ready to send.</summary>
public sealed record AttachmentDownload(SensorAttachment Attachment, Stream Content, string ContentType);
