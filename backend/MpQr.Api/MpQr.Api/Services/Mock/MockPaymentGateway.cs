//using Microsoft.AspNetCore.SignalR;
//using MpQr.Api.Dtos;
//using MpQr.Api.Hubs;
//using MpQr.Api.Models;
//using MpQr.Api.Persistence;
//using MpQr.Api.Services.Interfaces;

//namespace MpQr.Api.Services.Mock
//{
//    public class MockPaymentGateway : IPaymentGateway
//    {
//        private readonly IConfiguration _config;
//        private readonly IHubContext<PaymentHub> _hub;
//        private readonly PaymentRepository _repo;

//        public MockPaymentGateway(
//            IConfiguration config,
//            PaymentRepository repo,
//            IHubContext<PaymentHub> hub)
//        {
//            _config = config;
//            _repo = repo;
//            _hub = hub;
//        }

//        public async Task<CreatePaymentResponseDto> CreatePaymentAsync(decimal amount)
//        {
//            var externalRef = $"MOCK-{Guid.NewGuid():N}";

//            var payment = new Payment
//            {
//                ExternalReference = externalRef,
//                Status = "pending",
//                Amount = amount
//            };

//            await _repo.InsertAsync(payment);

//            // 🔥 Simulación async tipo webhook real
//            _ = Task.Run(async () =>
//            {
//                await Task.Delay(TimeSpan.FromSeconds(15));

//                var currentStatus = await _repo.GetStatusAsync(externalRef);

//                // NO sobrescribir si ya es terminal
//                if (IsTerminal(currentStatus))
//                    return;

//                var finalStatus = Random.Shared.Next(0, 100) < 70
//                    ? "approved"
//                    : "rejected";

//                await _repo.UpdateStatusAsync(externalRef, finalStatus);

//                // 🔥 PUSH en tiempo real
//                await _hub.Clients.All.SendAsync("PaymentUpdated", new
//                {
//                    externalReference = externalRef,
//                    status = finalStatus
//                });
//            });

//            return new CreatePaymentResponseDto
//            {
//                ExternalReference = externalRef,
//                QrCode = "https://api.qrserver.com/v1/create-qr-code/?size=300x300&data=" + externalRef,
//                Status = "pending"
//            };
//        }

//        public async Task<PaymentStatusResponseDto> GetStatusAsync(string externalReference)
//        {
//            var status = await _repo.GetStatusAsync(externalReference);

//            return new PaymentStatusResponseDto
//            {
//                Status = status
//            };
//        }

//        public async Task<string> CancelAsync(string externalReference)
//        {
//            var currentStatus = await _repo.GetStatusAsync(externalReference);

//            // Si ya es terminal, no hacer nada
//            if (IsTerminal(currentStatus))
//                return currentStatus;

//            await _repo.UpdateStatusAsync(externalReference, "cancelled");

//            // 🔥 PUSH inmediato
//            await _hub.Clients.All.SendAsync("PaymentUpdated", new
//            {
//                externalReference,
//                status = "cancelled"
//            });

//            return "cancelled";
//        }

//        public async Task ProcessWebhookAsync(string externalReference, string status)
//        {
//            var current = await _repo.GetStatusAsync(externalReference);

//            // No sobrescribir estados finales
//            if (IsTerminal(current))
//                return;

//            await _repo.UpdateStatusAsync(externalReference, status);

//            // 🔥 PUSH desde webhook
//            await _hub.Clients.All.SendAsync("PaymentUpdated", new
//            {
//                externalReference,
//                status
//            });
//        }

//        private bool IsTerminal(string? status)
//        {
//            if (string.IsNullOrWhiteSpace(status))
//                return false;

//            return status.ToLower() is "approved" or "rejected" or "cancelled";
//        }
//    }
//}
