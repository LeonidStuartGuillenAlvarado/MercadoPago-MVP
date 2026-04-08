namespace MpQr.Api.Models
{
    public class Payment
    {
        public int      Id                      { get; set; }
        public string   ExternalReference       { get; set; } = default!;
        public string   Status                  { get; set; } = PaymentStatus.Pending;
        public string?  StatusDetail            { get; set; }
        public decimal  Amount                  { get; set; }
        public string?  MercadoPagoPaymentId    { get; set; }
        public DateTime CreatedAt               { get; set; }
        public DateTime? UpdatedAt              { get; set; }
    }
}
