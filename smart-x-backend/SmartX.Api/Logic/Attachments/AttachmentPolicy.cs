using System.Text;
using SmartX.Api.Models;

namespace SmartX.Api.Logic.Attachments;

/// <summary>
/// The allow-list of files a sensor profile will accept, and the checks that
/// enforce it. A file is accepted only when all three agree:
/// <list type="number">
///   <item><description>its extension is on the list for the chosen attachment type,</description></item>
///   <item><description>the browser's declared MIME type matches that extension, and</description></item>
///   <item><description>its first bytes match the format's signature ("magic bytes"), so a renamed executable cannot pass as a photo.</description></item>
/// </list>
/// The stored content type always comes from this list, never from the client.
/// </summary>
public static class AttachmentPolicy
{
    /// <summary>Bytes read from the start of a file to identify it.</summary>
    public const int SniffLength = 4096;

    public const int MaxFileNameLength = 120;
    public const int MaxDescriptionLength = 200;

    private static readonly byte[] JpegSignature = [0xFF, 0xD8, 0xFF];
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    /// <summary>
    /// What browsers send for a file the OS has no type for (.log, .yaml, .conf).
    /// Accepted for text formats only; their content is checked instead.
    /// </summary>
    private static readonly string[] Unlabelled = ["", "application/octet-stream"];

    private static readonly AttachmentType[] ConfigOnly = [AttachmentType.ConfigFile];
    private static readonly AttachmentType[] LogOnly = [AttachmentType.HardwareLog];
    private static readonly AttachmentType[] ConfigOrLog = [AttachmentType.ConfigFile, AttachmentType.HardwareLog];
    private static readonly AttachmentType[] PhotoOnly = [AttachmentType.DeploymentPhoto];

    private static readonly Dictionary<string, FileKind> Kinds = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = FileKind.Binary("image/jpeg", ["image/jpeg", "image/pjpeg"], PhotoOnly, IsJpeg),
        [".jpeg"] = FileKind.Binary("image/jpeg", ["image/jpeg", "image/pjpeg"], PhotoOnly, IsJpeg),
        [".png"] = FileKind.Binary("image/png", ["image/png"], PhotoOnly, IsPng),
        [".webp"] = FileKind.Binary("image/webp", ["image/webp"], PhotoOnly, IsWebp),

