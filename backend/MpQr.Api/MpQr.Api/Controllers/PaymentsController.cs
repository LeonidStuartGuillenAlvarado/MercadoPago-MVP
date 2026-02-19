using MpQr.Api.Dtos;
using MpQr.Api.Security;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MpQr.Api.Hubs;
using MpQr.Api.Services.Interfaces;
using MercadoPago.Client.Payment;

namespace MpQr.Api.Controllers
{
    [ApiController]
    [Route("api/payments")]
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentGateway _gateway;
        private readonly IHubContext<PaymentHub> _hub;

        public PaymentsController(
            IPaymentGateway gateway,
            IHubContext<PaymentHub> hub)
        {
            _gateway = gateway;
            _hub = hub;
        }

        // ===============================
        // CREATE PAYMENT
        // ===============================
        [HttpPost]
        public async Task<IActionResult> Create(CreatePaymentRequestDto dto)
        {
            var result = await _gateway.CreatePaymentAsync(dto.Amount);
            return Ok(result);
        }

        // ===============================
        // GET STATUS
        // ===============================
        [HttpGet("{externalReference}/status")]
        public async Task<IActionResult> Status(string externalReference)
        {
            var status = await _gateway.GetStatusAsync(externalReference);
            return Ok(status);
        }

        // ===============================
        // CANCEL PAYMENT
        // ===============================
        [HttpPost("{externalReference}/cancel")]
        public async Task<IActionResult> Cancel(string externalReference)
        {
            var result = await _gateway.CancelAsync(externalReference);

            await _hub.Clients.All.SendAsync("PaymentUpdated", new
            {
                externalReference,
                status = result.Status
            });

            return Ok(result);
        }

        // ===============================
        // WEBHOOK (MercadoPago)
        // ===============================
        [HttpPost("webhook")]
        public async Task<IActionResult> Webhook()
        {
            try
            {
                // ==========================================
                // 1️⃣ SOPORTE FEED v2 (querystring)
                // ==========================================
                if (Request.Query.ContainsKey("id") &&
                    Request.Query.ContainsKey("topic"))
                {
                    var topic = Request.Query["topic"].ToString();
                    var id = Request.Query["id"].ToString();

                    if (topic == "payment" &&
                        long.TryParse(id, out long paymentIdFromQuery))
                    {
                        await ProcessPaymentAsync(paymentIdFromQuery);
                    }

                    return Ok();
                }

                // ==========================================
                // 2️⃣ WEBHOOK v1 (body JSON)
                // ==========================================
                using var reader = new StreamReader(Request.Body);
                var body = await reader.ReadToEndAsync();

                // Logging seguro (nunca rompe el webhook)
                try
                {
                    var logPath = Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "webhook-log.txt"
                    );

                    await System.IO.File.AppendAllTextAsync(
                        logPath,
                        $"\n\n{DateTime.UtcNow}\n{body}\n"
                    );
                }
                catch
                {
                    // Nunca romper webhook por logging
                }

                if (string.IsNullOrWhiteSpace(body))
                    return Ok();

                var webhook = JsonSerializer.Deserialize<MercadoPagoWebhookRequestDto>(
                    body,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }
                );

                if (webhook?.Type != "payment" ||
                    webhook?.Data?.Id == null)
                    return Ok();

                if (!long.TryParse(webhook.Data.Id, out long paymentId))
                    return Ok();

                await ProcessPaymentAsync(paymentId);

                return Ok();
            }
            catch
            {
                // NUNCA devolver 500 a MercadoPago
                return Ok();
            }
        }

        // ==========================================
        // LÓGICA CENTRALIZADA DE PROCESAMIENTO
        // ==========================================
        private async Task ProcessPaymentAsync(long paymentId)
        {
            var client = new PaymentClient();
            var payment = await client.GetAsync(paymentId);

            if (payment == null ||
                string.IsNullOrEmpty(payment.ExternalReference))
                return;

            var externalRef = payment.ExternalReference;
            var newStatus = payment.Status;

            var currentStatusDto = await _gateway.GetStatusAsync(externalRef);

            if (currentStatusDto != null &&
                currentStatusDto.Status == newStatus)
                return; // No hubo cambio real

            await _gateway.ProcessWebhookAsync(
                externalRef,
                newStatus,
                paymentId.ToString()
            );

            await _hub.Clients.All.SendAsync("PaymentUpdated", new
            {
                externalReference = externalRef,
                status = newStatus
            });
        }

    }
}
