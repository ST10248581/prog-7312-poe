using System.ComponentModel.DataAnnotations;

namespace SmartX.Api.Configuration;

/// <summary>
/// Where the dashboard is served from. Bound from the "Frontend" section of
/// appsettings.json, so pointing the API at a different client is a
/// configuration change rather than a code change.
/// </summary>
public class FrontendOptions
{
    public const string SectionName = "Frontend";

    /// <summary>Origins allowed to call the API from a browser (the CORS allow-list).</summary>
    [MinLength(1, ErrorMessage = "Frontend:AllowedOrigins must list at least one origin.")]
    public string[] AllowedOrigins { get; set; } = [];
}

/// <summary>Limits and encryption settings for sensor attachments ("Attachments" section).</summary>
public class AttachmentOptions
{
    public const string SectionName = "Attachments";

    /// <summary>
    /// Largest file accepted, in bytes. The multipart body limit is derived from
    /// this (see Program.cs). Capped below Kestrel's default 30 MB request limit,
    /// which still applies.
    /// </summary>
    [Range(1, 25 * 1024 * 1024)]
    public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024;

    /// <summary>
    /// Plaintext bytes encrypted per AES-GCM chunk. Uploads are read and
    /// encrypted one chunk at a time, so memory per upload stays at about two
    /// chunks however large the file is.
    /// </summary>
    [Range(4 * 1024, 1024 * 1024)]
    public int ChunkSizeBytes { get; set; } = 64 * 1024;

    /// <summary>
    /// Base64 AES-256 key (32 bytes). Supply it through user-secrets or an
    /// environment variable (Attachments__EncryptionKey), never in a committed
    /// file. When it is absent a random key is generated at start-up; that suits
    /// this in-memory store, whose files do not outlive the process either.
    /// </summary>
    public string? EncryptionKey { get; set; }
}
