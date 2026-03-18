using ClosedXML.Excel;
using FichaCosto.Service.Models.DTOs;
using FichaCosto.Service.Models.Entities;
using FichaCosto.Service.Services.Interfaces;

namespace FichaCosto.Service.Services.Implementations
{
    /// <summary>
    /// Implementación del servicio Excel usando ClosedXML
    /// Compatible con .xlsx, no requiere Microsoft Office
    /// </summary>
    public class ExcelService : IExcelService
    {
        private readonly ILogger<ExcelService> _logger;

        public ExcelService(ILogger<ExcelService> logger)
        {
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<MateriaPrima>> ImportarMateriasPrimasAsync(Stream stream)
        {
            var materiasPrimas = new List<MateriaPrima>();

            try
            {
                using var workbook = new XLWorkbook(stream);
                var worksheet = workbook.Worksheet(1); // Primera hoja

                // Validar encabezados
                if (!ValidarEncabezadosMateriasPrimas(worksheet))
                {
                    throw new FormatException("Formato de archivo inválido. Encabezados esperados: Código, Nombre, Cantidad, CostoUnitario");
                }

                // Leer filas (asumiendo fila 1 = encabezados, datos desde fila 2)
                var rows = worksheet.RangeUsed().RowsUsed().Skip(1); // Skip header

                foreach (var row in rows)
                {
                    try
                    {
                        var mp = new MateriaPrima
                        {
                            CodigoInterno = row.Cell(1).GetString(),
                            Nombre = row.Cell(2).GetString(),
                            Cantidad = row.Cell(3).GetValue<decimal>(), //  .GetDecimal(),
                            CostoUnitario = row.Cell(4).GetValue<decimal>(), //GetDecimal(),
                            Activo = true
                        };

                        // Validaciones básicas
                        if (string.IsNullOrWhiteSpace(mp.Nombre))
                        {
                            _logger.LogWarning("Fila {Fila} ignorada: Nombre vacío", row.RowNumber());
                            continue;
                        }

                        if (mp.Cantidad <= 0 || mp.CostoUnitario < 0)
                        {
                            _logger.LogWarning("Fila {Fila} ignorada: Valores inválidos (Cantidad: {Cantidad}, Costo: {Costo})",
                                row.RowNumber(), mp.Cantidad, mp.CostoUnitario);
                            continue;
                        }

                        materiasPrimas.Add(mp);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error procesando fila {Fila}", row.RowNumber());
                    }
                }

                _logger.LogInformation("Importadas {Count} materias primas desde Excel", materiasPrimas.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importando materias primas desde Excel");
                throw;
            }

            return await Task.FromResult(materiasPrimas);
        }

        /// <inheritdoc/>
        public async Task<ManoObraDirecta?> ImportarManoObraAsync(Stream stream)
        {
            try
            {
                using var workbook = new XLWorkbook(stream);
                var worksheet = workbook.Worksheet("ManoObra"); // Hoja específica o primera

                // Buscar hoja "ManoObra" o usar primera
                if (worksheet == null)
                {
                    worksheet = workbook.Worksheet(1);
                }

                // Leer celdas específicas (formato simple)
                var horas = worksheet.Cell("B1").GetValue<decimal>();
                var salario = worksheet.Cell("B2").GetValue<decimal>();
                var cargas = worksheet.Cell("B3").GetValue<decimal>();

                if (horas <= 0 || salario <= 0)
                {
                    _logger.LogWarning("Datos de mano de obra inválidos en Excel");
                    return null;
                }

                var manoObra = new ManoObraDirecta
                {
                    Horas = horas,
                    SalarioHora = salario,
                    PorcentajeCargasSociales = cargas > 0 ? cargas : 35.5m, // Default si no especifica
                    DescripcionTarea = worksheet.Cell("B4").GetString() ?? "Tarea no especificada"
                };

                _logger.LogInformation("Importada mano de obra: {Horas}h, ${Salario}/h",
                    manoObra.Horas, manoObra.SalarioHora);

                return await Task.FromResult(manoObra);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importando mano de obra desde Excel");
                return null;
            }
        }

        /// <inheritdoc/>
        public async Task<Stream> ExportarFichaCostoAsync(ResultadoCalculoDto resultado)
        {
            try
            {
                using var workbook = new XLWorkbook();

                // Hoja 1: Resumen
                var wsResumen = workbook.Worksheets.Add("Resumen");
                CrearHojaResumen(wsResumen, resultado);

                // Hoja 2: Desglose de Costos
                var wsCostos = workbook.Worksheets.Add("Desglose Costos");
                CrearHojaDesglose(wsCostos, resultado);

                // Guardar en stream
                var stream = new MemoryStream();
                workbook.SaveAs(stream);
                stream.Position = 0; // Reset para lectura

                _logger.LogInformation("Ficha de costo exportada para ProductoId: {ProductoId}", resultado.ProductoId);

                return await Task.FromResult(stream);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exportando ficha de costo a Excel");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<Stream> GenerarPlantillaAsync()
        {
            try
            {
                using var workbook = new XLWorkbook();

                // Hoja 1: Materias Primas
                var wsMp = workbook.Worksheets.Add("MateriasPrimas");
                wsMp.Cell("A1").Value = "Código";
                wsMp.Cell("B1").Value = "Nombre";
                wsMp.Cell("C1").Value = "Cantidad";
                wsMp.Cell("D1").Value = "CostoUnitario";

                // Formato encabezados
                var range = wsMp.Range("A1:D1");
                range.Style.Font.Bold = true;
                range.Style.Fill.BackgroundColor = XLColor.LightGray;

                // Ajustar anchos
                wsMp.Column(1).Width = 15;
                wsMp.Column(2).Width = 30;
                wsMp.Column(3).Width = 12;
                wsMp.Column(4).Width = 15;

                // Hoja 2: Mano de Obra
                var wsMo = workbook.Worksheets.Add("ManoObra");
                wsMo.Cell("A1").Value = "Horas:";
                wsMo.Cell("B1").Value = 0;
                wsMo.Cell("A2").Value = "Salario/Hora:";
                wsMo.Cell("B2").Value = 0;
                wsMo.Cell("A3").Value = "% Cargas Sociales:";
                wsMo.Cell("B3").Value = 35.5;
                wsMo.Cell("A4").Value = "Descripción:";
                wsMo.Cell("B4").Value = "";

                var stream = new MemoryStream();
                workbook.SaveAs(stream);
                stream.Position = 0;

                return await Task.FromResult(stream);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generando plantilla Excel");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<(bool esValido, List<string> errores)> ValidarFormatoAsync(Stream stream)
        {
            var errores = new List<string>();

            try
            {
                using var workbook = new XLWorkbook(stream);
                var worksheet = workbook.Worksheet(1);

                // Verificar que tenga datos
                if (worksheet.LastRowUsed()?.RowNumber() < 2)
                {
                    errores.Add("El archivo no contiene datos (solo encabezados o vacío)");
                }

                // Verificar encabezados básicos
                var header1 = worksheet.Cell("A1").GetString().ToLower();
                if (!header1.Contains("codigo") && !header1.Contains("código"))
                {
                    errores.Add("Columna A debe contener 'Código'");
                }

                return await Task.FromResult((errores.Count == 0, errores));
            }
            catch (Exception ex)
            {
                errores.Add($"Error leyendo archivo: {ex.Message}");
                return (false, errores);
            }
        }

        #region Métodos Privados

        private bool ValidarEncabezadosMateriasPrimas(IXLWorksheet worksheet)
        {
            var h1 = worksheet.Cell("A1").GetString().ToLower();
            var h2 = worksheet.Cell("B1").GetString().ToLower();

            return h1.Contains("codigo") || h1.Contains("código") ||
                   h2.Contains("nombre");
        }

        private void CrearHojaResumen(IXLWorksheet ws, ResultadoCalculoDto resultado)
        {
            // Título
            ws.Cell("A1").Value = "FICHA DE COSTO";
            ws.Cell("A1").Style.Font.Bold = true;
            ws.Cell("A1").Style.Font.FontSize = 16;

            // Datos generales
            ws.Cell("A3").Value = "Producto:";
            ws.Cell("B3").Value = resultado.ProductoNombre;
            ws.Cell("A4").Value = "Fecha Cálculo:";
            ws.Cell("B4").Value = resultado.FechaCalculo.ToString(); //("yyyy-MM-dd HH:mm");
            ws.Cell("A5").Value = "Resolución:";
            ws.Cell("B5").Value = resultado.NumeroResolucionAplicada;

            // Costos
            ws.Cell("A7").Value = "COSTOS DIRECTOS";
            ws.Cell("A7").Style.Font.Bold = true;
            ws.Cell("A8").Value = "Materias Primas:";
            ws.Cell("B8").Value = resultado.CostoMateriasPrimas;
            ws.Cell("B8").Style.NumberFormat.Format = "$#,##0.00";

            ws.Cell("A9").Value = "Mano de Obra:";
            ws.Cell("B9").Value = resultado.CostoManoObra;
            ws.Cell("B9").Style.NumberFormat.Format = "$#,##0.00";

            ws.Cell("A10").Value = "Total Costos Directos:";
            ws.Cell("B10").Value = resultado.CostosDirectosTotales;
            ws.Cell("B10").Style.NumberFormat.Format = "$#,##0.00";
            ws.Cell("B10").Style.Font.Bold = true;

            // Margen y precio
            ws.Cell("A12").Value = "Margen Utilidad:";
            ws.Cell("B12").Value = resultado.MargenUtilidad / 100m;
            ws.Cell("B12").Style.NumberFormat.Format = "0.00%";

            ws.Cell("A13").Value = "Precio Venta:";
            ws.Cell("B13").Value = resultado.PrecioVentaCalculado;
            ws.Cell("B13").Style.NumberFormat.Format = "$#,##0.00";
            ws.Cell("B13").Style.Font.Bold = true;
            ws.Cell("B13").Style.Font.FontColor = XLColor.Green;

            // Estado
            ws.Cell("A15").Value = "Estado Validación:";
            ws.Cell("B15").Value = resultado.EstadoValidacion.ToString();

            // LÍNEAS 300-303 CORREGIDAS:
            if (resultado.ObservacionesValidacion != null && resultado.ObservacionesValidacion.Any())
            {
                ws.Cell("A16").Value = "Observaciones:";
                // Convertir List<string> a string con saltos de línea
                ws.Cell("B16").Value = string.Join(Environment.NewLine, resultado.ObservacionesValidacion);
                // Opcional: ajustar alto de fila para mostrar todo el texto
                ws.Row(16).Height = 15 * Math.Max(1, resultado.ObservacionesValidacion.Count);
            }

            //if (!string.IsNullOrEmpty(resultado.ObservacionesValidacion))
            //{
            //    ws.Cell("A16").Value = "Observaciones:";
            //    ws.Cell("B16").Value = resultado.ObservacionesValidacion;
            //}

            // Ajustar anchos
            ws.Column(1).Width = 25;
            ws.Column(2).Width = 20;
        }

        private void CrearHojaDesglose(IXLWorksheet ws, ResultadoCalculoDto resultado)
        {
            ws.Cell("A1").Value = "DESGLOSE DE CÁLCULOS";
            ws.Cell("A1").Style.Font.Bold = true;

            // Aquí se pueden agregar fórmulas detalladas si se tiene acceso 
            // a los datos originales (materias primas individuales, etc.)
            // Por ahora, resumen de totales

            ws.Cell("A3").Value = "Concepto";
            ws.Cell("B3").Value = "Monto";
            ws.Cell("C3").Value = "Fórmula";

            var header = ws.Range("A3:C3");
            header.Style.Font.Bold = true;
            header.Style.Fill.BackgroundColor = XLColor.LightGray;

            ws.Cell("A4").Value = "Materias Primas";
            ws.Cell("B4").Value = resultado.CostoMateriasPrimas;
            ws.Cell("C4").Value = "Σ(Cantidad × CostoUnitario)";

            ws.Cell("A5").Value = "Mano de Obra";
            ws.Cell("B5").Value = resultado.CostoManoObra;
            ws.Cell("C5").Value = "Horas × Salario × (1 + Cargas%)";

            ws.Cell("A6").Value = "Costos Directos";
            ws.Cell("B6").Value = resultado.CostosDirectosTotales;
            ws.Cell("C6").Value = "MP + MO";

            ws.Cell("A8").Value = "Precio Venta";
            ws.Cell("B8").Value = resultado.PrecioVentaCalculado;
            ws.Cell("C8").Value = $"Costos × (1 + {resultado.MargenUtilidad}%)";

            ws.Column(1).Width = 20;
            ws.Column(2).Width = 15;
            ws.Column(3).Width = 35;
        }

        #endregion
    }
}