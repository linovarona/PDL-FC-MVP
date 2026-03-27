// DTOs/MateriaPrimaDto.cs
namespace FichaCosto.Service.DTOs
{
    public class MateriaPrimaDto
    {
        public int Id { get; set; }
        public int ProductoId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? CodigoInterno { get; set; }
        public decimal Cantidad { get; set; }
        public decimal CostoUnitario { get; set; }
        public decimal CostoTotal => Cantidad * CostoUnitario;
        public string? Observaciones { get; set; }
        public int Orden { get; set; }
        public bool Activo { get; set; }
    }
}