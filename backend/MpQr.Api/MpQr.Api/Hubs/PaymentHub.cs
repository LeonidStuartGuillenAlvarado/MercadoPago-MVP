using Microsoft.AspNetCore.SignalR;

namespace MpQr.Api.Hubs
{
    public class PaymentHub : Hub
    {
        // El hub actúa solo como canal de broadcast.
        // Los mensajes se emiten desde los controllers vía IHubContext<PaymentHub>.
    }
}
