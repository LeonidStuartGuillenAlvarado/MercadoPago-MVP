using Microsoft.Data.SqlClient;

namespace MpQr.Api.Persistence
{
    public class SqlConnectionFactory
    {
        private readonly string _connectionString;

        public SqlConnectionFactory(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection no configurado.");
        }

        public SqlConnection Create() => new(_connectionString);
    }
}
