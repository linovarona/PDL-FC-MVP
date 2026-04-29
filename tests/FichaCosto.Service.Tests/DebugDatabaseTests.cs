// tests/FichaCosto.Service.Tests/Integration/DebugDatabaseTests.cs
using Dapper;
using FichaCosto.Repositories.Interfaces;
using FichaCosto.Service.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Data;
using Xunit;
using Xunit.Abstractions;

namespace FichaCosto.Service.Tests.Integration;

public class DebugDatabaseTests : IDisposable
{
    private readonly TestConnectionFactory _connectionFactory;
    private readonly ServiceProvider _services;
    private readonly ITestOutputHelper _output;

    // ÚNICO CONSTRUCTOR - recibe ITestOutputHelper de xUnit
    public DebugDatabaseTests(ITestOutputHelper output)
    {
        _output = output;
        _connectionFactory = new TestConnectionFactory();

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging(b =>
        {
            b.AddConsole();
            b.SetMinimumLevel(LogLevel.Debug);
        });
        serviceCollection.AddSingleton<IHostEnvironment>(new TestHostingEnvironment());
        serviceCollection.AddSingleton<IConnectionFactory>(_connectionFactory);
        serviceCollection.AddSingleton<DatabaseInitializer>();

        _services = serviceCollection.BuildServiceProvider();
    }

    [Fact]
    public async Task Debug_InitializationStepByStep()
    {
        // 1. Verificar conexión
        using var conn = _connectionFactory.CreateConnection();
        Assert.Equal(ConnectionState.Open, conn.State);
        _output.WriteLine("✓ Conexión abierta");

        // 2. Verificar que no hay tablas inicialmente
        var tablesBefore = await conn.QueryAsync<string>(
            "SELECT name FROM sqlite_master WHERE type='table'");
        _output.WriteLine($"Tablas antes: {tablesBefore.Count()}");

        // 3. Ejecutar inicializador
        _output.WriteLine("Ejecutando DatabaseInitializer...");
        var initializer = _services.GetRequiredService<DatabaseInitializer>();
        await initializer.InitializeAsync();
        _output.WriteLine("✓ Inicializador completado");

        // 4. Verificar tablas creadas
        var tablesAfter = await conn.QueryAsync<string>(
            "SELECT name FROM sqlite_master WHERE type='table'");
        _output.WriteLine($"Tablas después: {string.Join(", ", tablesAfter)}");

        // 5. Verificar datos
        var clientes = await conn.QueryAsync<dynamic>("SELECT * FROM Clientes");
        _output.WriteLine($"Clientes encontrados: {clientes.Count()}");

        foreach (var c in clientes)
        {
            _output.WriteLine($"  - ID:{c.Id} | {c.NombreEmpresa} | CUIT:{c.CUIT}");
        }

        // 6. Verificar productos
        var productos = await conn.QueryAsync<dynamic>("SELECT * FROM Productos");
        _output.WriteLine($"Productos encontrados: {productos.Count()}");

        // 7. Verificar fichas
        var fichas = await conn.QueryAsync<dynamic>("SELECT * FROM FichasCosto");
        _output.WriteLine($"Fichas encontradas: {fichas.Count()}");

        // Asserts
        Assert.Contains("Clientes", tablesAfter);
        Assert.NotEmpty(clientes);
    }

    public void Dispose()
    {
        _connectionFactory?.Dispose();
        _services?.Dispose();
    }
}