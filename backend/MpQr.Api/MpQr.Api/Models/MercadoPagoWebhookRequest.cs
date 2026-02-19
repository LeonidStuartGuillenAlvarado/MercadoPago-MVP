namespace MpQr.Api.Models
{
    public class MercadoPagoWebhookRequest
    {
        public string Id { get; set; }
        public bool Live_Mode { get; set; }
        public string Type { get; set; }
        public string Date_Created { get; set; }
        public string User_Id { get; set; }
        public string Api_Version { get; set; }
        public string Action { get; set; }
        public WebhookData Data { get; set; }
    }

    public class WebhookData
    {
        public string Id { get; set; }
    }
}