        [".json"] = FileKind.Text("application/json", ["application/json", "text/json"], ConfigOrLog),
        [".yaml"] = FileKind.Text("application/yaml", ["application/yaml", "application/x-yaml", "text/yaml", "text/x-yaml"], ConfigOnly),
        [".yml"] = FileKind.Text("application/yaml", ["application/yaml", "application/x-yaml", "text/yaml", "text/x-yaml"], ConfigOnly),
        [".xml"] = FileKind.Text("application/xml", ["application/xml", "text/xml"], ConfigOnly),
        [".ini"] = FileKind.Text("text/plain", ["text/plain"], ConfigOnly),
        [".conf"] = FileKind.Text("text/plain", ["text/plain"], ConfigOnly),
        [".cfg"] = FileKind.Text("text/plain", ["text/plain"], ConfigOnly),
        [".txt"] = FileKind.Text("text/plain", ["text/plain"], ConfigOrLog),
        [".log"] = FileKind.Text("text/plain", ["text/plain", "text/x-log"], LogOnly),
        // Windows reports .csv as an Excel type, so that is accepted too.
        [".csv"] = FileKind.Text("text/csv", ["text/csv", "application/csv", "application/vnd.ms-excel"], LogOnly)
    };

    /// <summary>Extensions accepted for each attachment type, for error messages and the client.</summary>
    public static IReadOnlyList<string> ExtensionsFor(AttachmentType type) =>
        Kinds.Where(kind => kind.Value.AllowedFor.Contains(type)).Select(kind => kind.Key).Order().ToList();

    /// <summary>
    /// Checks the name, declared type and size of an upload before any of it is
    /// read. Returns the matched kind, or an error message.
    /// </summary>
    public static (FileKind? Kind, string? Error) CheckDeclared(
        string fileName,
        string? declaredContentType,
        long length,
        AttachmentType attachmentType,
        long maxBytes)
    {
        if (length <= 0)
        {
            return (null, "The file is empty.");
        }

        if (length > maxBytes)
        {
            return (null, $"The file is {FormatBytes(length)}; the limit is {FormatBytes(maxBytes)}.");
        }

        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrEmpty(extension) || !Kinds.TryGetValue(extension, out var kind))
        {
            return (null, $"{(string.IsNullOrEmpty(extension) ? "Files without an extension" : $"{extension} files")} are not accepted. " +
                          $"Allowed for {attachmentType}: {string.Join(", ", ExtensionsFor(attachmentType))}.");
        }

        if (!kind.AllowedFor.Contains(attachmentType))
        {
            return (null, $"{extension} files cannot be attached as {attachmentType}. " +
                          $"Allowed: {string.Join(", ", ExtensionsFor(attachmentType))}.");
        }

        var declared = (declaredContentType ?? string.Empty).Split(';')[0].Trim();
        var declaredOk = kind.AcceptedContentTypes.Contains(declared, StringComparer.OrdinalIgnoreCase)
                         || (kind.IsText && Unlabelled.Contains(declared, StringComparer.OrdinalIgnoreCase));
        if (!declaredOk)
        {
            return (null, $"The file is labelled {declared} but has a {extension} extension.");
        }

        return (kind, null);
    }

    /// <summary>
    /// Checks the first bytes of the file against its claimed format. Binary
    /// formats must carry their signature; text formats must be valid UTF-8 with
    /// no NUL bytes, which rules out executables and archives renamed to .txt.
    /// </summary>
    public static string? CheckContent(FileKind kind, ReadOnlySpan<byte> head, bool isWholeFile)
    {
        if (kind.Signature is not null)
        {
            return kind.Signature(head) ? null : "The file's contents do not match its extension.";
        }

        if (head.IndexOf((byte)0) >= 0)
        {
            return "The file contains binary data, so it is not a text file.";
        }

        try
        {
            // flush: false tolerates a multi-byte character cut off at the end of
            // the sniffed window; it still throws on genuinely invalid sequences.
            var decoder = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true).GetDecoder();
            decoder.GetCharCount(head, flush: isWholeFile);
            return null;
        }
        catch (DecoderFallbackException)
        {
            return "The file is not valid UTF-8 text.";
        }
    }

    /// <summary>
    /// Strips any path and control characters from a client-supplied file name
    /// and caps its length, keeping the extension.
    /// </summary>
    public static string SanitiseFileName(string fileName)
    {
        var name = Path.GetFileName(fileName.Replace('\\', '/'));
        var cleaned = new string(name.Where(character => !char.IsControl(character) && character is not ('"' or '<' or '>' or '|' or ':' or '*' or '?')).ToArray()).Trim();

        if (cleaned.Length <= MaxFileNameLength)
        {
            return cleaned;
        }

        var extension = Path.GetExtension(cleaned);
        return cleaned[..(MaxFileNameLength - extension.Length)] + extension;
    }

    private static bool IsJpeg(ReadOnlySpan<byte> head) => head.StartsWith(JpegSignature);

    private static bool IsPng(ReadOnlySpan<byte> head) => head.StartsWith(PngSignature);

    // RIFF....WEBP: a RIFF container whose form type is WEBP.
    private static bool IsWebp(ReadOnlySpan<byte> head) =>
        head.Length >= 12 && head[..4].SequenceEqual("RIFF"u8) && head.Slice(8, 4).SequenceEqual("WEBP"u8);

    private static string FormatBytes(long bytes) =>
        bytes >= 1024 * 1024 ? $"{bytes / (1024d * 1024d):0.#} MB" : $"{bytes / 1024d:0.#} KB";
}

/// <summary>One accepted file format.</summary>
public sealed class FileKind
{
    public delegate bool SignatureCheck(ReadOnlySpan<byte> head);

    private FileKind(string contentType, string[] acceptedContentTypes, AttachmentType[] allowedFor, SignatureCheck? signature)
    {
        ContentType = contentType;
        AcceptedContentTypes = acceptedContentTypes;
        AllowedFor = allowedFor;
        Signature = signature;
    }

    /// <summary>The content type the file is stored and served with.</summary>
    public string ContentType { get; }

    public string[] AcceptedContentTypes { get; }

    public AttachmentType[] AllowedFor { get; }

    /// <summary>Magic-byte check for binary formats; null for text formats.</summary>
    public SignatureCheck? Signature { get; }

    public bool IsText => Signature is null;

    public static FileKind Binary(string contentType, string[] accepted, AttachmentType[] allowedFor, SignatureCheck signature) =>
        new(contentType, accepted, allowedFor, signature);

    public static FileKind Text(string contentType, string[] accepted, AttachmentType[] allowedFor) =>
        new(contentType, accepted, allowedFor, null);
}
