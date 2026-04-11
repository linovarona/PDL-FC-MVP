using System.Data;
using Dapper;
using FichaCosto.Repositories.Interfaces;

namespace FichaCosto.Service.Data;

/// <summary>
/// Inicializador de base de datos SQLite con Dapper
/// Soporta: Producción (conexiones nuevas) y Tests (conexión compartida)
/// </summary>
public class DatabaseInitializer
{
    private readonly IConnectionFactory _connectionFactory;
    private readonly ILogger<DatabaseInitializer> _logger;
    private readonly IHostEnvironment _environment;
    private readonly string _basePath;

    public DatabaseInitializer(
        IConnectionFactory connectionFactory,
        ILogger<DatabaseInitializer> logger,
        IHostEnvironment environment)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));

        _basePath = AppContext.BaseDirectory;
    }

    /// <summary>
    /// Inicializa la base de datos: crea directorio, ejecuta schema y seed data
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            _logger.LogInformation("Inicializando base de datos SQLite [Environment: {Environment}]...",
                _environment.EnvironmentName);

            // 1. Asegurar directorio de datos (solo en producción, no en tests en memoria)
            await EnsureDataDirectoryAsync();

            // 2. Verificar si necesita inicialización
            using var connection = _connectionFactory.CreateConnection();

            var tableCount = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%';"
            );

            if (tableCount > 0)
            {
                _logger.LogInformation("Base de datos ya inicializada con {Count} tablas", tableCount);

                // Verificar si necesita seeding adicional (tablas vacías)
                await ConditionalSeedAsync(connection);
                return;
            }

            // 3. Ejecutar schema SQL
            await ExecuteSchemaAsync(connection);

            // 4. Seed data según ambiente
            if (IsTestEnvironment())
            {
                await SeedMinimalTestDataAsync(connection);
            }
            else
            {
                await SeedProductionDataAsync(connection);
            }

            _logger.LogInformation("Base de datos inicializada correctamente");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al inicializar la base de datos");
            throw;
        }
    }

    /// <summary>
    /// Crea directorio de datos si es necesario (solo producción)
    /// </summary>
    private async Task EnsureDataDirectoryAsync()
    {
        // En tests con SQLite en memoria (:memory:) o tests, no crear directorio
        if (IsTestEnvironment())
        {
            _logger.LogDebug("Modo test detectado, omitiendo creación de directorio");
            return;
        }

        try
        {
            // Extraer ruta de la connection string de forma segura
            var connectionString = GetConnectionStringFromFactory();
            var dbPath = ExtractDataSource(connectionString);

            if (string.IsNullOrEmpty(dbPath) || dbPath == ":memory:")
            {
                return; // SQLite en memoria
            }

            var directory = Path.GetDirectoryName(dbPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                _logger.LogInformation("Directorio de datos creado: {Directory}", directory);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo verificar/crear directorio de datos (puede ser normal en tests)");
        }
    }

    /// <summary>
    /// Ejecuta Schema.sql desde archivo embebido o disco
    /// </summary>
    private async Task ExecuteSchemaAsync(IDbConnection connection)
    {
        var schemaPath = ResolveSchemaPath();

        if (!File.Exists(schemaPath))
        {
            _logger.LogWarning("Schema.sql no encontrado en: {Path}, usando schema embebido", schemaPath);
            await ExecuteEmbeddedSchemaAsync(connection);
            return;
        }

        var sql = await File.ReadAllTextAsync(schemaPath);
        await ExecuteSqlBatchedAsync(connection, sql);

        _logger.LogInformation("Schema SQL ejecutado desde: {Path}", schemaPath);
    }

    /// <summary>
    /// Resuelve la ruta del Schema.sql
    /// </summary>
    private string ResolveSchemaPath()
    {
        // 1. Intentar en directorio de ejecución (producción publicada)
        var path = Path.Combine(_basePath, "Data", "Schema.sql");
        if (File.Exists(path)) return path;

        // 2. Intentar en estructura de proyecto (desarrollo)
        path = Path.Combine(_basePath, "..", "..", "..", "Data", "Schema.sql");
        if (File.Exists(path)) return path;

        // 3. Intentar buscar recursivamente (para tests)
        var possiblePaths = new[]
        {
            Path.Combine(Directory.GetCurrentDirectory(), "Data", "Schema.sql"),
            Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "Data", "Schema.sql"),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "src", "FichaCosto.Service", "Data", "Schema.sql")
        };

        foreach (var possible in possiblePaths)
        {
            if (File.Exists(possible)) return possible;
        }

        return Path.Combine(_basePath, "Data", "Schema.sql"); // default, fallará si no existe
    }

    /// <summary>
    /// Ejecuta schema embebido como fallback
    /// </summary>
    private async Task ExecuteEmbeddedSchemaAsync(IDbConnection connection)
    {
        // Schema mínimo embebido para que no falle si falta archivo
        const string embeddedSchema = @"
            -- Schema mínimo embebido (fallback)
            CREATE TABLE IF NOT EXISTS Clientes (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                NombreEmpresa TEXT NOT NULL,
                CUIT TEXT NOT NULL UNIQUE,
                Direccion TEXT,
                ContactoNombre TEXT,
                ContactoEmail TEXT,
                ContactoTelefono TEXT,
                Activo INTEGER DEFAULT 1,
                FechaAlta TEXT DEFAULT (datetime('now'))
            );
            -- Nota: En producción, Schema.sql debe existir con el schema completo
        ";

        await connection.ExecuteAsync(embeddedSchema);
        _logger.LogWarning("Se usó schema embebido mínimo. Verificar que Schema.sql esté presente en producción.");
    }

    /// <summary>
    /// Seed mínimo para tests unitarios (datos estables, predecibles)
    /// </summary>
    private async Task SeedMinimalTestDataAsync(IDbConnection connection)
    {
        _logger.LogInformation("Insertando datos mínimos para tests...");

        // Usar transacción para asegurar integridad
        using var transaction = connection.BeginTransaction();

        try
        {
            // 1. Cliente test ID=1
            await connection.ExecuteAsync(@"
            INSERT INTO Clientes (Id, NombreEmpresa, CUIT, Direccion, ContactoNombre, ContactoEmail, Activo, FechaAlta)
            VALUES (1, 'Cliente Test S.A.', '30111111111', 'Direccion Test 123', 'Test User', 'test@test.com', 1, datetime('now'));
        ", transaction: transaction);

            // 2. Producto test ID=1
            await connection.ExecuteAsync(@"
            INSERT INTO Productos (Id, ClienteId, Codigo, Nombre, Descripcion, UnidadMedida, Activo, FechaCreacion)
            VALUES (1, 1, 'TEST-001', 'Producto Test Unitario', 'Descripción para tests automáticos', 5, 1, datetime('now'));
        ", transaction: transaction);

            // 3. Materias primas test
            await connection.ExecuteAsync(@"
            INSERT INTO MateriasPrimas (Id, ProductoId, Nombre, CodigoInterno, Cantidad, CostoUnitario, Observaciones, Orden, Activo)
            VALUES 
                (1, 1, 'Materia Prima A Test', 'MP-TEST-A', 10.0, 100.00, 'Insumo test A', 1, 1),
                (2, 1, 'Materia Prima B Test', 'MP-TEST-B', 5.0, 200.00, 'Insumo test B', 2, 1);
        ", transaction: transaction);

            // 4. Mano de obra test
            await connection.ExecuteAsync(@"
            INSERT INTO ManoObraDirecta (Id, ProductoId, Horas, SalarioHora, PorcentajeCargasSociales, DescripcionTarea)
            VALUES (1, 1, 2.0, 150.00, 35.5, 'Tarea de test - Ensamblaje manual');
        ", transaction: transaction);

            // 5. FICHA DE COSTO - Insertar con todos los campos requeridos
            // Verificar primero si el schema tiene las columnas que usamos
            var fichaSql = @"
            INSERT INTO FichasCosto (
                Id, ProductoId, FechaCalculo,
                CostoMateriasPrimas, CostoManoObra, CostosDirectosTotales,
                MargenUtilidad, PrecioVentaCalculado, EstadoValidacion,
                ObservacionesValidacion, NumeroResolucionAplicada, GeneradoPor, VersionCalculo,
                CostoTotal, PrecioVentaSugerido, Observaciones, CalculadoPor
            ) VALUES (
                1, 1, datetime('now'),
                2000.00, 468.00, 2468.00,
                30.00, 3525.71, 1,
                'Ficha generada automáticamente para tests', '209/2024', 'TestSystem', '1.0.0-TEST',
                2468.00, 3525.71, 'Datos estables para pruebas unitarias', 'TestSystem'
            );";

            try
            {
                await connection.ExecuteAsync(fichaSql, transaction: transaction);
                _logger.LogInformation("FichaCosto insertada correctamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error insertando FichaCosto. SQL: {Sql}", fichaSql);
                throw; // Re-lanzar para ver el error real
            }

            transaction.Commit();
            _logger.LogInformation("Datos de test insertados: Cliente=1, Producto=1, Ficha=1");
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            _logger.LogError(ex, "Error en SeedMinimalTestDataAsync, rollback ejecutado");
            throw;
        }
    }

    /// <summary>
    /// Seed completo para producción (desde SeedData.sql o datos reales)
    /// </summary>
    private async Task SeedProductionDataAsync(IDbConnection connection)
    {
        _logger.LogInformation("Insertando datos de producción...");

        // Intentar cargar desde SeedData.sql
        var seedPath = Path.Combine(_basePath, "Data", "SeedData.sql");

        if (!File.Exists(seedPath))
        {
            // Fallback: buscar en otras ubicaciones
            seedPath = Path.Combine(_basePath, "..", "..", "..", "Data", "SeedData.sql");
        }

        if (File.Exists(seedPath))
        {
            var sql = await File.ReadAllTextAsync(seedPath);
            await ExecuteSqlBatchedAsync(connection, sql);
            _logger.LogInformation("SeedData.sql ejecutado: {Path}", seedPath);
        }
        else
        {
            _logger.LogWarning("SeedData.sql no encontrado, usando seed mínimo");
            await SeedMinimalTestDataAsync(connection); // Fallback seguro
        }
    }

    /// <summary>
    /// Seeding condicional: si tablas están vacías en producción, agregar datos
    /// </summary>
    private async Task ConditionalSeedAsync(IDbConnection connection)
    {
        if (IsTestEnvironment()) return; // No modificar datos en tests

        // Verificar si Clientes está vacío (podría ser BD existente sin datos)
        var clientesCount = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Clientes");

        if (clientesCount == 0)
        {
            _logger.LogInformation("Tablas vacías detectadas en BD existente, aplicando seed...");
            await SeedProductionDataAsync(connection);
        }
    }

    /// <summary>
    /// Ejecuta SQL dividiendo por sentencias (maneja GO, comentarios, etc.)
    /// </summary>
    private async Task ExecuteSqlBatchedAsync(IDbConnection connection, string sql)
    {
        // Limpiar comentarios de línea
        var lines = sql.Split('\n')
            .Where(line => !line.TrimStart().StartsWith("--"))
            .Select(line => line.Split("--")[0]); // Remover comentarios inline

        var cleanSql = string.Join("\n", lines);

        // Dividir por ; pero respetar bloques
        var commands = cleanSql.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(cmd => cmd.Trim())
            .Where(cmd => !string.IsNullOrWhiteSpace(cmd))
            .ToList();

        using var transaction = connection.BeginTransaction();
        try
        {
            foreach (var command in commands)
            {
                // Ignorar pragmas y comandos especiales de SQLite que pueden fallar en batch
                if (command.Trim().StartsWith("PRAGMA", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        await connection.ExecuteAsync(command, transaction: transaction);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug("PRAGMA ignorado (puede ser normal en transacción): {Message}", ex.Message);
                    }
                    continue;
                }

                await connection.ExecuteAsync(command, transaction: transaction);
            }

            transaction.Commit();
            _logger.LogDebug("Ejecutados {Count} comandos SQL", commands.Count);
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    /// <summary>
    /// Detecta si estamos en ambiente de test
    /// </summary>
    private bool IsTestEnvironment()
    {
        return _environment.EnvironmentName == "Testing"
            || _environment.EnvironmentName == "Test"
            || _environment.IsEnvironment("Testing");
    }

    /// <summary>
    /// Obtiene connection string de forma segura (para crear directorio)
    /// </summary>
    private string GetConnectionStringFromFactory()
    {
        // Intentar obtener del factory si es posible (reflection como último recurso)
        // O usar configuración inyectada si está disponible

        // Fallback: buscar en variables de entorno o appsettings
        var envConnection = Environment.GetEnvironmentVariable("CONNECTIONSTRINGS__DEFAULTCONNECTION");
        if (!string.IsNullOrEmpty(envConnection)) return envConnection;

        return "Data Source=./Data/fichacosto.db"; // default
    }

    /// <summary>
    /// Extrae Data Source de connection string
    /// </summary>
    private static string ExtractDataSource(string connectionString)
    {
        if (string.IsNullOrEmpty(connectionString)) return null;

        var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            var trimmed = part.Trim();
            if (trimmed.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("DataSource=", StringComparison.OrdinalIgnoreCase))
            {
                var index = trimmed.IndexOf('=');
                if (index > 0)
                {
                    return trimmed.Substring(index + 1).Trim();
                }
            }
        }
        return null;
    }
}