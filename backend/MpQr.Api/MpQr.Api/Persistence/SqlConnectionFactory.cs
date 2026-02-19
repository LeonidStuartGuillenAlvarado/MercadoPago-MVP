using Microsoft.Data.SqlClient;

namespace MpQr.Api.Persistence
{
    public class SqlConnectionFactory
    {
        private readonly string _connectionString;

        public SqlConnectionFactory(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        public SqlConnection Create()
        {
            return new SqlConnection(_connectionString);
        }
    }
}
