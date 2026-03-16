using FichaCosto.Repositories.Interfaces;
using Microsoft.Data.Sqlite;
using System.Data;

namespace FichaCosto.Service.Tests
{
    public class TestConnectionFactory : IConnectionFactory
    {
        private readonly SqliteConnection _shared;

        public TestConnectionFactory(SqliteConnection shared)
        {
            _shared = shared;
        }

        public IDbConnection CreateConnection()
        {
            return new NonDisposableConnection(_shared);
        }
    }
}
