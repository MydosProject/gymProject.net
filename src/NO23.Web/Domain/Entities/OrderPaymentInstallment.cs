namespace NO23.Web.Domain.Entities;

public class OrderPaymentInstallment
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public int Number { get; set; }
    public decimal Amount { get; set; }
    public DateOnly DueDate { get; set; }
    public DateOnly? PaidOn { get; set; }
    public string? PaymentReference { get; set; }
    public string? RecordedByUserId { get; set; }
}
