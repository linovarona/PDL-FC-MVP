using Dapper;
using FichaCosto.Service.Models.Entities;
using FichaCosto.Repositories.Interfaces;
using Microsoft.Data.Sqlite;
using System.Data;

namespace FichaCosto.Repositories.Implementations
{
    public class ClienteRepository : IClienteRepository
    {
        private readonly string _connectionString;
        private readonly ILogger<ClienteRepository> _logger;

        public ClienteRepository(IConfiguration configuration, ILogger<ClienteRepository> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            _logger = logger;
        }

        private IDbConnection CreateConnection() => new SqliteConnection(_connectionString);

        public async Task<Cliente?> GetByIdAsync(int id)
        {
            const string sql = "SELECT * FROM Clientes WHERE Id = @Id";
            using var connection = CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<Cliente>(sql, new { Id = id });
        }

        public async Task<IEnumerable<Cliente>> GetAllAsync()
        {
            const string sql = "SELECT * FROM Clientes ORDER BY Nombre";
            using var connection = CreateConnection();
            return await connection.QueryAsync<Cliente>(sql);
        }

        public async Task<int> CreateAsync(Cliente cliente)
        {
            const string sql = @"
                INSERT INTO Clientes (NombreEmpresa, CUIT, Direccion, ContactoTelefono, ContactoEmail, Activo, FechaAlta)
                VALUES (@Nombre, @Cuit, @Direccion, @Telefono, @Email, @Activo, @FechaAlta);
                SELECT last_insert_rowid();";

            using var connection = CreateConnection();
            var id = await connection.ExecuteScalarAsync<int>(sql, cliente);
            _logger.LogInformation("Cliente creado con ID: {Id}", id);
            return id;
        }

        public async Task<bool> UpdateAsync(Cliente cliente)
        {
            const string sql = @"
                UPDATE Clientes 
                SET NombreEmpresa = @Nombre, CUIT = @Cuit, Direccion = @Direccion, 
                    ContactoTelefono = @Telefono, ContactoEmail = @Email, Activo = @Activo
                WHERE Id = @Id";

            using var connection = CreateConnection();
            var rowsAffected = await connection.ExecuteAsync(sql, cliente);
            return rowsAffected > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            const string sql = "DELETE FROM Clientes WHERE Id = @Id";
            using var connection = CreateConnection();
            var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id });
            return rowsAffected > 0;
        }

        public async Task<bool> ExistsByCuitAsync(string cuit)
        {
            const string sql = "SELECT COUNT(1) FROM Clientes WHERE CUIT = @Cuit";
            using var connection = CreateConnection();
            var count = await connection.ExecuteScalarAsync<int>(sql, new { CUIT = cuit });
            return count > 0;
        }
    }
}