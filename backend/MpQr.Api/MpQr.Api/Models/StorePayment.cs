namespace MpQr.Api.Models
{
    public class StorePayment
    {
        public int      Id                      { get; set; }
        public string   ExternalReference       { get; set; } = default!;
        public string   Status                  { get; set; } = PaymentStatus.Pending;
        public string?  StatusDetail            { get; set; }
        public decimal  Amount                  { get; set; }
        public bool     IsEnabled               { get; set; }
        public string   CheckoutUrl             { get; set; } = default!;
        public string?  MercadoPagoPaymentId    { get; set; }
        public DateTime CreatedAt               { get; set; }
        public DateTime? UpdatedAt              { get; set; }
    }
}
