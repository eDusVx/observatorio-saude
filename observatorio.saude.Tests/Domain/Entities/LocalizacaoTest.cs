using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using observatorio.saude.Domain.Entities;

namespace observatorio.saude.tests.Domain.Entities;

public class LocalizacaoTest
{
    private static Localizacao CriarEntidadeValida()
    {
        return new Localizacao
        {
            CodUnidade = "UNIDADE-VALIDA-123",
            CodCep = 12345678,
            Endereco = "Avenida dos Testes Unitários",
            Numero = 101,
            Bairro = "Centro",
            Latitude = -23.5505m,
            Longitude = -46.6333m,
            CodIbge = 3550308,
            CodUf = 35
        };
    }

    private static (bool IsValid, ICollection<ValidationResult> Results) ValidarModelo(Localizacao entidade)
    {
        var validationResults = new List<ValidationResult>();
        var context = new ValidationContext(entidade, null, null);
        var isValid = Validator.TryValidateObject(entidade, context, validationResults, true);
        return (isValid, validationResults);
    }

    [Fact]
    public void EntidadeComDadosValidosDeveSerConsideradaValida()
    {
        var entidade = CriarEntidadeValida();

        var (isValid, results) = ValidarModelo(entidade);

        isValid.Should().BeTrue();
        results.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CodUnidadeQuandoNuloOuVazioDeveSerInvalido(string codUnidade)
    {
        var entidade = CriarEntidadeValida();
        entidade.CodUnidade = codUnidade;

        var (isValid, results) = ValidarModelo(entidade);

        isValid.Should().BeFalse();
        results.Should().HaveCount(1);
        results.First().MemberNames.Should().Contain(nameof(Localizacao.CodUnidade));
    }
}