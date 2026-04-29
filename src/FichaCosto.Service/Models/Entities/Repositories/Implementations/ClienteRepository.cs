using Dapper;
using FichaCosto.Repositories.Interfaces;
using FichaCosto.Service.Models.Entities;
using System.Data;

namespace FichaCosto.Repositories.Implementations
{
    public class ClienteRepository : IClienteRepository
    {
        private readonly IConnectionFactory _connectionFactory;
        private readonly ILogger<ClienteRepository> _logger;

        public ClienteRepository(IConnectionFactory connectionFactory, ILogger<ClienteRepository> logger)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;
        }

        public async Task<Cliente?> GetByIdAsync(int id)
        {
            const string sql = "SELECT * FROM Clientes WHERE Id = @Id";
            using var connection = _connectionFactory.CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<Cliente>(sql, new { Id = id });
        }

        public async Task<IEnumerable<Cliente>> GetAllAsync()
        {
            const string sql = "SELECT * FROM Clientes ORDER BY NombreEmpresa";
            using var connection = _connectionFactory.CreateConnection();
            return await connection.QueryAsync<Cliente>(sql);
        }

        public async Task<int> CreateAsync(Cliente cliente)
        {
            const string sql = @"
                INSERT INTO Clientes (NombreEmpresa, CUIT, Direccion, ContactoNombre, ContactoTelefono, ContactoEmail, Activo, FechaAlta)
                VALUES (@NombreEmpresa, @CUIT, @Direccion, @ContactoNombre, @ContactoTelefono, @ContactoEmail, @Activo, @FechaAlta);
                SELECT last_insert_rowid();";

            using var connection = _connectionFactory.CreateConnection();
            var id = await connection.ExecuteScalarAsync<int>(sql, cliente);
            _logger.LogInformation("Cliente creado con ID: {Id}", id);
            return id;
        }

        public async Task<bool> UpdateAsync(Cliente cliente)
        {
            const string sql = @"
                UPDATE Clientes 
                SET NombreEmpresa = @NombreEmpresa, 
                    CUIT = @CUIT, 
                    Direccion = @Direccion, 
                    ContactoNombre = @ContactoNombre,
                    ContactoTelefono = @ContactoTelefono, 
                    ContactoEmail = @ContactoEmail, 
                    Activo = @Activo
                WHERE Id = @Id";

            using var connection = _connectionFactory.CreateConnection();
            var rowsAffected = await connection.ExecuteAsync(sql, cliente);
            return rowsAffected > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            const string sql = "DELETE FROM Clientes WHERE Id = @Id";
            using var connection = _connectionFactory.CreateConnection();
            var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id });
            return rowsAffected > 0;
        }

        public async Task<bool> ExistsByCuitAsync(string cuit)
        {
            const string sql = "SELECT COUNT(1) FROM Clientes WHERE CUIT = @CUIT";
            using var connection = _connectionFactory.CreateConnection();
            var count = await connection.ExecuteScalarAsync<int>(sql, new { CUIT = cuit });
            return count > 0;
        }
    }
}