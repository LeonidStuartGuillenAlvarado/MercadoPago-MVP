using MercadoPago.Client.Payment;
using MpQr.Api.Dtos;
using MpQr.Api.Hubs;
using MpQr.Api.Models;
using MpQr.Api.Security;
using MpQr.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Text.Json;

namespace MpQr.Api.Controllers
{
    [ApiController]
    [Route("api/payments")]
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentGateway                _gateway;
        private readonly IHubContext<PaymentHub>        _hub;
        private readonly MercadoPagoSignatureValidator  _signatureValidator;
        private readonly ILogger<PaymentsController>    _logger;

        public PaymentsController(
            IPaymentGateway gateway,
            IHubContext<PaymentHub> hub,
            MercadoPagoSignatureValidator signatureValidator,
            ILogger<PaymentsController> logger)
        {
            _gateway            = gateway;
            _hub                = hub;
            _signatureValidator = signatureValidator;
            _logger             = logger;
        }

        // ── POST /api/payments ────────────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreatePaymentRequestDto dto)
        {
            if (dto.Amount <= 0)
                return BadRequest(new { error = "El monto debe ser mayor a 0." });

            var mode   = dto.Mode ?? "web";
            var result = await _gateway.CreatePaymentAsync(dto.Amount, mode);
            return Ok(result);
        }

        // ── GET /api/payments/{externalReference}/status ─────────────────────
        [HttpGet("{externalReference}/status")]
        public async Task<IActionResult> Status(string externalReference)
        {
            var status = await _gateway.GetStatusAsync(externalReference);
            return Ok(status);
        }

        // ── POST /api/payments/{externalReference}/cancel ─────────────────────
        [HttpPost("{externalReference}/cancel")]
        public async Task<IActionResult> Cancel(string externalReference)
        {
            var result = await _gateway.CancelAsync(externalReference);

            await _hub.Clients.All.SendAsync("PaymentUpdated", new
            {
                externalReference,
                status       = result.Status,
                statusDetail = (string?)null
            });

            return Ok(result);
        }

        // ── POST /api/payments/webhook ────────────────────────────────────────
        [HttpPost("webhook")]
        public async Task<IActionResult> Webhook()
        {
            try
            {
                // ── Feed v2 (querystring: ?id=X&topic=payment) ────────────────
                if (Request.Query.ContainsKey("id") && Request.Query.ContainsKey("topic"))
                {
                    var topic = Request.Query["topic"].ToString();
                    var id    = Request.Query["id"].ToString();

                    _logger.LogInformation(
                        "Webhook v2 recibido: topic={Topic} id={Id}", topic, id);

                    if (topic == "payment" && long.TryParse(id, out long qsPaymentId))
                    {
                        var qsSig       = Request.Headers["x-signature"].ToString();
                        var qsRequestId = Request.Headers["x-request-id"].ToString();
                        if (!_signatureValidator.Validate(qsSig, id, qsRequestId))
                        {
                            _logger.LogWarning(
                                "Webhook v2: firma inválida para id={Id}. " +
                                "Verificar que WebhookSecret en appsettings coincida con el configurado en MercadoPago.", id);
                            return Ok();
                        }

                        await ProcessPaymentAsync(qsPaymentId);
                    }
                    return Ok();
                }

                // ── Webhook v1 (body JSON) ────────────────────────────────────
                using var reader = new StreamReader(Request.Body);
                var body = await reader.ReadToEndAsync();

                if (string.IsNullOrWhiteSpace(body))
                {
                    _logger.LogWarning("Webhook v1: body vacío recibido.");
                    return Ok();
                }

                _logger.LogInformation("Webhook v1 recibido: {Body}", body);

                var webhook = JsonSerializer.Deserialize<MercadoPagoWebhookRequestDto>(
                    body,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (webhook?.Type != "payment" || webhook.Data?.Id == null)
                {
                    _logger.LogInformation(
                        "Webhook v1 ignorado: type={Type} dataId={DataId}",
                        webhook?.Type, webhook?.Data?.Id);
                    return Ok();
                }

                // ── Validar firma ─────────────────────────────────────────────
                var signature  = Request.Headers["x-signature"].ToString();
                var requestId  = Request.Headers["x-request-id"].ToString();
                if (!_signatureValidator.Validate(signature, webhook.Data.Id, requestId))
                {
                    _logger.LogWarning(
                        "Webhook v1: firma inválida para dataId={Id}. " +
                        "Verificar que WebhookSecret en appsettings coincida con el configurado en MercadoPago.",
                        webhook.Data.Id);
                    return Ok();
                }

                if (!long.TryParse(webhook.Data.Id, out long paymentId))
                {
                    _logger.LogWarning("Webhook v1: data.id no es un long válido: {Id}", webhook.Data.Id);
                    return Ok();
                }

                await ProcessPaymentAsync(paymentId);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando webhook.");
                return Ok();
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // LÓGICA CENTRALIZADA DE PROCESAMIENTO
        // ─────────────────────────────────────────────────────────────────────
        private async Task ProcessPaymentAsync(long paymentId)
        {
            _logger.LogInformation("ProcessPayment: consultando SDK para paymentId={PaymentId}", paymentId);

            var client  = new PaymentClient();
            var payment = await client.GetAsync(paymentId);

            if (payment == null)
            {
                _logger.LogWarning("ProcessPayment: MP devolvió null para paymentId={PaymentId}", paymentId);
                return;
            }

            if (string.IsNullOrEmpty(payment.ExternalReference))
            {
                _logger.LogWarning(
                    "ProcessPayment: ExternalReference vacío para paymentId={PaymentId}. " +
                    "El pago puede no haber sido creado desde esta aplicación.", paymentId);
                return;
            }

            var externalRef  = payment.ExternalReference;
            var newStatus    = payment.Status    ?? PaymentStatus.Pending;
            var statusDetail = payment.StatusDetail ?? string.Empty;

            _logger.LogInformation(
                "ProcessPayment: paymentId={PaymentId} ref={Ref} status={Status} detail={Detail}",
                paymentId, externalRef, newStatus, statusDetail);

            var currentDto = await _gateway.GetStatusAsync(externalRef);

            if (currentDto != null && PaymentStatus.IsFinal(currentDto.Status))
            {
                _logger.LogInformation(
                    "ProcessPayment: {Ref} ya está en estado final ({Status}), ignorando.",
                    externalRef, currentDto.Status);
                return;
            }

            if (currentDto?.Status == newStatus)
            {
                _logger.LogInformation(
                    "ProcessPayment: {Ref} ya tiene el mismo estado ({Status}), ignorando.",
                    externalRef, newStatus);
                return;
            }

            await _gateway.ProcessWebhookAsync(
                externalRef,
                newStatus,
                statusDetail,
                paymentId.ToString());

            _logger.LogInformation(
                "ProcessPayment: {Ref} actualizado a {Status}. Emitiendo SignalR.",
                externalRef, newStatus);

            await _hub.Clients.All.SendAsync("PaymentUpdated", new
            {
                externalReference = externalRef,
                status            = newStatus,
                statusDetail
            });
        }
    }
}
