// =============================================================================
// CODE ATTRIBUTION — Attachment encryption at rest
//
// The chunked authenticated encryption of sensor attachments in this file was
// written with reference to the sources below.
//
// Code Attribution [23]
// Author: Microsoft
// Year: 2025
// Title: AesGcm Class (System.Security.Cryptography)
// Type: [Source code]
// Available at: <https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.aesgcm>
// Accessed: [Accessed 8 October 2026]
// Modifications: Used AesGcm with a 256-bit key and a 16-byte tag to seal each
//   fixed-size chunk of an attachment separately, so large files are encrypted
//   without being held in memory.
// Reference: Microsoft, 2025. AesGcm Class (System.Security.Cryptography) [Source code] Available at: <https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.aesgcm> [Accessed 8 October 2026].
//
// Code Attribution [24]
// Author: Microsoft
// Year: 2025
// Title: IncrementalHash Class (System.Security.Cryptography)
// Type: [Source code]
// Available at: <https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.incrementalhash>
// Accessed: [Accessed 8 October 2026]
// Modifications: Used IncrementalHash.CreateHash(SHA256) to hash the attachment
//   chunk by chunk while it is encrypted and decrypted.
// Reference: Microsoft, 2025. IncrementalHash Class (System.Security.Cryptography) [Source code] Available at: <https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.incrementalhash> [Accessed 8 October 2026].
//
// Code Attribution [25]
// Author: Hoang, V.T., Reyhanitabar, R., Rogaway, P. and Vizár, D.
// Year: 2015
// Title: Online Authenticated-Encryption and its Nonce-Reuse Misuse-Resistance
// Type: [Source code]
// Available at: <https://eprint.iacr.org/2015/189>
// Accessed: [Accessed 8 October 2026]
// Modifications: Adapted the STREAM construction: each chunk's nonce is a
//   random per-file prefix plus the chunk counter, and the authenticated data
//   binds the attachment id, chunk index and a final-chunk flag, so chunks
//   cannot be swapped, reordered or truncated. Not taken from code; written
//   from the paper's description.
// Reference: Hoang, V.T., Reyhanitabar, R., Rogaway, P. and Vizár, D., 2015. Online Authenticated-Encryption and its Nonce-Reuse Misuse-Resistance [Source code] Available at: <https://eprint.iacr.org/2015/189> [Accessed 8 October 2026].
// =============================================================================

using System.Buffers;
using System.Buffers.Binary;
using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using SmartX.Api.Configuration;

namespace SmartX.Api.Logic.Attachments;

/// <summary>
/// Encrypts attachments at rest with AES-256-GCM, one chunk at a time.
/// <para>
/// AES-GCM in .NET works on whole buffers, so a large file is split into
/// fixed-size chunks that are each sealed separately. That keeps memory per
/// upload at about two chunks, and lets encryption start before the whole file
/// has been read. Each chunk's nonce is a random per-file prefix plus the chunk
/// number, and its authenticated data binds the chunk to the attachment id, its
/// position and whether it is the last one - so chunks cannot be swapped between
/// files, reordered, or dropped from the end without decryption failing.
/// </para>
/// <para>
/// Layout: "SXE1" | chunk size (int32) | nonce prefix (8 bytes) | chunk 0 | ... | chunk n,
/// where every chunk is ciphertext followed by its 16-byte tag.
/// </para>
/// </summary>
public sealed class AttachmentCipher
{
    private static readonly byte[] Magic = "SXE1"u8.ToArray();

    private const int NoncePrefixLength = 8;
    private const int HeaderLength = 4 + sizeof(int) + NoncePrefixLength;
    private const int TagLength = 16;
    private const int NonceLength = 12;
    private const int AadLength = 16 + sizeof(int) + 1;

    private readonly byte[] _key;
    private readonly int _chunkSize;

    public AttachmentCipher(IOptions<AttachmentOptions> options, ILogger<AttachmentCipher> logger)
    {
        _chunkSize = options.Value.ChunkSizeBytes;

        if (string.IsNullOrWhiteSpace(options.Value.EncryptionKey))
        {
            _key = RandomNumberGenerator.GetBytes(32);
            logger.LogInformation(
                "No Attachments:EncryptionKey configured; generated a key for this run. " +
                "Attachments are held in memory, so none outlive the key.");
        }
        else
        {
            _key = Convert.FromBase64String(options.Value.EncryptionKey);
            if (_key.Length != 32)
            {
                throw new InvalidOperationException("Attachments:EncryptionKey must be a base64-encoded 32-byte (AES-256) key.");
            }
        }
    }

    /// <summary>Size of the sealed payload for a file of <paramref name="plaintextLength"/> bytes.</summary>
    public long SealedLength(long plaintextLength)
    {
        var chunks = Math.Max(1, (plaintextLength + _chunkSize - 1) / _chunkSize);
        return HeaderLength + plaintextLength + chunks * TagLength;
    }

    /// <summary>
    /// Reads exactly <paramref name="plaintextLength"/> bytes from
    /// <paramref name="source"/>, encrypting as it goes, and returns the sealed
    /// payload with the SHA-256 of the plaintext. The output is allocated once at
    /// its final size, so the file is never held twice.
    /// </summary>
    public async Task<SealedAttachment> EncryptAsync(
        Guid attachmentId,
        Stream source,
        long plaintextLength,
        CancellationToken cancellationToken)
    {
        var sealedPayload = new byte[SealedLength(plaintextLength)];

        Magic.CopyTo(sealedPayload, 0);
        BinaryPrimitives.WriteInt32BigEndian(sealedPayload.AsSpan(4), _chunkSize);
        RandomNumberGenerator.Fill(sealedPayload.AsSpan(8, NoncePrefixLength));
        var noncePrefix = sealedPayload.AsMemory(8, NoncePrefixLength);

        using var aes = new AesGcm(_key, TagLength);
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);

