using FichaCosto.Service.Models.DTOs;
using FichaCosto.Service.Models.Entities;


namespace FichaCosto.Service.Services.Interfaces
{
    /// <summary>
    /// Servicio de importación y exportación de datos desde/hacia Excel
    /// Usa ClosedXML (LGPL) para manipulación sin dependencia de Office
    /// </summary>
    public interface IExcelService
    {
        /// <summary>
        /// Importa materias primas desde un archivo Excel
        /// Formato esperado: Columnas A:Código, B:Nombre, C:Cantidad, D:CostoUnitario
        /// </summary>
        /// <param name="stream">Stream del archivo Excel</param>
        /// <returns>Lista de materias primas importadas</returns>
        Task<IEnumerable<MateriaPrima>> ImportarMateriasPrimasAsync(Stream stream);

        /// <summary>
        /// Importa mano de obra desde Excel
        /// Formato: Horas, SalarioHora, PorcentajeCargasSociales
        /// </summary>
        Task<ManoObraDirecta?> ImportarManoObraAsync(Stream stream);

        /// <summary>
        /// Exporta una ficha de costo calculada a Excel
        /// </summary>
        /// <param name="resultado">Resultado del cálculo</param>
        /// <returns>Stream del archivo Excel generado</returns>
        Task<Stream> ExportarFichaCostoAsync(ResultadoCalculoDto resultado);

        /// <summary>
        /// Genera plantilla Excel vacía para ingreso de datos
        /// </summary>
        Task<Stream> GenerarPlantillaAsync();

        /// <summary>
        /// Valida que el archivo Excel tenga el formato correcto
        /// </summary>
        Task<(bool esValido, List<string> errores)> ValidarFormatoAsync(Stream stream);
    }
}