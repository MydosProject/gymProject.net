using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Areas.Admin.Controllers;
using NO23.Web.Controllers;
using NO23.Web.Data;
using NO23.Web.Domain.Entities;
using NO23.Web.Services;

namespace NO23.Tests;

public class UploadedImageTests
{
    [Fact]
    public void Inspect_ReadsTypeAndSizeFromTheFileHeader()
    {
        Assert.Equal(new ImageFileInfo("image/png", 640, 480), UploadedImageService.Inspect(Png(640, 480)));
        Assert.Equal(new ImageFileInfo("image/jpeg", 1600, 900), UploadedImageService.Inspect(Jpeg(1600, 900)));
        Assert.Equal(new ImageFileInfo("image/webp", 1600, 1067), UploadedImageService.Inspect(WebpExtended(1600, 1067)));
        Assert.Equal(new ImageFileInfo("image/webp", 800, 600), UploadedImageService.Inspect(WebpLossless(800, 600)));
        Assert.Equal(new ImageFileInfo("image/webp", 1200, 675), UploadedImageService.Inspect(WebpLossy(1200, 675)));
    }

    [Theory]
    [InlineData("GIF89a")]
    [InlineData("<svg xmlns=\"http://www.w3.org/2000/svg\"><script>alert(1)</script></svg>")]
    [InlineData("not an image at all, just some plain text")]
    public void Inspect_RejectsOtherFiles(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content.PadRight(40, ' '));
        Assert.Null(UploadedImageService.Inspect(bytes));
    }

    [Fact]
    public async Task Save_StoresThePhotoAndReturnsItsLink()
    {
        await using var db = Db();
        var service = new UploadedImageService(db);

        var result = await service.SaveAsync(WebpExtended(1600, 1067), "admin@no23.test");

        Assert.True(result.Succeeded);
        var image = await db.UploadedImages.SingleAsync();
        Assert.Equal(UploadedImageService.UrlFor(image.Id), result.Url);
        Assert.Equal(("image/webp", 1600, 1067, "admin@no23.test"), (image.ContentType, image.Width, image.Height, image.CreatedBy));
    }

    [Fact]
    public async Task Save_RejectsLargeFilesHugePhotosAndNonImages()
    {
        await using var db = Db();
        var service = new UploadedImageService(db);
        var tooBig = Png(100, 100).Concat(new byte[UploadedImageService.MaxBytes]).ToArray();

        Assert.Contains("1,5 MB", (await service.SaveAsync(tooBig, null)).Error);
        Assert.Contains("4096", (await service.SaveAsync(Png(5000, 3000), null)).Error);
        Assert.Contains("JPEG, PNG veya WebP", (await service.SaveAsync(Encoding.UTF8.GetBytes("GIF89a".PadRight(40)), null)).Error);
        Assert.Empty(db.UploadedImages);
    }

    [Fact]
    public async Task RemoveUnused_DeletesOnlyOldPhotosThatNothingLinksTo()
    {
        await using var db = Db();
        var old = DateTime.UtcNow.AddDays(-3);
        var unused = Image(old);
        var usedByKitchen = Image(old);
        var usedByStory = Image(old);
        var fresh = Image(DateTime.UtcNow);
        db.UploadedImages.AddRange(unused, usedByKitchen, usedByStory, fresh);
        db.KitchenMenuItems.Add(new KitchenMenuItem
        {
            Name = "Bowl",
            Ingredients = "Tavuk",
            ImageUrl = "http://213.254.136.245:5044" + UploadedImageService.UrlFor(usedByKitchen.Id)
        });
        db.SuccessStories.Add(new SuccessStory { AfterImageUrl = $"/media/{usedByStory.Id:D}" });
        await db.SaveChangesAsync();

        var removed = await new UploadedImageService(db).RemoveUnusedAsync(DateTime.UtcNow.AddDays(-1));

        Assert.Equal(1, removed);
        Assert.Equal(
            new[] { usedByKitchen.Id, usedByStory.Id, fresh.Id }.Order(),
            (await db.UploadedImages.Select(x => x.Id).ToListAsync()).Order());
    }

    [Fact]
    public void Cleanup_KnowsEveryImageLinkColumn()
    {
        // A new *ImageUrl column must be added to ReferencingProperties, or cleanup would delete its photos.
        using var db = Db();
        var columns = db.Model.GetEntityTypes()
            .SelectMany(entity => entity.GetProperties()
                .Where(property => property.ClrType == typeof(string) && property.Name.EndsWith("ImageUrl"))
                .Select(property => $"{entity.ClrType.Name}.{property.Name}"))
            .Order()
            .ToList();

        Assert.Equal(UploadedImageService.ReferencingProperties.Order(), columns);
    }

    [Fact]
    public async Task UploadThenServe_RoundTripsWithLongCaching()
    {
        await using var db = Db();
        var upload = new MediaController(new UploadedImageService(db))
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        var bytes = Jpeg(1200, 800);
        using var stream = new MemoryStream(bytes);
        var file = new FormFile(stream, 0, bytes.Length, "file", "photo.jpg") { Headers = new HeaderDictionary(), ContentType = "image/jpeg" };

        var json = Assert.IsType<JsonResult>(await upload.Upload(file, CancellationToken.None));
        var url = (string)json.Value!.GetType().GetProperty("url")!.GetValue(json.Value)!;
        var id = Guid.Parse(url["/media/".Length..]);

        var serve = new MediaFilesController(db)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        var served = Assert.IsType<FileContentResult>(await serve.Get(id, CancellationToken.None));
        Assert.Equal("image/jpeg", served.ContentType);
        Assert.Equal(bytes, served.FileContents);
        Assert.Contains("immutable", serve.Response.Headers.CacheControl.ToString());
        Assert.Equal("nosniff", serve.Response.Headers.XContentTypeOptions.ToString());
        Assert.IsType<NotFoundResult>(await serve.Get(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task Upload_RejectsAnEmptyOrTooLargeFile()
    {
        await using var db = Db();
        var upload = new MediaController(new UploadedImageService(db))
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        var big = new byte[UploadedImageService.MaxBytes + 1];
        using var stream = new MemoryStream(big);

        Assert.IsType<BadRequestObjectResult>(await upload.Upload(null, CancellationToken.None));
        Assert.IsType<BadRequestObjectResult>(await upload.Upload(
            new FormFile(stream, 0, big.Length, "file", "big.jpg"), CancellationToken.None));
        Assert.Empty(db.UploadedImages);
    }

    private static UploadedImage Image(DateTime createdAtUtc) => new()
    {
        Id = Guid.NewGuid(),
        ContentType = "image/png",
        Data = Png(10, 10),
        ByteLength = 33,
        Width = 10,
        Height = 10,
        CreatedAtUtc = createdAtUtc
    };

    private static byte[] Png(int width, int height)
    {
        var bytes = new List<byte> { 137, 80, 78, 71, 13, 10, 26, 10, 0, 0, 0, 13 };
        bytes.AddRange("IHDR"u8.ToArray());
        bytes.AddRange(BigEndian(width));
        bytes.AddRange(BigEndian(height));
        bytes.AddRange(new byte[] { 8, 6, 0, 0, 0, 0, 0, 0, 0 });
        return [.. bytes];
    }

    private static byte[] Jpeg(int width, int height)
    {
        var bytes = new List<byte> { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10 };
        bytes.AddRange("JFIF\0"u8.ToArray());
        bytes.AddRange(new byte[] { 1, 1, 0, 0, 1, 0, 1, 0, 0 });
        bytes.AddRange(new byte[] { 0xFF, 0xC0, 0x00, 0x11, 0x08, (byte)(height >> 8), (byte)height, (byte)(width >> 8), (byte)width });
        bytes.AddRange(new byte[] { 3, 1, 0x22, 0, 2, 0x11, 1, 3, 0x11, 1, 0xFF, 0xD9 });
        return [.. bytes];
    }

    private static byte[] WebpExtended(int width, int height)
    {
        var bytes = RiffHeader("VP8X", 10);
        bytes.AddRange(new byte[] { 0, 0, 0, 0 });
        bytes.AddRange(LittleEndian24(width - 1));
        bytes.AddRange(LittleEndian24(height - 1));
        return [.. bytes];
    }

    private static byte[] WebpLossless(int width, int height)
    {
        var bytes = RiffHeader("VP8L", 10);
        var bits = (uint)(width - 1) | (uint)(height - 1) << 14;
        bytes.Add(0x2F);
        bytes.AddRange(BitConverter.GetBytes(bits));
        bytes.AddRange(new byte[5]);
        return [.. bytes];
    }

    private static byte[] WebpLossy(int width, int height)
    {
        var bytes = RiffHeader("VP8 ", 10);
        bytes.AddRange(new byte[] { 0, 0, 0, 0x9D, 0x01, 0x2A });
        bytes.AddRange(BitConverter.GetBytes((ushort)width));
        bytes.AddRange(BitConverter.GetBytes((ushort)height));
        return [.. bytes];
    }

    private static List<byte> RiffHeader(string chunk, int chunkSize)
    {
        var bytes = new List<byte>();
        bytes.AddRange("RIFF"u8.ToArray());
        bytes.AddRange(BitConverter.GetBytes(chunkSize + 12));
        bytes.AddRange("WEBP"u8.ToArray());
        bytes.AddRange(Encoding.ASCII.GetBytes(chunk));
        bytes.AddRange(BitConverter.GetBytes(chunkSize));
        return bytes;
    }

    private static byte[] BigEndian(int value) => [(byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)value];

    private static byte[] LittleEndian24(int value) => [(byte)value, (byte)(value >> 8), (byte)(value >> 16)];

    private static ApplicationDbContext Db() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}
