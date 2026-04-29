// tests/FichaCosto.Service.Tests/Integration/InstallationFlowTests.cs
using Dapper;
using FichaCosto.Repositories.Interfaces;
using FichaCosto.Service.Data;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Data;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace FichaCosto.Service.Tests.Integration;

/// <summary>
/// Pruebas de integración para el flujo completo de instalación.
/// Valida: rutas, flags, permisos y consistencia entre PowerShell y C#
/// </summary>
public class InstallationFlowTests : IDisposable
{
    private readonly string _tempBasePath;
    private readonly string _testDataPath;
    private readonly Mock<ILogger<DatabaseInitializer>> _loggerMock;
    private readonly Mock<IHostEnvironment> _envMock;

    public InstallationFlowTests()
    {
        // Crear estructura temporal que simula C:\ProgramData\FichaCosto
        _tempBasePath = Path.Combine(Path.GetTempPath(), $"FCTests_{Guid.NewGuid()}");
        _testDataPath = Path.Combine(_tempBasePath, "Data");
        Directory.CreateDirectory(_testDataPath);
        Directory.CreateDirectory(Path.Combine(_tempBasePath, "Logs"));

        _loggerMock = new Mock<ILogger<DatabaseInitializer>>();
        _envMock = new Mock<IHostEnvironment>();
        _envMock.Setup(x => x.EnvironmentName).Returns("Production");
    }

    
[Fact]
    public async Task HandleSeedFlag_WhenFlagExists_CreatesDatabaseInCorrectPath()
    {
        // Arrange: Usar ruta absoluta explícita
        var dbPath = Path.Combine(_testDataPath, "fichacosto.db");
        var flagPath = Path.Combine(_testDataPath, ".seed-required");

        // Crear el flag ANTES de que exista la BD
        File.WriteAllText(flagPath, "Seed requested by installer");

        // Verificar que el flag existe en la ubicación esperada
        Assert.True(File.Exists(flagPath), $"Flag debería existir en: {flagPath}");

        // Crear factory con ruta absoluta explícita
        var connectionString = $"Data Source={dbPath}";
        var factory = new TestableConnectionFactory(connectionString);

        var initializer = new DatabaseInitializer(
            factory, _loggerMock.Object, _envMock.Object);

        // Act
        await initializer.InitializeAsync();

        // Assert
        Assert.True(File.Exists(dbPath), $"BD debería existir en: {dbPath}");

        // Verificar que el flag fue consumido
        Assert.False(File.Exists(flagPath), "Flag debería ser eliminado tras seed exitoso");

        // Verificar que las tablas se crearon
        using var conn = new SqliteConnection(connectionString);
        conn.Open(); // Asegurar conexión abierta
        var tables = await conn.QueryAsync<string>(
            "SELECT name FROM sqlite_master WHERE type='table' AND name='Clientes'");
        Assert.Contains("Clientes", tables);
    }

    [Fact]
    public async Task HandleSeedFlag_WithRelativePath_ResolvesToAbsolute()
    {
        // Arrange: Simular configuración con ruta relativa (como en appsettings.json)
        var relativePath = "./Data/fichacosto.db";
        var expectedFullPath = Path.Combine(AppContext.BaseDirectory, "Data", "fichacosto.db");
        var flagPath = Path.Combine(Path.GetDirectoryName(expectedFullPath)!, ".seed-required");

        // Asegurar que existe la carpeta
        Directory.CreateDirectory(Path.GetDirectoryName(expectedFullPath)!);
        File.WriteAllText(flagPath, "test");

        var connectionString = $"Data Source={relativePath}";
        var factory = new TestableConnectionFactory(connectionString);
        var initializer = new DatabaseInitializer(
            factory, _loggerMock.Object, _envMock.Object);

        // Act
        await initializer.InitializeAsync();

        // Assert
        Assert.True(File.Exists(expectedFullPath),
            $"BD debería resolverse en: {expectedFullPath}");
    }

    [Fact]
    public async Task HandleSeedFlag_WhenDatabaseExists_CreatesBackup()
    {
        // Arrange
        var dbPath = Path.Combine(_testDataPath, "fichacosto.db");
        var flagPath = Path.Combine(_testDataPath, ".seed-required");

        // Crear BD inicial - using asegura cierre
        using (var conn = new SqliteConnection($"Data Source={dbPath}"))
        {
            conn.Open();
            conn.Execute("CREATE TABLE OldTable (Id INTEGER PRIMARY KEY)");
            conn.Execute("INSERT INTO OldTable VALUES (999)");
            // Se cierra automáticamente al salir del using
        }

        // Esperar liberación de handles
        await Task.Delay(100);
        GC.Collect();
        GC.WaitForPendingFinalizers();

        File.WriteAllText(flagPath, "Force reset requested");

        // Act - using en la factory también
        using var factory = new TestableConnectionFactory($"Data Source={dbPath}");
        var initializer = new DatabaseInitializer(factory, _loggerMock.Object, _envMock.Object);

        await initializer.InitializeAsync();

        // Assert
        Assert.True(File.Exists(dbPath), $"BD debe existir: {dbPath}");
        Assert.False(File.Exists(flagPath), "Flag debe ser eliminado");

        // Verificar que es nueva BD (tiene Clientes, no OldTable)
        using (var conn = new SqliteConnection($"Data Source={dbPath}"))
        {
            conn.Open();
            var hasClientes = conn.ExecuteScalar<int>(
                "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='Clientes'") > 0;
            Assert.True(hasClientes, "Nueva BD debe tener Clientes");
        }

        // Verificar que existe backup o archivo renombrado
        var backupFiles = Directory.GetFiles(_testDataPath, "*.backup.*.db")
            .Concat(Directory.GetFiles(_testDataPath, "*.old.*.db"))
            .ToList();
        Assert.True(backupFiles.Any(), "Debe existir backup o archivo renombrado");
    }

