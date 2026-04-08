using MercadoPago.Client.Preference;
using MercadoPago.Config;
using MpQr.Api.Dtos;
using MpQr.Api.Models;
using MpQr.Api.Persistence;
using MpQr.Api.Services.Interfaces;

namespace MpQr.Api.Services.MercadoPago
{
    public class MercadoPagoCheckoutApiGateway : IPaymentGateway
    {
        private readonly PaymentRepository      _repository;
        private readonly StorePaymentRepository _storeRepository;
        private readonly IConfiguration         _config;
        private readonly ILogger<MercadoPagoCheckoutApiGateway> _logger;

        public MercadoPagoCheckoutApiGateway(
            PaymentRepository repository,
            StorePaymentRepository storeRepository,
            IConfiguration config,
            ILogger<MercadoPagoCheckoutApiGateway> logger)
        {
            _repository      = repository;
            _storeRepository = storeRepository;
            _config          = config;
            _logger          = logger;

            // Configuración del SDK (una sola vez por instancia)
            MercadoPagoConfig.AccessToken = config["MercadoPago:AccessToken"]
                ?? throw new InvalidOperationException("MercadoPago:AccessToken no configurado.");
        }

        // ─────────────────────────────────────────────────────────────────────
        // CREAR PAGO
        // ─────────────────────────────────────────────────────────────────────
        public async Task<CreatePaymentResponseDto> CreatePaymentAsync(decimal amount, string mode = "web")
        {
            var isStore        = mode == "store";
            var prefix         = isStore ? "STORE" : "WEB";
            var externalRef    = $"{prefix}-{Guid.NewGuid()}";
            var title          = isStore ? "Pago Tienda Física" : "Compra Web";

            var request = new PreferenceRequest
            {
                Items = new List<PreferenceItemRequest>
                {
                    new()
                    {
                        Title      = title,
                        Quantity   = 1,
                        CurrencyId = "ARS",
                        UnitPrice  = amount
                    }
                },
                ExternalReference = externalRef,
                NotificationUrl   = $"{_config["App:BaseUrl"]}/api/payments/webhook",
                Expires           = true,
                ExpirationDateTo  = DateTime.UtcNow.AddMinutes(10)
            };

            var client     = new PreferenceClient();
            var preference = await client.CreateAsync(request);
            var checkoutUrl = preference.InitPoint ?? preference.SandboxInitPoint;

            if (isStore)
            {
                await _storeRepository.InsertAsync(new StorePayment
                {
                    ExternalReference = externalRef,
                    Status            = PaymentStatus.Pending,
                    Amount            = amount,
                    IsEnabled         = true,
                    CheckoutUrl       = checkoutUrl
                });
            }
            else
            {
                await _repository.InsertAsync(new Payment
                {
                    ExternalReference = externalRef,
                    Status            = PaymentStatus.Pending,
                    Amount            = amount
                });
            }

            return new CreatePaymentResponseDto
            {
                ExternalReference = externalRef,
                QrCode            = checkoutUrl,
                Status            = PaymentStatus.Pending
            };
        }

        // ─────────────────────────────────────────────────────────────────────
        // OBTENER ESTADO
        // ─────────────────────────────────────────────────────────────────────
        public async Task<PaymentStatusResponseDto> GetStatusAsync(string externalReference)
        {
            // Buscar primero en WEB, luego en STORE según el prefijo
            string? status;

            if (externalReference.StartsWith("STORE-"))
            {
                var sp = await _storeRepository.GetByExternalReferenceAsync(externalReference);
                status = sp?.Status;
            }
            else
            {
                status = await _repository.GetStatusAsync(externalReference);
            }

            return new PaymentStatusResponseDto { Status = status ?? PaymentStatus.Pending };
        }

        // ─────────────────────────────────────────────────────────────────────
        // CANCELAR
        // ─────────────────────────────────────────────────────────────────────
        public async Task<CancelPaymentResponseDto> CancelAsync(string externalReference)
        {
            if (externalReference.StartsWith("STORE-"))
                await _storeRepository.UpdateStatusAsync(externalReference, PaymentStatus.Cancelled);
            else
                await _repository.UpdateStatusAsync(externalReference, PaymentStatus.Cancelled);

            return new CancelPaymentResponseDto { Status = PaymentStatus.Cancelled };
        }

        // ─────────────────────────────────────────────────────────────────────
        // PROCESAR WEBHOOK
        // statusDetail ya viene resuelto desde el controller (sin doble llamada al SDK)
        // ─────────────────────────────────────────────────────────────────────
        public async Task ProcessWebhookAsync(
            string externalReference,
            string status,
            string statusDetail,
            string mercadoPagoPaymentId)
        {
            if (externalReference.StartsWith("STORE-"))
            {
                var sp = await _storeRepository.GetByExternalReferenceAsync(externalReference);

                if (sp == null)
                {
                    _logger.LogWarning("Webhook STORE: ExternalReference no encontrado {Ref}", externalReference);
                    return;
                }

                // Bloqueo de estados finales
                if (PaymentStatus.IsFinal(sp.Status))
                    return;

                // Idempotencia: mismo ID y mismo estado → ya procesado
                if (sp.MercadoPagoPaymentId == mercadoPagoPaymentId &&
                    sp.Status == status)
                    return;

                // Actualizar para CUALQUIER estado (approved, rejected, in_process, etc.)
                _logger.LogInformation(
                    "Webhook STORE: actualizando {Ref} → {Status} ({Detail})",
                    externalReference, status, statusDetail);

                await _storeRepository.UpdateStatusAndMpIdAsync(
                    externalReference, status, statusDetail, mercadoPagoPaymentId);
            }
            else
            {
                var payment = await _repository.GetByExternalReferenceAsync(externalReference);

                if (payment == null)
                {
                    _logger.LogWarning("Webhook WEB: ExternalReference no encontrado {Ref}", externalReference);
                    return;
                }

                // Bloqueo de estados finales
                if (PaymentStatus.IsFinal(payment.Status))
                    return;

                // Idempotencia: mismo ID y mismo estado → ya procesado
                if (payment.MercadoPagoPaymentId == mercadoPagoPaymentId &&
                    payment.Status == status)
                    return;

                await _repository.UpdateStatusAndMpIdAsync(
                    externalReference, status, statusDetail, mercadoPagoPaymentId);
            }
        }
    }
}
