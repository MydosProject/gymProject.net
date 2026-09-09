namespace NO23.Web.Services;

public static class ProgressPhotoValidation
{
    public const int MaxBytes = 5 * 1024 * 1024;
    public static string? ContentType(byte[] bytes)
    {
        if (bytes.Length is < 12 or > MaxBytes) return null;
        if (bytes.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })) return "image/png";
        if (bytes[0] == 255 && bytes[1] == 216 && bytes[2] == 255 && bytes[^2] == 255 && bytes[^1] == 217) return "image/jpeg";
        return null;
    }
}
