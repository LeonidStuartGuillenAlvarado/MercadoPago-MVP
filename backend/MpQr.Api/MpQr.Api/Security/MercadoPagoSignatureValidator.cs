using System.Security.Cryptography;
using System.Text;

namespace MpQr.Api.Security
{
    public class MercadoPagoSignatureValidator
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<MercadoPagoSignatureValidator> _logger;

        public MercadoPagoSignatureValidator(
            IConfiguration configuration,
            ILogger<MercadoPagoSignatureValidator> logger)
        {
            _configuration = configuration;
            _logger        = logger;
        }

        /// <summary>
        /// Valida la firma HMAC-SHA256 según documentación oficial de MercadoPago.
        ///
        /// Header x-signature:   "ts=TIMESTAMP,v1=HASH"
        /// Header x-request-id:  UUID de la request (opcional pero recomendado)
        ///
        /// Payload firmado (formato exacto MP):
        ///   Con request-id:  "id:{dataId};request-id:{xRequestId};ts:{ts};"
        ///   Sin request-id:  "id:{dataId};ts:{ts};"
        ///
        /// Para saltear validación en desarrollo, agregar en appsettings.json:
        ///   "MercadoPago:SkipSignatureValidation": true
        /// </summary>
        public bool Validate(string? signatureHeader, string dataId, string? xRequestId = null)
        {
            // ── Modo bypass para desarrollo / diagnóstico ─────────────────────
            var skip = _configuration.GetValue<bool>("MercadoPago:SkipSignatureValidation");
            if (skip)
            {
                _logger.LogWarning(
                    "⚠️  Webhook: validación de firma DESACTIVADA (SkipSignatureValidation=true). " +
                    "No usar en producción.");
                return true;
            }

            // ── Header ausente ────────────────────────────────────────────────
            if (string.IsNullOrEmpty(signatureHeader))
            {
                _logger.LogWarning(
                    "Webhook: header x-signature ausente para dataId={DataId}. " +
                    "Si estás en desarrollo, podés activar MercadoPago:SkipSignatureValidation=true " +
                    "en appsettings.json para continuar.", dataId);
                return false;
            }

            var secret = _configuration["MercadoPago:WebhookSecret"];
            if (string.IsNullOrEmpty(secret))
            {
                _logger.LogError("Webhook: MercadoPago:WebhookSecret no configurado.");
                return false;
            }

            // ── Parsear "ts=...,v1=..." ───────────────────────────────────────
            var parts    = signatureHeader.Split(',');
            var tsPart   = parts.FirstOrDefault(x => x.StartsWith("ts="));
            var hashPart = parts.FirstOrDefault(x => x.StartsWith("v1="));

            if (tsPart == null || hashPart == null)
            {
                _logger.LogWarning(
                    "Webhook: formato de x-signature inválido: '{Header}'", signatureHeader);
                return false;
            }

            var ts           = tsPart[3..];   // quita "ts="
            var receivedHash = hashPart[3..]; // quita "v1="

            // ── Calcular firma esperada ───────────────────────────────────────
            // Intentar CON y SIN request-id para cubrir variaciones de MP
            var payloadConReqId = $"id:{dataId};request-id:{xRequestId};ts:{ts};";
            var payloadSinReqId = $"id:{dataId};ts:{ts};";

            var payload = string.IsNullOrEmpty(xRequestId) ? payloadSinReqId : payloadConReqId;

            using var hmac    = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var computedBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            var computedHex   = Convert.ToHexString(computedBytes).ToLower();

            var valid = CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(computedHex),
                Encoding.UTF8.GetBytes(receivedHash.ToLower()));

            // ── Si falló, probar también el payload alternativo ───────────────
            if (!valid && !string.IsNullOrEmpty(xRequestId))
            {
                using var hmac2    = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
                var computedBytes2 = hmac2.ComputeHash(Encoding.UTF8.GetBytes(payloadSinReqId));
                var computedHex2   = Convert.ToHexString(computedBytes2).ToLower();

                if (CryptographicOperations.FixedTimeEquals(
                        Encoding.UTF8.GetBytes(computedHex2),
                        Encoding.UTF8.GetBytes(receivedHash.ToLower())))
                {
                    _logger.LogInformation(
                        "Webhook: firma válida (formato sin request-id). dataId={DataId}", dataId);
                    return true;
                }
            }

            if (!valid)
            {
                _logger.LogWarning(
                    "Webhook: FIRMA INVÁLIDA.\n" +
                    "  dataId        = {DataId}\n" +
                    "  xRequestId    = {ReqId}\n" +
                    "  ts            = {Ts}\n" +
                    "  payload usado = '{Payload}'\n" +
                    "  hash recibido = {ReceivedHash}\n" +
                    "  hash calculado= {ComputedHex}\n" +
                    "  → Verificar que WebhookSecret en appsettings.json coincida exactamente " +
                    "con el Secret configurado en el panel de MercadoPago (Developers → Webhooks).",
                    dataId, xRequestId, ts, payload, receivedHash.ToLower(), computedHex);
            }
            else
            {
                _logger.LogInformation(
                    "Webhook: firma válida. dataId={DataId}", dataId);
            }

            return valid;
        }
    }
}
