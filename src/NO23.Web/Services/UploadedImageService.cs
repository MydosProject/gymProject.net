using System.Buffers.Binary;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Domain.Entities;

namespace NO23.Web.Services;

public sealed record ImageFileInfo(string ContentType, int Width, int Height);

public sealed record ImageSaveResult(bool Succeeded, string? Url, string? Error)
{
    public static ImageSaveResult Ok(string url) => new(true, url, null);
    public static ImageSaveResult Fail(string error) => new(false, null, error);
}

// Stores admin photo uploads in the database (they survive deployments) and
// removes uploads that no content uses any more.
public sealed partial class UploadedImageService(ApplicationDbContext db)
{
    // The admin page shrinks photos in the browser first (longest side 1600 px,
    // usually 150-400 KB), so this limit only stops very large originals.
    public const int MaxBytes = 1536 * 1024;
    public const long MaxRequestBytes = 2 * 1024 * 1024;
    public const int MaxDimension = 4096;
    public const string MaxBytesText = "1,5 MB";

    // Every column that can hold a /media link. Cleanup keeps any image these use.
    public static readonly IReadOnlyList<string> ReferencingProperties =
    [
        "BlogPost.CoverImageUrl",
        "CommunityChallenge.ImageUrl",
        "CommunityEvent.ImageUrl",
        "KitchenMenuItem.ImageUrl",
        "ShopProduct.ImageUrl",
        "SuccessStory.AfterImageUrl",
        "SuccessStory.BeforeImageUrl"
    ];

    private const string MediaMarker = "/media/";
    private static readonly byte[] PngSignature = [137, 80, 78, 71, 13, 10, 26, 10];

    public static string UrlFor(Guid id) => $"{MediaMarker}{id:N}";

    public async Task<ImageSaveResult> SaveAsync(byte[] data, string? actor, CancellationToken cancellationToken = default)
    {
        if (data.Length == 0)
        {
            return ImageSaveResult.Fail("Fotoğraf seçilmedi.");
        }

        if (data.Length > MaxBytes)
        {
            return ImageSaveResult.Fail($"Fotoğraf en fazla {MaxBytesText} olabilir.");
        }

        var info = Inspect(data);
        if (info is null)
        {
            return ImageSaveResult.Fail("Yalnız JPEG, PNG veya WebP fotoğraf yüklenebilir.");
        }

        if (info.Width > MaxDimension || info.Height > MaxDimension)
        {
            return ImageSaveResult.Fail($"Fotoğrafın kenarı en fazla {MaxDimension} piksel olabilir.");
        }

        var image = new UploadedImage
        {
            Id = Guid.NewGuid(),
            ContentType = info.ContentType,
            Data = data,
            ByteLength = data.Length,
            Width = info.Width,
            Height = info.Height,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedBy = actor
        };
        db.UploadedImages.Add(image);
        await db.SaveChangesAsync(cancellationToken);

        // Uploads are rare, so tidy up here instead of running a background job.
        await RemoveUnusedAsync(DateTime.UtcNow.AddDays(-1), cancellationToken);

        return ImageSaveResult.Ok(UrlFor(image.Id));
    }

    // Deletes uploads older than the cutoff that no saved content links to,
    // e.g. a photo uploaded and then replaced or never saved.
    public async Task<int> RemoveUnusedAsync(DateTime olderThanUtc, CancellationToken cancellationToken = default)
    {
        var candidates = await db.UploadedImages
            .Where(image => image.CreatedAtUtc < olderThanUtc)
            .Select(image => image.Id)
            .ToListAsync(cancellationToken);
        if (candidates.Count == 0)
        {
            return 0;
        }

        var used = await ReferencedIdsAsync(cancellationToken);
        var unused = candidates.Where(id => !used.Contains(id)).ToList();
        if (unused.Count == 0)
        {
            return 0;
        }

        var tracked = await db.UploadedImages
            .Where(image => unused.Contains(image.Id))
            .ToListAsync(cancellationToken);
        db.UploadedImages.RemoveRange(tracked);
        await db.SaveChangesAsync(cancellationToken);
        return tracked.Count;
    }

