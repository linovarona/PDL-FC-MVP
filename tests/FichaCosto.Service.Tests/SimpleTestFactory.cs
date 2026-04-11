// tests/FichaCosto.Service.Tests/SimpleTestFactory.cs
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.FileProviders;
using FichaCosto.Service.Data;
using FichaCosto.Repositories.Interfaces;

namespace FichaCosto.Service.Tests;

/// <summary>
/// Factory simplificada para tests de integración de base de datos.
/// </summary>
public class SimpleTestFactory : IDisposable, IAsyncLifetime
{
    private readonly TestConnectionFactory _connectionFactory;
    private readonly ServiceProvider _serviceProvider;
    private bool _initialized = false;

    public IServiceProvider Services => _serviceProvider;

    public SimpleTestFactory()
    {
        _connectionFactory = new TestConnectionFactory("Data Source=:memory:");

        var services = new ServiceCollection();

        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Warning); // Menos ruido en tests
        });

        services.AddSingleton<IHostEnvironment>(new TestHostingEnvironment
        {
            EnvironmentName = "Testing",
            ApplicationName = "FichaCosto.Service.Tests"
        });

        services.AddSingleton<IConnectionFactory>(_connectionFactory);
        services.AddSingleton<DatabaseInitializer>();

        _serviceProvider = services.BuildServiceProvider();
    }

    public async Task InitializeAsync()
    {
        if (_initialized) return;

        var initializer = _serviceProvider.GetRequiredService<DatabaseInitializer>();
        await initializer.InitializeAsync();
        _initialized = true;
    }

    public Task DisposeAsync()
    {
        Dispose();
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _connectionFactory?.Dispose();
        _serviceProvider?.Dispose();
    }
}

public class TestHostingEnvironment : IHostEnvironment
{
    public string EnvironmentName { get; set; } = "Testing";
    public string ApplicationName { get; set; } = "TestApp";
    public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}