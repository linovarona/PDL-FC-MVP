using FichaCosto.Service.Models.DTOs;
using FichaCosto.Service.Models.Enums;

namespace FichaCosto.Service.Services.Interfaces
{
    /// <summary>
    /// Servicio de validación de fichas según Resolución 209/2024
    /// </summary>
    public interface IValidadorFichaService
    {
        /// <summary>
        /// Valida un cálculo completo de ficha de costo
        /// </summary>
        /// <param name="resultado">Resultado del cálculo a validar</param>
        /// <returns>Resultado de la validación con estado y mensajes</returns>
        Task<ResultadoValidacionDto> ValidarCalculoAsync(ResultadoCalculoDto resultado);

        /// <summary>
        /// Valida si el margen de utilidad cumple los límites
        /// Res. 209/2024: Máximo 30%
        /// </summary>
        /// <param name="margenUtilidad">Porcentaje de margen</param>
        /// <returns>True si es válido, false si excede el límite</returns>
        bool ValidarMargenGanancia(decimal margenUtilidad);

        /// <summary>
        /// Obtiene el nivel de alerta según el margen
        /// </summary>
        /// <param name="margenUtilidad">Porcentaje de margen</param>
        /// <returns>Nivel de alerta (Verde, Amarillo, Rojo)</returns>
        NivelAlertaMargen ObtenerNivelAlerta(decimal margenUtilidad);
    }

    /// <summary>
    /// Niveles de alerta para el margen de utilidad
    /// </summary>
    public enum NivelAlertaMargen
    {
        Verde = 1,   // < 25% - Dentro de límites normales
        Amarillo = 2, // 25% - 30% - Cercano al límite
        Rojo = 3      // > 30% - Excede el límite legal
    }
}