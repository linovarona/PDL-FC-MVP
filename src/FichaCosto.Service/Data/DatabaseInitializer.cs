using System.Data;
using Dapper;
using Microsoft.Data.Sqlite;

namespace FichaCosto.Service.Data;

/// <summary>
/// Inicializador de base de datos SQLite con Dapper
/// </summary>
public class DatabaseInitializer
{
    private readonly string _connectionString;
    private readonly ILogger<DatabaseInitializer> _logger;
    private readonly string _schemaPath;

    public DatabaseInitializer(IConfiguration configuration, ILogger<DatabaseInitializer> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Data Source=./Data/fichacosto.db";
        _logger = logger;

        // Resolver ruta del schema SQL
        var baseDir = AppContext.BaseDirectory;
        _schemaPath = Path.Combine(baseDir, "Data", "Schema.sql");

        // Si no existe en binario, buscar en proyecto (modo desarrollo)
        if (!File.Exists(_schemaPath))
        {
            _schemaPath = Path.Combine(baseDir, "..", "..", "..", "Data", "Schema.sql");
        }
    }

    /// <summary>
    /// Inicializa la base de datos: crea archivo, ejecuta schema y seed data
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            _logger.LogInformation("Inicializando base de datos SQLite...");

            // 1. Asegurar que existe el directorio
            var dbPath = ExtractDataSource(_connectionString);
            var directory = Path.GetDirectoryName(dbPath);

            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                _logger.LogInformation("Directorio de datos creado: {Directory}", directory);
            }

            // 2. Verificar si la BD ya existe y tiene tablas
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var tableCount = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%';"
            );

            if (tableCount > 0)
            {
                _logger.LogInformation("Base de datos ya inicializada con {Count} tablas", tableCount);
                return;
            }

            // 3. Ejecutar schema SQL
            await ExecuteSchemaAsync(connection);

            // 4. Insertar datos de prueba (seed)
            await SeedDataAsync(connection);

            _logger.LogInformation("Base de datos inicializada correctamente");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al inicializar la base de datos");
            throw;
        }
    }

    /// <summary>
    /// Ejecuta el script Schema.sql
    /// </summary>
    private async Task ExecuteSchemaAsync(SqliteConnection connection)
    {
        if (!File.Exists(_schemaPath))
        {
            throw new FileNotFoundException($"No se encontró el archivo de esquema: {_schemaPath}");
        }

        var sql = await File.ReadAllTextAsync(_schemaPath);

        // Dividir por sentencias (simplificado para SQLite)
        var commands = sql.Split(new[] { ";" }, StringSplitOptions.RemoveEmptyEntries)
            .Where(cmd => !string.IsNullOrWhiteSpace(cmd))
            .ToList();

        using var transaction = connection.BeginTransaction();
        try
        {
            foreach (var command in commands)
            {
                var trimmed = command.Trim();
                if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("--"))
                    continue;

                await connection.ExecuteAsync(trimmed, transaction: transaction);
                _logger.LogDebug("Ejecutado: {CommandPreview}",
                    trimmed.Length > 50 ? trimmed[..50] + "..." : trimmed);
            }

            transaction.Commit();
            _logger.LogInformation("Schema SQL ejecutado: {Count} comandos", commands.Count);
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    /// <summary>
    /// Inserta datos de prueba iniciales
    /// </summary>
    private async Task SeedDataAsync(SqliteConnection connection)
    {
        _logger.LogInformation("Insertando datos de prueba...");

        // Cliente de ejemplo
        var clienteId = await connection.ExecuteScalarAsync<int>(@"
            INSERT INTO Clientes (NombreEmpresa, CUIT, Direccion, ContactoNombre, ContactoEmail)
            VALUES ('PyME Ejemplo S.A.', '30123456789', 'Av. Siempre Viva 123', 'Juan Pérez', 'juan@pyme.com');
            SELECT last_insert_rowid();"
        );

        // Producto de ejemplo
        var productoId = await connection.ExecuteScalarAsync<int>(@"
            INSERT INTO Productos (ClienteId, Codigo, Nombre, Descripcion, UnidadMedida)
            VALUES (@ClienteId, 'PROD-001', 'Producto de Prueba', 'Descripción del producto MVP', 5);
            SELECT last_insert_rowid();",
            new { ClienteId = clienteId }
        );

        // Materias primas de ejemplo
        await connection.ExecuteAsync(@"
            INSERT INTO MateriasPrimas (ProductoId, Nombre, Cantidad, CostoUnitario, Orden)
            VALUES 
                (@ProductoId, 'Materia Prima A', 10, 15.50, 1),
                (@ProductoId, 'Materia Prima B', 5, 25.00, 2);",
            new { ProductoId = productoId }
        );

        // Mano de obra de ejemplo
        await connection.ExecuteAsync(@"
            INSERT INTO ManoObraDirecta (ProductoId, Horas, SalarioHora, PorcentajeCargasSociales, DescripcionTarea)
            VALUES (@ProductoId, 2.5, 850.00, 35.5, 'Ensamblaje manual');",
            new { ProductoId = productoId }
        );

        _logger.LogInformation("Datos de prueba insertados: Cliente {ClienteId}, Producto {ProductoId}",
            clienteId, productoId);
    }

    /// <summary>
    /// Extrae la ruta del Data Source de la connection string
    /// </summary>
    private static string ExtractDataSource(string connectionString)
    {
        var parts = connectionString.Split(';');
        foreach (var part in parts)
        {
            if (part.Trim().StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase))
            {
                return part.Substring("Data Source=".Length).Trim();
            }
        }
        return "fichacosto.db"; // default
    }
}