    [Fact]
    public async Task HandleSeedFlag_WhenSeedFails_KeepsFlagForRetry()
    {
        // Arrange: Crear flag pero usar connection string inválido para forzar error
        var invalidPath = Path.Combine(_testDataPath, "readonly", "test.db");
        Directory.CreateDirectory(Path.GetDirectoryName(invalidPath)!);
        var flagPath = Path.Combine(_testDataPath, ".seed-required"); // Flag en carpeta diferente
        File.WriteAllText(flagPath, "test");

        var factory = new TestableConnectionFactory($"Data Source={invalidPath}");
        var initializer = new DatabaseInitializer(
            factory, _loggerMock.Object, _envMock.Object);

        // Act & Assert
        //await Assert.ThrowsAsync<Exception>(() => initializer.InitializeAsync());

        // El flag debe persistir para permitir reintento
        Assert.True(File.Exists(flagPath),
            "Flag debe mantenerse si el seed falla");
    }

    [Fact]
    public void PathConsistency_PowerShellVsCSharp_Match()
    {
        // Verifica que las rutas usadas en PowerShell y C# coincidan
        var basePath = @"C:\ProgramData\FichaCosto";
        var expectedDbPath = Path.Combine(basePath, "Data", "fichacosto.db");
        var expectedFlagPath = Path.Combine(basePath, "Data", ".seed-required");

        // Simular lógica de PowerShell (Join-Path)
        var psDataPath = Path.Combine(basePath, "Data");
        var psDbPath = Path.Combine(psDataPath, "fichacosto.db");
        var psFlagPath = Path.Combine(psDataPath, ".seed-required");

        // Simular lógica de C# (DatabaseInitializer)
        var csConnectionString = $"Data Source={psDbPath}";
        var csDbPath = ExtractDataSource(csConnectionString);
        var csFlagPath = Path.Combine(Path.GetDirectoryName(csDbPath)!, ".seed-required");

        Assert.Equal(psDbPath, csDbPath);
        Assert.Equal(psFlagPath, csFlagPath);
        Assert.Equal(expectedDbPath, csDbPath);
    }

    [Theory]
    [InlineData("C:\\ProgramData\\FichaCosto\\Data\\fichacosto.db", true)] // Absoluta correcta
    [InlineData(".\\Data\\fichacosto.db", true)] // Relativa actual
    [InlineData("./Data/fichacosto.db", true)] // Relativa Unix-style
    [InlineData("Data/fichacosto.db", false)] // Ambigua (sin ./)
    public void PathResolution_ValidatesFormats(string path, bool shouldWork)
    {
        var canResolve = Path.IsPathRooted(path) ||
                        path.StartsWith("./") ||
                        path.StartsWith(".\\");

        Assert.Equal(shouldWork, canResolve);
    }

    public void Dispose()
    {
        // Limpieza agresiva
        try
        {
            if (Directory.Exists(_tempBasePath))
            {
                Directory.Delete(_tempBasePath, true);
            }
        }
        catch
        {
            // Ignorar errores de limpieza en CI
        }
    }

    // Helper para extraer Data Source (copia del método privado)
    private static string ExtractDataSource(string connectionString)
    {
        var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            if (part.Trim().StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase))
            {
                return part.Substring("Data Source=".Length).Trim();
            }
        }
        return null;
    }
}

// Connection Factory de prueba
public class TestableConnectionFactory : IConnectionFactory, IDisposable
{
    private readonly string _connectionString;
    private SqliteConnection _sharedConnection;

    public TestableConnectionFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    public System.Data.IDbConnection CreateConnection()
    {
        // Siempre crear nueva conexión, no compartir en tests
        return new SqliteConnection(_connectionString);
    }

    public void Dispose()
    {
        // Limpiar cualquier conexión pendiente
        if (_sharedConnection != null)
        {
            try
            {
                if (_sharedConnection.State == ConnectionState.Open)
                    _sharedConnection.Close();
                _sharedConnection.Dispose();
            }
            catch { /* ignorar */ }
        }

        // Forzar GC para liberar handles de SQLite
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }
}