// tests/FichaCosto.Service.Tests/TestWebApplicationFactory.cs
using FichaCosto.Repositories.Interfaces;
using FichaCosto.Service.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestPlatform.TestHost;

namespace FichaCosto.Service.Tests;

/// <summary>
/// Factory para tests de integración. 
/// Usa conexión SQLite en memoria por defecto.
/// </summary>
public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    // Constructor SIN PARÁMETROS (requerido por xUnit)
    public TestWebApplicationFactory() : this("Data Source=:memory:")
    {
    }

    // Constructor privado con parámetros para uso interno
    private TestWebApplicationFactory(string connectionString)
    {
        ConnectionString = connectionString;
    }

    public string ConnectionString { get; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Remover registros existentes
            services.RemoveAll<IConnectionFactory>();
            services.RemoveAll<IHostEnvironment>();

            // Registrar factory de test
            var testFactory = new TestConnectionFactory(ConnectionString);
            services.AddSingleton<IConnectionFactory>(testFactory);

            // Registrar environment de test
            services.AddSingleton<IHostEnvironment>(sp => new HostingEnvironment
            {
                EnvironmentName = "Testing",
                ApplicationName = "FichaCosto.Service.Tests"
            });

            services.AddSingleton<DatabaseInitializer>();
        });
    }
}

public class HostingEnvironment : IHostEnvironment
{
    public string EnvironmentName { get; set; } = "Testing";
    public string ApplicationName { get; set; } = "TestApp";
    public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
    public IFileProvider ContentRootFileProvider { get; set; } = null!;
}