    private async Task<HashSet<Guid>> ReferencedIdsAsync(CancellationToken cancellationToken)
    {
        var links = new List<string?>();
        links.AddRange(await db.BlogPosts.Where(x => x.CoverImageUrl!.Contains(MediaMarker)).Select(x => x.CoverImageUrl).ToListAsync(cancellationToken));
        links.AddRange(await db.CommunityChallenges.Where(x => x.ImageUrl!.Contains(MediaMarker)).Select(x => x.ImageUrl).ToListAsync(cancellationToken));
        links.AddRange(await db.CommunityEvents.Where(x => x.ImageUrl!.Contains(MediaMarker)).Select(x => x.ImageUrl).ToListAsync(cancellationToken));
        links.AddRange(await db.KitchenMenuItems.Where(x => x.ImageUrl!.Contains(MediaMarker)).Select(x => x.ImageUrl).ToListAsync(cancellationToken));
        links.AddRange(await db.ShopProducts.Where(x => x.ImageUrl!.Contains(MediaMarker)).Select(x => x.ImageUrl).ToListAsync(cancellationToken));
        links.AddRange(await db.SuccessStories.Where(x => x.AfterImageUrl!.Contains(MediaMarker)).Select(x => x.AfterImageUrl).ToListAsync(cancellationToken));
        links.AddRange(await db.SuccessStories.Where(x => x.BeforeImageUrl!.Contains(MediaMarker)).Select(x => x.BeforeImageUrl).ToListAsync(cancellationToken));

        var ids = new HashSet<Guid>();
        foreach (var link in links)
        {
            if (link is not null && MediaIdPattern().Match(link) is { Success: true } match &&
                Guid.TryParse(match.Groups[1].Value, out var id))
            {
                ids.Add(id);
            }
        }

        return ids;
    }

    // Reads the real file type and size from the file header; the browser-sent type is not trusted.
    public static ImageFileInfo? Inspect(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 30)
        {
            return null;
        }

        if (bytes[..8].SequenceEqual(PngSignature))
        {
            if (!bytes.Slice(12, 4).SequenceEqual("IHDR"u8))
            {
                return null;
            }

            return Valid("image/png",
                BinaryPrimitives.ReadInt32BigEndian(bytes.Slice(16, 4)),
                BinaryPrimitives.ReadInt32BigEndian(bytes.Slice(20, 4)));
        }

        if (bytes[..4].SequenceEqual("RIFF"u8) && bytes.Slice(8, 4).SequenceEqual("WEBP"u8))
        {
            var chunk = bytes.Slice(12, 4);
            if (chunk.SequenceEqual("VP8X"u8))
            {
                return Valid("image/webp",
                    1 + (bytes[24] | bytes[25] << 8 | bytes[26] << 16),
                    1 + (bytes[27] | bytes[28] << 8 | bytes[29] << 16));
            }

            if (chunk.SequenceEqual("VP8L"u8))
            {
                if (bytes[20] != 0x2F)
                {
                    return null;
                }

                var bits = BinaryPrimitives.ReadUInt32LittleEndian(bytes.Slice(21, 4));
                return Valid("image/webp", (int)(bits & 0x3FFF) + 1, (int)((bits >> 14) & 0x3FFF) + 1);
            }

            if (chunk.SequenceEqual("VP8 "u8))
            {
                if (bytes[23] != 0x9D || bytes[24] != 0x01 || bytes[25] != 0x2A)
                {
                    return null;
                }

                return Valid("image/webp",
                    BinaryPrimitives.ReadUInt16LittleEndian(bytes.Slice(26, 2)) & 0x3FFF,
                    BinaryPrimitives.ReadUInt16LittleEndian(bytes.Slice(28, 2)) & 0x3FFF);
            }

            return null;
        }

        if (bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF)
        {
            return InspectJpeg(bytes);
        }

        return null;
    }

    private static ImageFileInfo? InspectJpeg(ReadOnlySpan<byte> bytes)
    {
        var index = 2;
        while (index + 8 < bytes.Length)
        {
            if (bytes[index] != 0xFF)
            {
                return null;
            }

            var marker = bytes[index + 1];
            if (marker == 0xFF)
            {
                index++;
                continue;
            }

            if (marker is 0x01 or (>= 0xD0 and <= 0xD8))
            {
                index += 2;
                continue;
            }

            if (marker == 0xDA)
            {
                return null;
            }

            var length = bytes[index + 2] << 8 | bytes[index + 3];
            if (length < 2)
            {
                return null;
            }

            // Start-of-frame markers carry the size; C4, C8 and CC are other tables.
            if (marker is >= 0xC0 and <= 0xCF and not 0xC4 and not 0xC8 and not 0xCC)
            {
                return Valid("image/jpeg",
                    bytes[index + 7] << 8 | bytes[index + 8],
                    bytes[index + 5] << 8 | bytes[index + 6]);
            }

            index += 2 + length;
        }

        return null;
    }

    private static ImageFileInfo? Valid(string contentType, int width, int height) =>
        width > 0 && height > 0 ? new ImageFileInfo(contentType, width, height) : null;

    [GeneratedRegex(@"/media/([0-9a-fA-F]{32}|[0-9a-fA-F-]{36})")]
    private static partial Regex MediaIdPattern();
}
