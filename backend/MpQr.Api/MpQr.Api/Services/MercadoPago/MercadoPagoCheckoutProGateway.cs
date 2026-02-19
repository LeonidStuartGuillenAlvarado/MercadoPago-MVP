//using MercadoPago.Client.Preference;
//using MercadoPago.Config;
//using MercadoPago.Resource.Preference;
//using Microsoft.AspNetCore.SignalR;
//using MpQr.Api.Dtos;
//using MpQr.Api.Hubs;
//using MpQr.Api.Models;
//using MpQr.Api.Persistence;
//using MpQr.Api.Services.Interfaces;

//namespace MpQr.Api.Services.MercadoPago
//{
//    public class MercadoPagoCheckoutProGateway : IPaymentGateway
//    {
//        private readonly IConfiguration _config;
//        private readonly PaymentRepository _repo;
//        private readonly IHubContext<PaymentHub> _hub;

//        public MercadoPagoCheckoutProGateway(
//            IConfiguration config,
//            PaymentRepository repo,
//            IHubContext<PaymentHub> hub)
//        {
//            _config = config;
//            _repo = repo;
//            _hub = hub;

//            MercadoPagoConfig.AccessToken =
//                _config["MercadoPago:AccessToken"];
//        }

//        public async Task<CreatePaymentResponseDto> CreatePaymentAsync(decimal amount)
//        {
//            var externalRef = $"MP-{Guid.NewGuid():N}";

//            var request = new PreferenceCreateRequest
//            {
//                Items = new List<PreferenceItemRequest>
//                {
//                    new PreferenceItemRequest
//                    {
//                        Title = "Pago MVP",
//                        Quantity = 1,
//                        UnitPrice = amount
//                    }
//                },
//                ExternalReference = externalRef,
//                NotificationUrl = "https://TU_NGROK_URL/api/payments/webhook",
//                BackUrls = new PreferenceBackUrlsRequest
//                {
//                    Success = "http://localhost:4200",
//                    Failure = "http://localhost:4200",
//                    Pending = "http://localhost:4200"
//                }
//            };

//            var client = new PreferenceClient();
//            Preference preference = await client.CreateAsync(request);

//            await _repo.InsertAsync(new Payment
//            {
//                ExternalReference = externalRef,
//                Status = "pending",
//                Amount = amount
//            });

//            return new CreatePaymentResponseDto
//            {
//                ExternalReference = externalRef,
//                QrCode = preference.InitPoint, // URL para redirigir
//                Status = "pending"
//            };
//        }

//        public async Task<PaymentStatusResponseDto> GetStatusAsync(string externalReference)
//        {
//            var status = await _repo.GetStatusAsync(externalReference);
//            return new PaymentStatusResponseDto { Status = status };
//        }

//        public async Task<string> CancelAsync(string externalReference)
//        {
//            await _repo.UpdateStatusAsync(externalReference, "cancelled");

//            await _hub.Clients.All.SendAsync("PaymentUpdated", new
//            {
//                externalReference,
//                status = "cancelled"
//            });

//            return "cancelled";
//        }

//        public async Task ProcessWebhookAsync(string externalReference, string status)
//        {
//            await _repo.UpdateStatusAsync(externalReference, status);

//            await _hub.Clients.All.SendAsync("PaymentUpdated", new
//            {
//                externalReference,
//                status
//            });
//        }
//    }
//}
