using System.Security.Cryptography;
using System.Text;

namespace MpQr.Api.Security
{
    public class MercadoPagoSignatureValidator
    {
        private readonly IConfiguration _configuration;

        public MercadoPagoSignatureValidator(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public bool Validate(string signatureHeader, string dataId)
        {
            if (string.IsNullOrEmpty(signatureHeader))
                return false;

            var secret = _configuration["MercadoPago:WebhookSecret"];

            var parts = signatureHeader.Split(',');
            var tsPart = parts.FirstOrDefault(x => x.StartsWith("ts="));
            var hashPart = parts.FirstOrDefault(x => x.StartsWith("v1="));

            if (tsPart == null || hashPart == null)
                return false;

            var ts = tsPart.Replace("ts=", "");
            var receivedHash = hashPart.Replace("v1=", "");

            var payload = $"{dataId}{ts}";

            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));

            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            var computedHex = BitConverter.ToString(computedHash)
                .Replace("-", "")
                .ToLower();

            return computedHex == receivedHash;
        }
    }
}
