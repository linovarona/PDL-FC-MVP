using Dapper;
using FichaCosto.Repositories.Interfaces;
using FichaCosto.Service.Models.Entities;
using System.Data;

namespace FichaCosto.Repositories.Implementations
{
    public class ProductoRepository : IProductoRepository
    {
        private readonly IConnectionFactory _connectionFactory;
        private readonly ILogger<ProductoRepository> _logger;

        public ProductoRepository(IConnectionFactory connectionFactory, ILogger<ProductoRepository> logger)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;
        }

        public async Task<Producto?> GetByIdAsync(int id)
        {
            const string sql = "SELECT * FROM Productos WHERE Id = @Id";
            using var connection = _connectionFactory.CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<Producto>(sql, new { Id = id });
        }

        public async Task<Producto?> GetByIdWithDetailsAsync(int id)
        {
            const string sqlProducto = "SELECT * FROM Productos WHERE Id = @Id";
            const string sqlMateriasPrimas = "SELECT * FROM MateriasPrimas WHERE ProductoId = @Id";
            const string sqlManoObra = "SELECT * FROM ManoObraDirecta WHERE ProductoId = @Id";

            using var connection = _connectionFactory.CreateConnection();

            var producto = await connection.QueryFirstOrDefaultAsync<Producto>(sqlProducto, new { Id = id });
            if (producto == null) return null;

            var materiasPrimas = await connection.QueryAsync<MateriaPrima>(sqlMateriasPrimas, new { Id = id });
            var manoObra = await connection.QueryAsync<ManoObraDirecta>(sqlManoObra, new { Id = id });

            producto.MateriasPrimas = materiasPrimas.ToList();
            producto.ManoObras = manoObra.ToList();

            return producto;
        }

        public async Task<IEnumerable<Producto>> GetByClienteIdAsync(int clienteId)
        {
            const string sql = "SELECT * FROM Productos WHERE ClienteId = @ClienteId ORDER BY Nombre";
            using var connection = _connectionFactory.CreateConnection();
            return await connection.QueryAsync<Producto>(sql, new { ClienteId = clienteId });
        }

        public async Task<IEnumerable<Producto>> GetAllAsync()
        {
            const string sql = "SELECT * FROM Productos ORDER BY Nombre";
            using var connection = _connectionFactory.CreateConnection();
            return await connection.QueryAsync<Producto>(sql);
        }

        public async Task<int> CreateAsync(Producto producto)
        {
            const string sql = @"
                INSERT INTO Productos (ClienteId, Codigo, Nombre, Descripcion, UnidadMedida, Activo, FechaCreacion)
                VALUES (@ClienteId, @Codigo, @Nombre, @Descripcion, @UnidadMedida, @Activo, @FechaCreacion);
                SELECT last_insert_rowid();";

            using var connection = _connectionFactory.CreateConnection();
            var id = await connection.ExecuteScalarAsync<int>(sql, producto);
            _logger.LogInformation("Producto creado con ID: {Id}", id);
            return id;
        }

        public async Task<bool> UpdateAsync(Producto producto)
        {
            const string sql = @"
                UPDATE Productos 
                SET ClienteId = @ClienteId, Codigo = @Codigo, Nombre = @Nombre, 
                    Descripcion = @Descripcion, UnidadMedida = @UnidadMedida, Activo = @Activo
                WHERE Id = @Id";

            using var connection = _connectionFactory.CreateConnection();
            var rowsAffected = await connection.ExecuteAsync(sql, producto);
            return rowsAffected > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            const string sql = "DELETE FROM Productos WHERE Id = @Id";
            using var connection = _connectionFactory.CreateConnection();
            var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id });
            return rowsAffected > 0;
        }

        public async Task<bool> ExistsByCodigoAsync(string codigo, int? excludeId = null)
        {
            var sql = "SELECT COUNT(1) FROM Productos WHERE Codigo = @Codigo";
            if (excludeId.HasValue)
                sql += " AND Id != @ExcludeId";

            using var connection = _connectionFactory.CreateConnection();
            var count = await connection.ExecuteScalarAsync<int>(sql, new { Codigo = codigo, ExcludeId = excludeId });
            return count > 0;
        }
    }
}