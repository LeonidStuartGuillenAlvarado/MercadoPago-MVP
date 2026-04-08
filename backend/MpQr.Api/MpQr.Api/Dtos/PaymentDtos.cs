namespace MpQr.Api.Dtos
{
    public class CreatePaymentRequestDto
    {
        public decimal  Amount  { get; set; }
        public string?  Mode    { get; set; }
    }

    public class CreatePaymentResponseDto
    {
        public string ExternalReference { get; set; } = default!;
        public string QrCode            { get; set; } = default!;
        public string Status            { get; set; } = "pending";
    }

    public class PaymentStatusResponseDto
    {
        public string Status { get; set; } = default!;
    }

    public class CancelPaymentResponseDto
    {
        public string Status { get; set; } = "cancelled";
    }
}
