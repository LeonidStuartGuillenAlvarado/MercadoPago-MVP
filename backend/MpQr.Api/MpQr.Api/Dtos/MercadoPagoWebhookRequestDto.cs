using System.Text.Json.Serialization;

namespace MpQr.Api.Dtos
{
    public class MercadoPagoWebhookRequestDto
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("action")]
        public string? Action { get; set; }

        [JsonPropertyName("data")]
        public WebhookDataDto? Data { get; set; }
    }

    public class WebhookDataDto
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }
    }
}
