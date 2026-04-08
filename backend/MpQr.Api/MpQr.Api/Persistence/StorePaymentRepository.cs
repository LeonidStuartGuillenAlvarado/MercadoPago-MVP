using Microsoft.Data.SqlClient;
using MpQr.Api.Models;

namespace MpQr.Api.Persistence
{
    public class StorePaymentRepository
    {
        private readonly SqlConnectionFactory _factory;

        public StorePaymentRepository(SqlConnectionFactory factory)
        {
            _factory = factory;
        }

        public async Task InsertAsync(StorePayment payment)
        {
            using var conn = _factory.Create();
            using var cmd  = new SqlCommand(@"
                INSERT INTO StorePayments
                    (ExternalReference, Status, Amount, IsEnabled, CheckoutUrl, CreatedAt)
                VALUES
                    (@ref, @status, @amount, @enabled, @checkoutUrl, GETDATE())", conn);

            cmd.Parameters.AddWithValue("@ref",        payment.ExternalReference);
            cmd.Parameters.AddWithValue("@status",     payment.Status);
            cmd.Parameters.AddWithValue("@amount",     payment.Amount);
            cmd.Parameters.AddWithValue("@enabled",    payment.IsEnabled);
            cmd.Parameters.AddWithValue("@checkoutUrl",payment.CheckoutUrl);

            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>Obtiene el pago activo más reciente para el QR estático.</summary>
        public async Task<StorePayment?> GetActiveAsync()
        {
            using var conn = _factory.Create();
            using var cmd  = new SqlCommand(@"
                SELECT TOP 1
                    Id, ExternalReference, Status, StatusDetail,
                    Amount, IsEnabled, CheckoutUrl,
                    MercadoPagoPaymentId, CreatedAt, UpdatedAt
                FROM StorePayments
                WHERE Status    = 'pending'
                  AND IsEnabled = 1
                ORDER BY CreatedAt DESC", conn);

            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();

            return reader.Read() ? MapStorePayment(reader) : null;
        }

        public async Task<StorePayment?> GetByExternalReferenceAsync(string externalReference)
        {
            using var conn = _factory.Create();
            using var cmd  = new SqlCommand(@"
                SELECT TOP 1
                    Id, ExternalReference, Status, StatusDetail,
                    Amount, IsEnabled, CheckoutUrl,
                    MercadoPagoPaymentId, CreatedAt, UpdatedAt
                FROM StorePayments
                WHERE ExternalReference = @ref", conn);

            cmd.Parameters.AddWithValue("@ref", externalReference);

            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();

            return reader.Read() ? MapStorePayment(reader) : null;
        }

        public async Task UpdateStatusAsync(string externalReference, string status)
        {
            using var conn = _factory.Create();
            using var cmd  = new SqlCommand(@"
                UPDATE StorePayments
                SET Status    = @status,
                    IsEnabled = CASE WHEN @status IN ('approved','cancelled','rejected') THEN 0 ELSE IsEnabled END,
                    UpdatedAt = GETDATE()
                WHERE ExternalReference = @ref", conn);

            cmd.Parameters.AddWithValue("@ref",    externalReference);
            cmd.Parameters.AddWithValue("@status", status);

            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task UpdateStatusAndMpIdAsync(
            string externalReference,
            string status,
            string statusDetail,
            string mpId)
        {
            using var conn = _factory.Create();
            using var cmd  = new SqlCommand(@"
                UPDATE StorePayments
                SET Status               = @status,
                    MercadoPagoPaymentId = @mpId,
                    StatusDetail         = @statusDetail,
                    IsEnabled            = CASE WHEN @status IN ('approved','cancelled','rejected') THEN 0 ELSE IsEnabled END,
                    UpdatedAt            = GETDATE()
                WHERE ExternalReference = @ref", conn);

            cmd.Parameters.AddWithValue("@ref",          externalReference);
            cmd.Parameters.AddWithValue("@status",       status);
            cmd.Parameters.AddWithValue("@statusDetail", (object?)statusDetail ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@mpId",         mpId);

            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        // ─────────────────────────────────────────────────────────────────────
        private static StorePayment MapStorePayment(SqlDataReader r) => new()
        {
            Id                   = (int)r["Id"],
            ExternalReference    = r["ExternalReference"].ToString()!,
            Status               = r["Status"].ToString()!,
            StatusDetail         = r["StatusDetail"] as string,
            Amount               = (decimal)r["Amount"],
            IsEnabled            = (bool)r["IsEnabled"],
            CheckoutUrl          = r["CheckoutUrl"].ToString()!,
            MercadoPagoPaymentId = r["MercadoPagoPaymentId"] as string,
            CreatedAt            = (DateTime)r["CreatedAt"],
            UpdatedAt            = r["UpdatedAt"] as DateTime?
        };
    }
}
