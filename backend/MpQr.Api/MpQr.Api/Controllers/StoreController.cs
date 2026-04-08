using Microsoft.AspNetCore.Mvc;
using MpQr.Api.Persistence;

namespace MpQr.Api.Controllers
{
    [ApiController]
    [Route("api/store")]
    public class StoreController : ControllerBase
    {
        private readonly StorePaymentRepository _repository;
        private readonly ILogger<StoreController> _logger;

        public StoreController(
            StorePaymentRepository repository,
            ILogger<StoreController> logger)
        {
            _repository = repository;
            _logger     = logger;
        }

        // ── GET /api/store/active ─────────────────────────────────────────────
        [HttpGet("active")]
        public async Task<IActionResult> GetActivePayment()
        {
            var payment = await _repository.GetActiveAsync();

            if (payment == null)
                return Ok(new { hasActivePayment = false });

            return Ok(new
            {
                hasActivePayment  = true,
                externalReference = payment.ExternalReference,
                checkoutUrl       = payment.CheckoutUrl
            });
        }

        // ── GET /api/store/scan  (destino del QR estático físico) ─────────────
        // El cliente escanea el QR impreso → llega aquí → redirige al checkout activo.
        [HttpGet("scan")]
        public async Task<IActionResult> Scan()
        {
            var payment = await _repository.GetActiveAsync();

            if (payment == null)
            {
                _logger.LogInformation("Scan: no hay pago activo.");
                return Content(
                    """
                    <!DOCTYPE html>
                    <html lang="es">
                    <head>
                      <meta charset="UTF-8">
                      <meta name="viewport" content="width=device-width, initial-scale=1">
                      <title>Pago no disponible</title>
                      <style>
                        body { font-family: sans-serif; text-align: center; padding: 60px 20px; color: #333; }
                        h2   { font-size: 1.4rem; margin-bottom: 1rem; }
                        p    { color: #666; }
                      </style>
                    </head>
                    <body>
                      <h2>No hay un pago habilitado en este momento.</h2>
                      <p>Solicite al cajero que genere el cobro e intente nuevamente.</p>
                    </body>
                    </html>
                    """,
                    "text/html");
            }

            return Redirect(payment.CheckoutUrl);
        }
    }
}
