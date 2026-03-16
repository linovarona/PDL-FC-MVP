using FichaCosto.Service.Models.Entities;
// Alias para resolver conflicto de nombres: namespace vs clase FichaCosto
using FichaCostoEntity = FichaCosto.Service.Models.Entities.FichaCosto;

namespace FichaCosto.Repositories.Interfaces
{
    public interface IFichaRepository
    {
        Task<FichaCostoEntity?> GetByIdAsync(int id);
        Task<IEnumerable<FichaCostoEntity>> GetByProductoIdAsync(int productoId);
        Task<IEnumerable<FichaCostoEntity>> GetHistorialByProductoIdAsync(int productoId, int limit = 10);
        Task<int> CreateAsync(FichaCostoEntity ficha);
        Task<bool> DeleteAsync(int id);
        Task<FichaCostoEntity?> GetUltimaFichaByProductoIdAsync(int productoId);
    }
}