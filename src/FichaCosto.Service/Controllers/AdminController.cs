// src/FichaCosto.Service/Controllers/AdminController.cs
using Dapper;
using FichaCosto.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FichaCosto.Service.Controllers;  // <-- NAMESPACE CRÍTICO

[ApiController]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    private readonly IConnectionFactory _connectionFactory;
    private readonly ILogger<AdminController> _logger;

    public AdminController(IConnectionFactory connectionFactory, ILogger<AdminController> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    [HttpGet("health")]
    public IActionResult Health()
    {
        _logger.LogInformation("Health check llamado");
        return Ok(new
        {
            status = "OK",
            timestamp = DateTime.UtcNow,
            message = "AdminController funcionando"
        });
    }

    [HttpGet("check-data")]
    public async Task<IActionResult> CheckData()
    {
        try
        {
            _logger.LogInformation("CheckData llamado");
            using var connection = _connectionFactory.CreateConnection();
            connection.Open();

            var tableExists = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='Clientes'");

            if (tableExists == 0)
                return Ok(new { hasData = false, clientes = 0, tables = 0 });

            var count = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Clientes");
            return Ok(new { hasData = count > 0, clientes = count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en CheckData");
            return Ok(new { hasData = false, clientes = 0, error = ex.Message });
        }
    }

    [HttpPost("seed")]
    public async Task<IActionResult> Seed([FromBody] SeedRequest request)
    {
        _logger.LogInformation("Seed llamado con {Clientes} clientes", request?.Clientes?.Count ?? 0);

        try
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            var existing = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM Clientes", transaction: transaction);

            if (existing > 0)
            {
                transaction.Commit();
                return Ok(new { message = "Datos ya existen", skipped = true, clientes = existing });
            }

            foreach (var c in request.Clientes)
            {
                var sql = @"
                    INSERT INTO Clientes (NombreEmpresa, CUIT, Direccion, ContactoNombre, ContactoEmail, ContactoTelefono, Activo, FechaAlta)
                    VALUES (@NombreEmpresa, @CUIT, @Direccion, @ContactoNombre, @ContactoEmail, @ContactoTelefono, 1, datetime('now'))";
                await connection.ExecuteAsync(sql, c, transaction);
            }

            foreach (var p in request.Productos)
            {
                var sql = @"
                    INSERT INTO Productos (ClienteId, Codigo, Nombre, Descripcion, UnidadMedida, Activo, FechaCreacion)
                    VALUES (@ClienteId, @Codigo, @Nombre, @Descripcion, @UnidadMedida, 1, datetime('now'))";
                await connection.ExecuteAsync(sql, p, transaction);
            }

            transaction.Commit();
            return Ok(new { message = "Seed ejecutado", clientes = request.Clientes.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en Seed");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}

// DTOs en el mismo archivo o separados
public record SeedRequest(List<ClienteSeed> Clientes, List<ProductoSeed> Productos);
public record ClienteSeed(string NombreEmpresa, string CUIT, string Direccion, string ContactoNombre, string ContactoEmail, string ContactoTelefono);
public record ProductoSeed(int ClienteId, string Codigo, string Nombre, string Descripcion, int UnidadMedida);