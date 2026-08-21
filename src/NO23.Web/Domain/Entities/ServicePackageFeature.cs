namespace NO23.Web.Domain.Entities;

public class ServicePackageFeature
{
    public int Id { get; set; }
    public int ServicePackageId { get; set; }
    public ServicePackage ServicePackage { get; set; } = null!;
    public string Text { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
}
