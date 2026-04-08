namespace MpQr.Api.Models
{
    /// <summary>
    /// Constantes de estado de pago.
    /// Único lugar donde se definen: se usan en toda la aplicación
    /// para evitar strings literales dispersos.
    /// </summary>
    public static class PaymentStatus
    {
        public const string Pending    = "pending";
        public const string Approved   = "approved";
        public const string Rejected   = "rejected";
        public const string Cancelled  = "cancelled";
        public const string InProcess  = "in_process";
        public const string Refunded   = "refunded";

        private static readonly HashSet<string> FinalStates = new(StringComparer.OrdinalIgnoreCase)
        {
            Approved, Rejected, Cancelled, Refunded
        };

        public static bool IsFinal(string? status) =>
            status != null && FinalStates.Contains(status);
    }
}
