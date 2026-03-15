using System.ComponentModel.DataAnnotations;
using FichaCosto.Service.Models.DTOs;
using Xunit;

namespace FichaCosto.Service.Tests;

public class DtoTests
{
    [Fact]
    public void FichaCostoDto_Validation_Margen_Excedido()
    {
        var dto = new FichaCostoDto
        {
            ProductoId = 1,
            MargenUtilidad = 35, // Excede 30%
            MateriasPrimas = new() { new() { Nombre = "Test", Cantidad = 1, CostoUnitario = 1 } },
            ManoObra = new() { Horas = 1, SalarioHora = 1 }
        };

        var validationResults = new List<ValidationResult>();
        var context = new ValidationContext(dto);
        var isValid = Validator.TryValidateObject(dto, context, validationResults, true);

        Assert.False(isValid);
        Assert.Contains(validationResults, v => v.ErrorMessage.Contains("30%"));
    }

    [Fact]
    public void FichaCostoDto_Validation_Margen_Valido()
    {
        var dto = new FichaCostoDto
        {
            ProductoId = 1,
            MargenUtilidad = 25, // Dentro del límite
            MateriasPrimas = new() { new() { Nombre = "Test", Cantidad = 1, CostoUnitario = 1 } },
            ManoObra = new() { Horas = 1, SalarioHora = 1 }
        };

        var validationResults = new List<ValidationResult>();
        var context = new ValidationContext(dto);
        var isValid = Validator.TryValidateObject(dto, context, validationResults, true);

        Assert.True(isValid);
    }

    [Fact]
    public void MateriaPrimaInputDto_Calculates_Correctly()
    {
        var mp = new MateriaPrimaInputDto
        {
            Nombre = "Madera",
            Cantidad = 10.5m,
            CostoUnitario = 25.75m
        };

        Assert.Equal(10.5m * 25.75m, mp.Cantidad * mp.CostoUnitario);
    }
}