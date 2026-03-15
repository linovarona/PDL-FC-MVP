namespace FichaCosto.Service.Models.Enums;

public enum EstadoValidacion
{
    /// <summary>Valor por defecto/no inicializado</summary>
    NoDefinido = 0,

    /// <summary>Margen válido (≤ 30%)</summary>
    Valido = 1,

    /// <summary>Margen en zona de alerta (25% - 30%)</summary>
    Advertencia = 2,

    /// <summary>Margen excedido (> 30%)</summary>
    Excedido = 3,

    /// <summary>Error en datos de entrada</summary>
    ErrorDatos = 4

}