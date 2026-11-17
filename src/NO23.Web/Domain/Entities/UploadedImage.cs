namespace NO23.Web.Domain.Entities;

// A photo an admin uploaded from their computer, served at /media/{id}.
public class UploadedImage
{
    public Guid Id { get; set; }

    public string ContentType { get; set; } = string.Empty;

    public byte[] Data { get; set; } = [];

    public int ByteLength { get; set; }

    public int Width { get; set; }

    public int Height { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public string? CreatedBy { get; set; }
}
