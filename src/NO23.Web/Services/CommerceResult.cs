namespace NO23.Web.Services;

public record CommerceResult(bool Succeeded, string? ErrorMessage, int? EntityId = null)
{
    public static CommerceResult Ok(int? entityId = null)
    {
        return new CommerceResult(true, null, entityId);
    }

    public static CommerceResult Fail(string message)
    {
        return new CommerceResult(false, message);
    }
}
