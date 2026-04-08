using MpQr.Api.Dtos;

namespace MpQr.Api.Services.Interfaces
{
    public interface IPaymentGateway
    {
        Task<CreatePaymentResponseDto>  CreatePaymentAsync(decimal amount, string mode = "web");
        Task<PaymentStatusResponseDto>  GetStatusAsync(string externalReference);
        Task<CancelPaymentResponseDto>  CancelAsync(string externalReference);

        /// <summary>
        /// Persiste el resultado del webhook en la tabla correspondiente (web o store).
        /// El statusDetail ya fue obtenido por el caller (PaymentsController)
        /// para evitar una segunda llamada al SDK de MercadoPago.
        /// </summary>
        Task ProcessWebhookAsync(
            string externalReference,
            string status,
            string statusDetail,
            string mercadoPagoPaymentId);
    }
}
