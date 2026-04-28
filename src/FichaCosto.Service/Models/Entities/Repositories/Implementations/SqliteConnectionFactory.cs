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
            var connStr = configuration.GetConnectionString("DefaultConnection")
                ?? "Data Source=fichacosto.db";
            _connectionString = ResolveConnectionString(connStr);
        }

        private static string ResolveConnectionString(string connectionString)
        {
            var dataSource = ExtractDataSource(connectionString);
            if (!Path.IsPathRooted(dataSource))
            {
                dataSource = Path.Combine(AppContext.BaseDirectory, dataSource);
            }
            return connectionString.Replace(ExtractDataSource(connectionString), dataSource);
        }

        private static string ExtractDataSource(string connectionString)
        {
            var parts = connectionString.Split(';');
            foreach (var part in parts)
            {
                if (part.Trim().StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase))
                {
                    return part.Substring("Data Source=".Length).Trim();
                }
            }
            return "fichacosto.db";
        }

        public IDbConnection CreateConnection()
        {
            var connection = new SqliteConnection(_connectionString);
            connection.Open();
            return connection;
        }
    }
}
