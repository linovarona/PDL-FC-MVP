// tests/FichaCosto.Service.Tests/Integration/DebugSeedTests.cs
using Dapper;
using FichaCosto.Repositories.Interfaces;
using FichaCosto.Service.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace FichaCosto.Service.Tests.Integration;

public class DebugSeedTests : IDisposable
{
    private readonly TestConnectionFactory _connectionFactory;
    private readonly ServiceProvider _services;
    private readonly ITestOutputHelper _output;

    public DebugSeedTests(ITestOutputHelper output)
    {
        _output = output;
        _connectionFactory = new TestConnectionFactory();

        var services = new ServiceCollection();
        services.AddLogging(b => b.AddDebug().SetMinimumLevel(LogLevel.Debug));
        services.AddSingleton<IHostEnvironment>(new TestHostingEnvironment());
        services.AddSingleton<IConnectionFactory>(_connectionFactory);
        services.AddSingleton<DatabaseInitializer>();

        _services = services.BuildServiceProvider();
    }

    [Fact]
    public async Task Debug_SeedDataStepByStep()
    {
        // Ejecutar inicializador
        var initializer = _services.GetRequiredService<DatabaseInitializer>();
        await initializer.InitializeAsync();

        using var connection = _connectionFactory.CreateConnection();

        // Verificar tabla por tabla
        var tables = await connection.QueryAsync<string>(
            "SELECT name FROM sqlite_master WHERE type='table' ORDER BY name");
        _output.WriteLine($"Tablas existentes: {string.Join(", ", tables)}");

        // Verificar cada tabla de datos
        foreach (var table in new[] { "Clientes", "Productos", "MateriasPrimas", "ManoObraDirecta", "FichasCosto" })
        {
            try
            {
                var count = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM {table}");
                _output.WriteLine($"{table}: {count} registros");

                if (count > 0 && table == "FichasCosto")
                {
                    var data = await connection.QueryFirstAsync<dynamic>($"SELECT * FROM {table}");
                    _output.WriteLine($"  Ficha ID={data.Id}, ProductoId={data.ProductoId}, CostoTotal={data.CostoTotal}");
                }
            }
            catch (Exception ex)
            {
                _output.WriteLine($"{table}: ERROR - {ex.Message}");
            }
        }

        // Verificar schema de FichasCosto
        var columns = await connection.QueryAsync<string>(
            "SELECT name FROM pragma_table_info('FichasCosto')");
        _output.WriteLine($"\nColumnas en FichasCosto: {string.Join(", ", columns)}");
    }

    public void Dispose()
    {
        _connectionFactory?.Dispose();
        _services?.Dispose();
    }
}