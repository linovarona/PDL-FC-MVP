// tests/FichaCosto.Service.Tests/TestConnectionFactory.cs
using System.Data;
using FichaCosto.Repositories.Interfaces;
using Microsoft.Data.Sqlite;

namespace FichaCosto.Service.Tests;

/// <summary>
/// Factory de conexiones para tests: reutiliza conexión compartida (SQLite in-memory).
/// </summary>
public class TestConnectionFactory : IConnectionFactory, IDisposable
{
    private readonly SqliteConnection _connection;
    private bool _disposed;

    public TestConnectionFactory(string connectionString = "Data Source=:memory:")
    {
        _connection = new SqliteConnection(connectionString);
        _connection.Open();

        // Habilitar foreign keys
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "PRAGMA foreign_keys = ON;";
        cmd.ExecuteNonQuery();
    }
    public TestConnectionFactory(SqliteConnection shared)
    {
        _connection = shared;
    }

    /// <summary>
    /// Crea una conexión que no cierra la conexión subyacente al hacer Dispose.
    /// Esto mantiene la base de datos en memoria viva durante toda la prueba.
    /// </summary>
    public IDbConnection CreateConnection()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(TestConnectionFactory));
        return new NonDisposableConnection(_connection);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _connection?.Close();
            _connection?.Dispose();
            _disposed = true;
        }
    }
}

/// <summary>
/// Wrapper de IDbConnection que ignora Dispose().
/// Mantiene la conexión SQLite in-memory abierta.
/// </summary>
//public class NonDisposableConnection : IDbConnection
//{
//    private readonly IDbConnection _inner;

//    public NonDisposableConnection(IDbConnection inner)
//    {
//        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
//    }

//    public string ConnectionString
//    {
//        get => _inner.ConnectionString;
//        set => _inner.ConnectionString = value;
//    }

//    public int ConnectionTimeout => _inner.ConnectionTimeout;
//    public string Database => _inner.Database;
//    public ConnectionState State => _inner.State;

//    public IDbTransaction BeginTransaction() => _inner.BeginTransaction();
//    public IDbTransaction BeginTransaction(IsolationLevel il) => _inner.BeginTransaction(il);
//    public void ChangeDatabase(string databaseName) => _inner.ChangeDatabase(databaseName);
//    public void Close() => _inner.Close();
//    public IDbCommand CreateCommand() => _inner.CreateCommand();
//    public void Open() => _inner.Open();

//    /// <summary>
//    /// NO llama _inner.Dispose() para mantener la conexión viva.
//    /// </summary>
//    public void Dispose()
//    {
//        // Intencionalmente vacío - no disposear la conexión padre
//    }
//}