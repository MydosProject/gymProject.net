namespace NO23.Web.Services;

public class DeliveryDetails
{
    public string FullName { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    public string AddressLine { get; set; } = string.Empty;

    public string District { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public string? PostalCode { get; set; }

    public DateOnly? DeliveryDate { get; set; }

    public string? DeliveryTimeSlot { get; set; }

    public string? Notes { get; set; }
}
