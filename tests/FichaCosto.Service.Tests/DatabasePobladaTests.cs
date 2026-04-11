// tests/FichaCosto.Service.Tests/Integration/DatabasePobladaTests.cs
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Dapper;
using FichaCosto.Repositories.Interfaces;

namespace FichaCosto.Service.Tests.Integration;

/// <summary>
/// Tests de integración de base de datos poblada.
/// </summary>
public class DatabasePobladaTests : IClassFixture<SimpleTestFactory>, IAsyncLifetime, IDisposable
{
    private readonly SimpleTestFactory _factory;
    private readonly IServiceScope _scope;
    private bool _disposed = false;

    public DatabasePobladaTests(SimpleTestFactory factory)
    {
        _factory = factory;
        // El factory se inicializa automáticamente por IAsyncLifetime
        _scope = factory.Services.CreateScope();
    }

    /// <summary>
    /// Inicialización asíncrona por test (requerida por IAsyncLifetime).
    /// </summary>
    public Task InitializeAsync()
    {
        // El factory ya se inicializó automáticamente
        // Verificar que la BD tiene datos
        return VerifyDatabaseHasDataAsync();
    }

    /// <summary>
    /// Verifica que la base de datos tiene los datos de test.
    /// </summary>
    private async Task VerifyDatabaseHasDataAsync()
    {
        var connectionFactory = _scope.ServiceProvider.GetRequiredService<IConnectionFactory>();
        using var connection = connectionFactory.CreateConnection();

        var clientes = await connection.QueryAsync<dynamic>("SELECT * FROM Clientes");

        if (!clientes.Any())
        {
            throw new InvalidOperationException(
                "La base de datos no tiene datos de test. " +
                "Verificar que DatabaseInitializer ejecutó SeedMinimalTestDataAsync.");
        }
    }

    [Fact]
    public async Task DatabaseInitialized_WithTestData()
    {
        // Arrange
        var connectionFactory = _scope.ServiceProvider.GetRequiredService<IConnectionFactory>();
        using var connection = connectionFactory.CreateConnection();

        // Act
        var clientes = await connection.QueryAsync<dynamic>("SELECT * FROM Clientes");
        var productos = await connection.QueryAsync<dynamic>("SELECT * FROM Productos");
        var fichas = await connection.QueryAsync<dynamic>("SELECT * FROM FichasCosto");

        // Assert
        Assert.Single(clientes);
        Assert.Single(productos);
        Assert.Single(fichas);

        Assert.Equal("Cliente Test S.A.", (string)clientes.First().NombreEmpresa);
        Assert.Equal("TEST-001", (string)productos.First().Codigo);
        Assert.Equal(1, (int)fichas.First().Id);
    }

    [Fact]
    public async Task FichaCosto_HasCorrectCalculatedValues()
    {
        var connectionFactory = _scope.ServiceProvider.GetRequiredService<IConnectionFactory>();
        using var connection = connectionFactory.CreateConnection();

        var ficha = await connection.QueryFirstAsync<dynamic>(
            "SELECT * FROM FichasCosto WHERE Id = 1");

        Assert.Equal(2000.00m, (decimal)ficha.CostoMateriasPrimas);
        Assert.Equal(468.00m, (decimal)ficha.CostoManoObra);
        Assert.Equal(2468.00m, (decimal)ficha.CostoTotal);
        Assert.Equal(30.00m, (decimal)ficha.MargenUtilidad);
        Assert.Equal(3525.71m, Math.Round((decimal)ficha.PrecioVentaSugerido, 2));
    }

    public Task DisposeAsync()
    {
        Dispose();
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _scope?.Dispose();
            _disposed = true;
        }
    }
}