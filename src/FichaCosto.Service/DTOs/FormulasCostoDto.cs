namespace FichaCosto.Service.DTOs
{
    public class FormulasCostoDto
    {
        public string Resolucion { get; set; } = string.Empty;
        public List<FormulaDetalleDto> Formulas { get; set; } = new();
        public MargenMaximoDto MargenMaximo { get; set; } = new();
    }

    public class FormulaDetalleDto
    {
        public string Concepto { get; set; } = string.Empty;
        public string Formula { get; set; } = string.Empty;
        public string Ejemplo { get; set; } = string.Empty;
    }

    public class MargenMaximoDto
    {
        public string Resolucion { get; set; } = string.Empty;
        public int Porcentaje { get; set; }
        public string Descripcion { get; set; } = string.Empty;
    }
}