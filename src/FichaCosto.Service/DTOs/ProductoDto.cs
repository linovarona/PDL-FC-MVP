// DTOs/ProductoDto.cs
using FichaCosto.Service.Models.Enums;

namespace FichaCosto.Service.DTOs
{
    public class ProductoDto
    {
        public int Id { get; set; }
        public int ClienteId { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public UnidadMedida UnidadMedida { get; set; }
        public bool Activo { get; set; }
        public DateTime FechaCreacion { get; set; }
    }
}