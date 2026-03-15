using Dapper;
//using FichaCosto.Service.Models.Entities;
using FichaCosto.Repositories.Interfaces;
using Microsoft.Data.Sqlite;
using System.Data;

// Alias para resolver conflicto de nombres: namespace vs clase FichaCosto
using FichaCostoEntity = FichaCosto.Service.Models.Entities.FichaCosto;

namespace FichaCosto.Repositories.Implementations
{
    public class FichaRepository : IFichaRepository
    {
        private readonly string _connectionString;
        private readonly ILogger<FichaRepository> _logger;

        public FichaRepository(IConfiguration configuration, ILogger<FichaRepository> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            _logger = logger;
        }

        private IDbConnection CreateConnection() => new SqliteConnection(_connectionString);

        public async Task<FichaCostoEntity?> GetByIdAsync(int id)
        {
            const string sql = "SELECT * FROM FichasCosto WHERE Id = @Id";
            using var connection = CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<FichaCostoEntity>(sql, new { Id = id });
        }

        public async Task<IEnumerable<FichaCostoEntity>> GetByProductoIdAsync(int productoId)
        {
            const string sql = @"
                SELECT * FROM FichasCosto 
                WHERE ProductoId = @ProductoId 
                ORDER BY FechaCalculo DESC";

            using var connection = CreateConnection();
            return await connection.QueryAsync<FichaCostoEntity>(sql, new { ProductoId = productoId });
        }

        public async Task<IEnumerable<FichaCostoEntity>> GetHistorialByProductoIdAsync(int productoId, int limit = 10)
        {
            const string sql = @"
                SELECT * FROM FichasCosto 
                WHERE ProductoId = @ProductoId 
                ORDER BY FechaCalculo DESC 
                LIMIT @Limit";

            using var connection = CreateConnection();
            return await connection.QueryAsync<FichaCostoEntity>(sql, new { ProductoId = productoId, Limit = limit });
        }

        public async Task<int> CreateAsync(FichaCostoEntity ficha)
        {
            const string sql = @"
                INSERT INTO FichasCosto (
                    ProductoId, FechaCalculo, CostoMateriasPrimas, CostoManoObra, 
                    CostosIndirectos, GastosGenerales, CostoTotal, MargenUtilidad, 
                    PrecioVentaSugerido, EstadoValidacion, Observaciones, CalculadoPor
                ) VALUES (
                    @ProductoId, @FechaCalculo, @CostoMateriasPrimas, @CostoManoObra, 
                    @CostosIndirectos, @GastosGenerales, @CostoTotal, @MargenUtilidad, 
                    @PrecioVentaSugerido, @EstadoValidacion, @Observaciones, @CalculadoPor
                );
                SELECT last_insert_rowid();";

            using var connection = CreateConnection();
            var id = await connection.ExecuteScalarAsync<int>(sql, ficha);
            _logger.LogInformation("Ficha de costo creada con ID: {Id} para Producto: {ProductoId}", id, ficha.ProductoId);
            return id;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            const string sql = "DELETE FROM FichasCosto WHERE Id = @Id";
            using var connection = CreateConnection();
            var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id });
            return rowsAffected > 0;
        }

        public async Task<FichaCostoEntity?> GetUltimaFichaByProductoIdAsync(int productoId)
        {
            const string sql = @"
                SELECT * FROM FichasCosto 
                WHERE ProductoId = @ProductoId 
                ORDER BY FechaCalculo DESC 
                LIMIT 1";

            using var connection = CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<FichaCostoEntity>(sql, new { ProductoId = productoId });
        }
    }
}