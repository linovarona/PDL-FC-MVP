// Mappings/EntityToDtoMappings.cs
using FichaCosto.Service.DTOs;
using FichaCosto.Service.Models.Entities;

namespace FichaCosto.Service.Mappings
{
    public static class EntityToDtoMappings
    {
        // Cliente → ClienteDto
        public static ClienteDto ToDto(this Cliente entity)
        {
            return new ClienteDto
            {
                Id = entity.Id,
                NombreEmpresa = entity.NombreEmpresa,
                CUIT = entity.CUIT,
                Direccion = entity.Direccion,
                ContactoNombre = entity.ContactoNombre,
                ContactoEmail = entity.ContactoEmail,
                ContactoTelefono = entity.ContactoTelefono,
                Activo = entity.Activo,
                FechaAlta = entity.FechaAlta
            };
        }

        // Producto → ProductoDto
        public static ProductoDto ToDto(this Producto entity)
        {
            return new ProductoDto
            {
                Id = entity.Id,
                ClienteId = entity.ClienteId,
                Codigo = entity.Codigo,
                Nombre = entity.Nombre,
                Descripcion = entity.Descripcion,
                UnidadMedida = entity.UnidadMedida,
                Activo = entity.Activo,
                FechaCreacion = entity.FechaCreacion
            };
        }

        // MateriaPrima → MateriaPrimaDto
        public static MateriaPrimaDto ToDto(this MateriaPrima entity)
        {
            return new MateriaPrimaDto
            {
                Id = entity.Id,
                ProductoId = entity.ProductoId,
                Nombre = entity.Nombre,
                CodigoInterno = entity.CodigoInterno,
                Cantidad = entity.Cantidad,
                CostoUnitario = entity.CostoUnitario,
                Observaciones = entity.Observaciones,
                Orden = entity.Orden,
                Activo = entity.Activo
            };
        }

        // Métodos para colecciones
        public static IEnumerable<ClienteDto> ToDtoList(this IEnumerable<Cliente> entities)
            => entities.Select(e => e.ToDto());

        public static IEnumerable<ProductoDto> ToDtoList(this IEnumerable<Producto> entities)
            => entities.Select(e => e.ToDto());

        public static IEnumerable<MateriaPrimaDto> ToDtoList(this IEnumerable<MateriaPrima> entities)
            => entities.Select(e => e.ToDto());
    }
}