using System.Data;

namespace FichaCosto.Service.Tests
{
    public class NonDisposableConnection : IDbConnection
    {
        private readonly IDbConnection _inner;

        public NonDisposableConnection(IDbConnection inner)
        {
            _inner = inner;
        }

        public string ConnectionString
        {
            get => _inner.ConnectionString;
            set => _inner.ConnectionString = value;

        }

        public int ConnectionTimeout => _inner.ConnectionTimeout;
        public string Database => _inner.Database;
        public ConnectionState State => _inner.State;

        public IDbTransaction BeginTransaction() => _inner.BeginTransaction();
        public IDbTransaction BeginTransaction(IsolationLevel il) => _inner.BeginTransaction(il);
        public void ChangeDatabase(string db) => _inner.ChangeDatabase(db);
        public void Close() => _inner.Close();
        public IDbCommand CreateCommand() => _inner.CreateCommand();
        public void Open() => _inner.Open();

        public void Dispose()
        {
            // IGNORAR - no cerrar conexión real
        }
    }
}
