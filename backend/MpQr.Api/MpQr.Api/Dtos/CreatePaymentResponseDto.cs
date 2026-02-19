namespace MpQr.Api.Dtos
{
    public class CreatePaymentResponseDto
    {
        public string ExternalReference { get; set; } = default!;
        public string QrCode { get; set; } = default!;
        public string Status { get; set; } = "pending";
    }
}
