// src/FichaCosto.Service/Data/DatabaseInitializer.cs
using Dapper;
using FichaCosto.Repositories.Interfaces;
using Microsoft.Data.Sqlite;
using System.Data;

namespace FichaCosto.Service.Data;

public class DatabaseInitializer
{
    private readonly IConnectionFactory _connectionFactory;
    private readonly ILogger<DatabaseInitializer> _logger;
    private readonly IHostEnvironment _environment;
    private readonly string _basePath;
    private readonly string _schemaPath;

    public DatabaseInitializer(
        IConnectionFactory connectionFactory,
        ILogger<DatabaseInitializer> logger,
        IHostEnvironment environment)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
        _environment = environment;
        _basePath = AppContext.BaseDirectory;

        _schemaPath = Path.Combine(_basePath, "Data", "Schema.sql");

        if (!File.Exists(_schemaPath))
        {
            _schemaPath = Path.Combine(_basePath, "..", "..", "..", "Data", "Schema.sql");
        }
    }

    public async Task InitializeAsync()
    {
        try
        {
            _logger.LogInformation("Inicializando base de datos SQLite [Environment: {Environment}]...",
                _environment.EnvironmentName);

            // Crear directorio de datos
            await EnsureDataDirectoryAsync();

            using var connection = _connectionFactory.CreateConnection();
            if (connection.State != ConnectionState.Open)
                connection.Open();

            // Verificar si ya existe schema
            var tableCount = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%';"
            );

            if (tableCount > 0)
            {
                _logger.LogInformation("Base de datos ya inicializada con {Count} tablas", tableCount);
                return;
            }

            // SOLO crear schema, SIN datos de seed
            await ExecuteSchemaAsync(connection);

            _logger.LogInformation("Schema creado exitosamente. BD lista para seed de datos.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al inicializar la base de datos");
            throw;
        }
    }

    private async Task EnsureDataDirectoryAsync()
    {
        try
        {
            var dbPath = GetDatabasePath();
            if (string.IsNullOrEmpty(dbPath) || dbPath == ":memory:") return;

            var directory = Path.GetDirectoryName(dbPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                _logger.LogInformation("Directorio de datos creado: {Directory}", directory);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo crear directorio de datos");
        }
    }

    private async Task ExecuteSchemaAsync(IDbConnection connection)
    {
        var schemaPath = ResolveSchemaPath();

        if (!File.Exists(schemaPath))
        {
            _logger.LogError("Schema.sql no encontrado en: {Path}", schemaPath);
            throw new FileNotFoundException("Schema.sql no encontrado", schemaPath);
        }

        var sql = await File.ReadAllTextAsync(schemaPath);

        if (connection.State != ConnectionState.Open)
            connection.Open();

        await ExecuteSqlBatchedAsync(connection, sql);
        _logger.LogInformation("Schema SQL ejecutado: {Path}", schemaPath);
    }

    private async Task ExecuteSqlBatchedAsync(IDbConnection connection, string sql)
    {
        // Limpiar comentarios
        var lines = sql.Split('\n')
            .Where(line => !line.TrimStart().StartsWith("--"))
            .Select(line => line.Split("--")[0]);

        var cleanSql = string.Join("\n", lines);

        var commands = cleanSql.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(cmd => cmd.Trim())
            .Where(cmd => !string.IsNullOrWhiteSpace(cmd))
            .ToList();

        using var transaction = connection.BeginTransaction();
        try
        {
            foreach (var command in commands)
            {
                if (command.Trim().StartsWith("PRAGMA", StringComparison.OrdinalIgnoreCase))
                {
                    try { await connection.ExecuteAsync(command, transaction: transaction); }
                    catch { /* ignorar PRAGMAs que fallen */ }
                    continue;
                }
                await connection.ExecuteAsync(command, transaction: transaction);
            }
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    private string ResolveSchemaPath()
    {
        var paths = new[]
        {
            Path.Combine(_basePath, "Data", "Schema.sql"),
            Path.Combine(_basePath, "..", "..", "..", "Data", "Schema.sql"),
            Path.Combine(Directory.GetCurrentDirectory(), "Data", "Schema.sql"),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "src", "FichaCosto.Service", "Data", "Schema.sql")
        };

        foreach (var path in paths)
        {
            if (File.Exists(path)) return path;
        }

        return paths[0];
    }

    private string GetDatabasePath()
    {
        try
        {
            using var connection = _connectionFactory.CreateConnection();
            if (connection is SqliteConnection sqliteConn)
            {
                var connStr = sqliteConn.ConnectionString;
                var dataSource = ExtractDataSource(connStr);

                if (!string.IsNullOrEmpty(dataSource) && !Path.IsPathRooted(dataSource))
                {
                    dataSource = Path.Combine(_basePath, dataSource.TrimStart('.', '\\', '/'));
                }
                return dataSource;
            }
        }
        catch { }
        return null;
    }

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
                var idx = trimmed.IndexOf('=');
                if (idx > 0) return trimmed.Substring(idx + 1).Trim();
            }
        }
        return null;
    }
}