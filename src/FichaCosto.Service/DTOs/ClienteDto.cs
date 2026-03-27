// DTOs/ClienteDto.cs
namespace FichaCosto.Service.DTOs
{
    public class ClienteDto
    {
        public int Id { get; set; }
        public string NombreEmpresa { get; set; } = string.Empty;
        public string CUIT { get; set; } = string.Empty;
        public string? Direccion { get; set; }
        public string? ContactoNombre { get; set; }
        public string? ContactoEmail { get; set; }
        public string? ContactoTelefono { get; set; }
        public bool Activo { get; set; }
        public DateTime FechaAlta { get; set; }
    }
}