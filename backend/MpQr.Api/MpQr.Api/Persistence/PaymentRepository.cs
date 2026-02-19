using Microsoft.Data.SqlClient;
using MpQr.Api.Models;

namespace MpQr.Api.Persistence
{
    public class PaymentRepository
    {
        private readonly SqlConnectionFactory _factory;

        public PaymentRepository(SqlConnectionFactory factory)
        {
            _factory = factory;
        }

        public async Task InsertAsync(Payment payment)
        {
            using var conn = _factory.Create();
            using var cmd = new SqlCommand(
                @"INSERT INTO Payments (ExternalReference, Status, Amount)
                  VALUES (@ref, @status, @amount)", conn);

            cmd.Parameters.AddWithValue("@ref", payment.ExternalReference);
            cmd.Parameters.AddWithValue("@status", payment.Status);
            cmd.Parameters.AddWithValue("@amount", payment.Amount);

            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<string> GetStatusAsync(string externalReference)
        {
            using var conn = _factory.Create();
            using var cmd = new SqlCommand(
                "SELECT Status FROM Payments WHERE ExternalReference = @ref", conn);

            cmd.Parameters.AddWithValue("@ref", externalReference);

            await conn.OpenAsync();
            var result = await cmd.ExecuteScalarAsync();

            return result?.ToString() ?? "pending";
        }

        public async Task UpdateStatusAsync(string externalReference, string status)
        {
            using var conn = _factory.Create();
            using var cmd = new SqlCommand(
                @"UPDATE Payments
                  SET Status = @status, UpdatedAt = GETDATE()
                  WHERE ExternalReference = @ref", conn);

            cmd.Parameters.AddWithValue("@ref", externalReference);
            cmd.Parameters.AddWithValue("@status", status);

            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }
        public async Task UpdateStatusAndMpIdAsync(
            string externalReference,
            string status,
            string mpPaymentId)
        {
            using var conn = _factory.Create();
            using var cmd = new SqlCommand(
                @"UPDATE Payments
                    SET Status = @status,
                     MercadoPagoPaymentId = @mpId,
                    UpdatedAt = GETDATE()
                    WHERE ExternalReference = @ref",
                    conn);

            cmd.Parameters.AddWithValue("@ref", externalReference);
            cmd.Parameters.AddWithValue("@status", status);
            cmd.Parameters.AddWithValue("@mpId", mpPaymentId);

            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

    }
}