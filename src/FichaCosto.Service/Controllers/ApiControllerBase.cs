using Microsoft.AspNetCore.Mvc;

namespace FichaCosto.Service.Controllers
{
    /// <summary>
    /// Controller base con configuración común para toda la API
    /// </summary>
    [ApiController]
    [Route("api / [controller]")]
    [Produces("application / json")]
    public abstract class ApiControllerBase : ControllerBase
    {
        /// <summary>
        /// Devuelve respuesta estandarizada para errores
        /// </summary>
        protected IActionResult ErrorResponse(string message, string details = null)
        {
            return BadRequest(new
            {
                error = message,
                details = details,
                timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Verifica si el modelo es válido y retorna BadRequest si no lo es
        /// </summary>
        protected IActionResult ValidateModel()
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(new { errors });
            }
            return null;
        }
    }
}