using FichaCosto.Service.Models.Entities;
using FichaCosto.Service.Models.Enums;
using Xunit;

// Alias para resolver conflicto de nombres: namespace vs clase FichaCosto
using FichaCostoEntity = FichaCosto.Service.Models.Entities.FichaCosto;

namespace FichaCosto.Service.Tests;

public class EntityTests
{
    [Fact]
    public void MateriaPrima_Calculates_CostoTotal()
    {
        var mp = new MateriaPrima
        {
            Cantidad = 10,
            CostoUnitario = 5.5m
        };

        Assert.Equal(55.0m, mp.CostoTotal);
    }

    [Fact]
    public void ManoObra_Calculates_CostoTotal()
    {
        var mo = new ManoObraDirecta
        {
            Horas = 10,
            SalarioHora = 100,
            PorcentajeCargasSociales = 35.5m
        };

        Assert.Equal(1355.0m, mo.CostoTotal);
    }

        
    [Fact]
    public void FichaCosto_Has_Valid_Defaults()
    {
        var ficha = new FichaCostoEntity { ProductoId = 1 };

        Assert.Equal(EstadoValidacion.Valido, ficha.EstadoValidacion);
        Assert.Equal("209/2024", ficha.NumeroResolucionAplicada);
        Assert.Equal("1.0.0-MVP", ficha.VersionCalculo);
        Assert.Equal("Sistema", ficha.GeneradoPor);
    }
}