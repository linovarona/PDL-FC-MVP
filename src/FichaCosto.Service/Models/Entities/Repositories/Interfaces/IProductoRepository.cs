using FichaCosto.Service.Models.Entities;

namespace FichaCosto.Repositories.Interfaces
{
    public interface IProductoRepository
    {
        Task<Producto?> GetByIdAsync(int id);
        Task<Producto?> GetByIdWithDetailsAsync(int id);
        Task<IEnumerable<Producto>> GetByClienteIdAsync(int clienteId);
        Task<IEnumerable<Producto>> GetAllAsync();
        Task<int> CreateAsync(Producto producto);
        Task<bool> UpdateAsync(Producto producto);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsByCodigoAsync(string codigo, int? excludeId = null);
    }
}