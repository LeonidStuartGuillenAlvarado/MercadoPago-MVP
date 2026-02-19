namespace MpQr.Api.Models
{
    public class Payment
    {
        public int Id { get; set; }
        public string ExternalReference { get; set; } = default!;
        public string Status { get; set; } = default!;
        public decimal Amount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // NUEVO: id del pago de MercadoPago
        public string? MercadoPagoPaymentId { get; set; }
    }
}
