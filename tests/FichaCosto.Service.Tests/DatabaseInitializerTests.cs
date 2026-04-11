// tests/FichaCosto.Service.Tests/Unit/DatabaseInitializerTests.cs
using Dapper;
using FichaCosto.Repositories.Interfaces;
using FichaCosto.Service.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;

namespace FichaCosto.Service.Tests;

/// <summary>
/// Tests unitarios del DatabaseInitializer.
/// </summary>
public class DatabaseInitializerTests : IDisposable
{
    private readonly TestConnectionFactory _connectionFactory;
    private readonly ServiceProvider _services;

    public DatabaseInitializerTests()
    {
        _connectionFactory = new TestConnectionFactory();

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging(b => b.AddDebug());
        serviceCollection.AddSingleton<IHostEnvironment>(new TestHostingEnvironment());
        serviceCollection.AddSingleton<IConnectionFactory>(_connectionFactory);
        serviceCollection.AddSingleton<DatabaseInitializer>();

        _services = serviceCollection.BuildServiceProvider();
    }

    [Fact]
    public async Task InitializeAsync_CreatesSchema()
    {
        // Arrange
        var initializer = _services.GetRequiredService<DatabaseInitializer>();

        // Act
        await initializer.InitializeAsync();

        // Assert
        using var connection = _connectionFactory.CreateConnection();
        var tables = await connection.QueryAsync<string>(
            "SELECT name FROM sqlite_master WHERE type='table'");

        Assert.Contains("Clientes", tables);
        Assert.Contains("Productos", tables);
        Assert.Contains("FichasCosto", tables);
    }

    [Fact]
    public async Task InitializeAsync_DoesNotDuplicateData_OnSecondRun()
    {
        // Arrange
        var initializer = _services.GetRequiredService<DatabaseInitializer>();

        // Act - Primera inicialización
        await initializer.InitializeAsync();

        // Segunda inicialización (debe ser idempotente)
        await initializer.InitializeAsync();

        // Assert - Verificar que no hay duplicados
        using var connection = _connectionFactory.CreateConnection();
        var clientes = await connection.QueryAsync<dynamic>("SELECT * FROM Clientes");

        // En test, solo debe haber 1 cliente (el de test)
        Assert.Single(clientes);
    }

    public void Dispose()
    {
        _connectionFactory?.Dispose();
        _services?.Dispose();
    }
}