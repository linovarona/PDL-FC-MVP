using Dapper;
using FichaCosto.Repositories.Interfaces;
using FichaCostoEntity = FichaCosto.Service.Models.Entities.FichaCosto;

using System.Data;

namespace FichaCosto.Repositories.Implementations
{
    public class FichaRepository : IFichaRepository
    {
        private readonly IConnectionFactory _connectionFactory;
        private readonly ILogger<FichaRepository> _logger;

        public FichaRepository(IConnectionFactory connectionFactory, ILogger<FichaRepository> logger)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;
        }

        public async Task<FichaCostoEntity?> GetByIdAsync(int id)
        {
            const string sql = "SELECT * FROM FichasCosto WHERE Id = @Id";
            using var connection = _connectionFactory.CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<FichaCostoEntity>(sql, new { Id = id });
        }

        public async Task<IEnumerable<FichaCostoEntity>> GetByProductoIdAsync(int productoId)
        {
            const string sql = @"
                SELECT * FROM FichasCosto 
                WHERE ProductoId = @ProductoId 
                ORDER BY FechaCalculo DESC";

            using var connection = _connectionFactory.CreateConnection();
            return await connection.QueryAsync<FichaCostoEntity>(sql, new { ProductoId = productoId });
        }

        public async Task<IEnumerable<FichaCostoEntity>> GetHistorialByProductoIdAsync(int productoId, int limit = 10)
        {
            const string sql = @"
                SELECT * FROM FichasCosto 
                WHERE ProductoId = @ProductoId 
                ORDER BY FechaCalculo DESC 
                LIMIT @Limit";

            using var connection = _connectionFactory.CreateConnection();
            return await connection.QueryAsync<FichaCostoEntity>(sql, new { ProductoId = productoId, Limit = limit });
        }

        public async Task<int> CreateAsync(FichaCostoEntity ficha)
        {
            // NOTA: Sin CostosIndirectos y GastosGenerales (post-MVP)
            const string sql = @"
                INSERT INTO FichasCosto (
                    ProductoId, FechaCalculo, CostoMateriasPrimas, CostoManoObra, 
                    CostoTotal, MargenUtilidad, PrecioVentaSugerido, 
                    EstadoValidacion, Observaciones, CalculadoPor, CostosDirectosTotales, PrecioVentaCalculado
                ) VALUES (
                    @ProductoId, @FechaCalculo, @CostoMateriasPrimas, @CostoManoObra, 
                    @CostoTotal, @MargenUtilidad, @PrecioVentaSugerido, 
                    @EstadoValidacion, @Observaciones, @CalculadoPor, @CostosDirectosTotales, @PrecioVentaCalculado
                );
                SELECT last_insert_rowid();";

            using var connection = _connectionFactory.CreateConnection();
            var id = await connection.ExecuteScalarAsync<int>(sql, ficha);
            _logger.LogInformation("Ficha creada con ID: {Id}", id);
            return id;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            const string sql = "DELETE FROM FichasCosto WHERE Id = @Id";
            using var connection = _connectionFactory.CreateConnection();
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

            using var connection = _connectionFactory.CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<FichaCostoEntity>(sql, new { ProductoId = productoId });
        }
    }
}