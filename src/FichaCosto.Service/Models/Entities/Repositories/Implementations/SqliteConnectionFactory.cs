using FichaCosto.Repositories.Interfaces;
using Microsoft.Data.Sqlite;
using System.Data;

namespace FichaCosto.Repositories.Implementations
{
    public class SqliteConnectionFactory : IConnectionFactory
    {
        private readonly string _connectionString;

        public SqliteConnectionFactory(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        }

        public IDbConnection CreateConnection()
        {
            var connection = new SqliteConnection(_connectionString);
            connection.Open();
            return connection;
        }
    }
}