        var buffer = ArrayPool<byte>.Shared.Rent(_chunkSize);
        try
        {
            var offset = HeaderLength;
            long remaining = plaintextLength;
            var chunkIndex = 0;

            do
            {
                var size = (int)Math.Min(_chunkSize, remaining);
                await source.ReadExactlyAsync(buffer.AsMemory(0, size), cancellationToken);

                var plaintext = buffer.AsSpan(0, size);
                hash.AppendData(plaintext);

                remaining -= size;
                var isFinal = remaining == 0;

                SealChunk(aes, attachmentId, noncePrefix.Span, chunkIndex, isFinal, plaintext,
                    sealedPayload.AsSpan(offset, size), sealedPayload.AsSpan(offset + size, TagLength));

                offset += size + TagLength;
                chunkIndex++;
            }
            while (remaining > 0);

            // A client that sent more than it declared is lying about the file.
            if (source.CanSeek ? source.Position != source.Length : await source.ReadAsync(buffer.AsMemory(0, 1), cancellationToken) != 0)
            {
                throw new InvalidDataException("The upload was longer than its declared length.");
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer, clearArray: true);
        }

        return new SealedAttachment(sealedPayload, Convert.ToHexStringLower(hash.GetHashAndReset()));
    }

    /// <summary>
    /// Decrypts a sealed payload chunk by chunk into <paramref name="destination"/>
    /// and returns the SHA-256 of what was written. Throws
    /// <see cref="AuthenticationTagMismatchException"/> if any chunk was altered.
    /// </summary>
    public async Task<string> DecryptAsync(
        Guid attachmentId,
        byte[] sealedPayload,
        Stream destination,
        CancellationToken cancellationToken)
    {
        if (sealedPayload.Length < HeaderLength + TagLength || !sealedPayload.AsSpan(0, 4).SequenceEqual(Magic))
        {
            throw new InvalidDataException("The stored attachment is not a recognised encrypted payload.");
        }

        var chunkSize = BinaryPrimitives.ReadInt32BigEndian(sealedPayload.AsSpan(4));
        var noncePrefix = sealedPayload.AsMemory(8, NoncePrefixLength);

        using var aes = new AesGcm(_key, TagLength);
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);

        var buffer = ArrayPool<byte>.Shared.Rent(chunkSize);
        try
        {
            var offset = HeaderLength;
            var chunkIndex = 0;

            while (offset < sealedPayload.Length)
            {
                var remaining = sealedPayload.Length - offset;
                var isFinal = remaining <= chunkSize + TagLength;
                var size = isFinal ? remaining - TagLength : chunkSize;

                if (size < 0)
                {
                    throw new InvalidDataException("The stored attachment is truncated.");
                }

                OpenChunk(aes, attachmentId, noncePrefix.Span, chunkIndex, isFinal,
                    sealedPayload.AsSpan(offset, size), sealedPayload.AsSpan(offset + size, TagLength),
                    buffer.AsSpan(0, size));

                hash.AppendData(buffer, 0, size);
                await destination.WriteAsync(buffer.AsMemory(0, size), cancellationToken);

                offset += size + TagLength;
                chunkIndex++;
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer, clearArray: true);
        }

        return Convert.ToHexStringLower(hash.GetHashAndReset());
    }

    private static void SealChunk(
        AesGcm aes, Guid attachmentId, ReadOnlySpan<byte> noncePrefix, int chunkIndex, bool isFinal,
        ReadOnlySpan<byte> plaintext, Span<byte> ciphertext, Span<byte> tag)
    {
        Span<byte> nonce = stackalloc byte[NonceLength];
        Span<byte> aad = stackalloc byte[AadLength];
        BuildNonceAndAad(attachmentId, noncePrefix, chunkIndex, isFinal, nonce, aad);

        aes.Encrypt(nonce, plaintext, ciphertext, tag, aad);
    }

    private static void OpenChunk(
        AesGcm aes, Guid attachmentId, ReadOnlySpan<byte> noncePrefix, int chunkIndex, bool isFinal,
        ReadOnlySpan<byte> ciphertext, ReadOnlySpan<byte> tag, Span<byte> plaintext)
    {
        Span<byte> nonce = stackalloc byte[NonceLength];
        Span<byte> aad = stackalloc byte[AadLength];
        BuildNonceAndAad(attachmentId, noncePrefix, chunkIndex, isFinal, nonce, aad);

        aes.Decrypt(nonce, ciphertext, tag, plaintext, aad);
    }

    private static void BuildNonceAndAad(
        Guid attachmentId, ReadOnlySpan<byte> noncePrefix, int chunkIndex, bool isFinal,
        Span<byte> nonce, Span<byte> aad)
    {
        // Nonce: per-file random prefix + chunk counter, so no two chunks under
        // this key ever share a nonce.
        noncePrefix.CopyTo(nonce);
        BinaryPrimitives.WriteInt32BigEndian(nonce[NoncePrefixLength..], chunkIndex);

        // Authenticated data: which file, which chunk, and whether it is the last.
        attachmentId.TryWriteBytes(aad);
        BinaryPrimitives.WriteInt32BigEndian(aad.Slice(16, sizeof(int)), chunkIndex);
        aad[20] = isFinal ? (byte)1 : (byte)0;
    }
}

/// <summary>An encrypted attachment and the SHA-256 of its plaintext.</summary>
public sealed record SealedAttachment(byte[] Payload, string Sha256);
