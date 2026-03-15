namespace FichaCosto.Service.Models.Enums;

/// <summary>
/// Tipos de costos según Resolución 148/2023
/// </summary>
public enum TipoCosto
{
    /// <summary>
    /// Materias primas e insumos directos
    /// </summary>
    MateriaPrima = 1,

    /// <summary>
    /// Mano de obra directa
    /// </summary>
    ManoObra = 2,

    /// <summary>
    /// Costos indirectos de fabricación (post-MVP)
    /// </summary>
    CostoIndirecto = 3,

    /// <summary>
    /// Gastos generales (post-MVP)
    /// </summary>
    GastosGenerales = 4
}