namespace NO23.Web.Services;

public sealed class ClubPickupOptions
{
    public const string SectionName = "ClubPickup";

    public string DisplayName { get; set; } = "NO23 Sports Club";

    public string AddressLine { get; set; } = string.Empty;

    public string District { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public string? PostalCode { get; set; }

    public string EffectiveDisplayName =>
        string.IsNullOrWhiteSpace(DisplayName)
            ? "NO23 Sports Club"
            : DisplayName.Trim();

    public string EffectiveAddressLine =>
        string.IsNullOrWhiteSpace(AddressLine)
            ? EffectiveDisplayName
            : AddressLine.Trim();

    public string EffectiveDistrict =>
        string.IsNullOrWhiteSpace(District)
            ? EffectiveDisplayName
            : District.Trim();

    public string EffectiveCity =>
        string.IsNullOrWhiteSpace(City)
            ? EffectiveDisplayName
            : City.Trim();
}
