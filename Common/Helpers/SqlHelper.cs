using Microsoft.Data.SqlClient;

namespace Common.Helpers
{
    public class SqlHelper
    {
        private readonly string _connectionString;

        public SqlHelper(string connectionString)
        {
            _connectionString = connectionString;
        }

        public SqlConnection CreateConnection()
        {
            return new SqlConnection(_connectionString);
        }

        public async Task<int> ExecuteNonQueryAsync(string sql, params SqlParameter[] parameters)
        {
            using var connection = CreateConnection();
            await connection.OpenAsync();

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddRange(parameters);

            return await command.ExecuteNonQueryAsync();
        }

        public async Task<int> ExecuteScalarInsertAsync(string sql, params SqlParameter[] parameters)
        {
            using var connection = CreateConnection();
            await connection.OpenAsync();

            using var command = new SqlCommand(sql + "; SELECT CAST(SCOPE_IDENTITY() AS INT);", connection);
            command.Parameters.AddRange(parameters);

            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        public async Task<(SqlConnection connection, SqlDataReader reader)> ExecuteReaderAsync(
            string sql, params SqlParameter[] parameters)
        {
            var connection = CreateConnection();
            await connection.OpenAsync();

            var command = new SqlCommand(sql, connection);
            command.Parameters.AddRange(parameters);

            var reader = await command.ExecuteReaderAsync();
            return (connection, reader);
        }
    }
}
