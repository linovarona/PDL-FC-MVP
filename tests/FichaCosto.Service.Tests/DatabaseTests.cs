using System.Data;
using Dapper;
using Microsoft.Data.Sqlite;
using Xunit;

namespace FichaCosto.Service.Tests;

public class DatabaseTests
{
    private readonly string _connectionString = "Data Source=:memory:";

    [Fact]
    public void Can_Connect_To_Sqlite()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        Assert.Equal(ConnectionState.Open, connection.State);
    }

    [Fact]
    public void Can_Create_Tables_From_Schema()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var schema = @"
            CREATE TABLE Test (Id INTEGER PRIMARY KEY, Name TEXT);
            INSERT INTO Test (Name) VALUES ('Test');
        ";

        connection.Execute(schema);
        var count = connection.ExecuteScalar<int>("SELECT COUNT(*) FROM Test;");

        Assert.Equal(1, count);
    }

    [Fact]
    public void Schema_SQL_Exists()
    {
        // Verificar que el archivo Schema.sql existe en el proyecto
        var schemaPath = Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "FichaCosto.Service", "Data", "Schema.sql"
        );

        // Normalizar ruta
        var fullPath = Path.GetFullPath(schemaPath);

        Assert.True(File.Exists(fullPath), $"Schema.sql no encontrado en: {fullPath}");
    }
}