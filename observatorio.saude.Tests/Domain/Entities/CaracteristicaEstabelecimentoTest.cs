using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using observatorio.saude.Domain.Entities;

namespace observatorio.saude.tests.Domain.Entities;

public class CaracteristicaEstabelecimentoTests
{
    private static CaracteristicaEstabelecimento CriarEntidadeValida()
    {
        return new CaracteristicaEstabelecimento
        {
            CodUnidade = "1234567",
            NmRazaoSocial = "HOSPITAL MODELO LTDA",
            NmFantasia = "HOSPITAL MODELO",
            NumCnpj = "12.345.678/0001-99",
            NumCnpjEntidade = "98.765.432/0001-11",
            Email = "contato@hospitalmodelo.com",
            NumTelefone = "+55 (11) 98765-4321"
        };
    }

    private static (bool IsValid, ICollection<ValidationResult> Results) ValidarModelo(
        CaracteristicaEstabelecimento entidade)
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
        results.First().MemberNames.Should()
            .Contain(nameof(CaracteristicaEstabelecimento.CodUnidade));
    }

    [Theory]
    [InlineData("email.sem.arroba.com")]
    [InlineData("email@@duplo.com")]
    [InlineData("@inicio.com")]
    public void EmailComFormatoInvalidoDeveSerInvalido(string email)
    {
        var entidade = CriarEntidadeValida();
        entidade.Email = email;

        var (isValid, results) = ValidarModelo(entidade);

        isValid.Should().BeFalse();
        results.Should().Contain(r => r.MemberNames.Contains(nameof(CaracteristicaEstabelecimento.Email)));
    }

    [Fact]
    public void EmailComFormatoValidoDeveSerValido()
    {
        var entidade = CriarEntidadeValida();
        entidade.Email = "teste.valido@meudominio.com.br";

        var (isValid, _) = ValidarModelo(entidade);

        isValid.Should().BeTrue();
    }

    [Fact]
    public void TelefoneComFormatoInvalidoDeveSerInvalido()
    {
        var entidade = CriarEntidadeValida();
        entidade.NumTelefone = "isto nao e um telefone";

        var (isValid, results) = ValidarModelo(entidade);

        isValid.Should().BeFalse();
        results.Should().Contain(r => r.MemberNames.Contains(nameof(CaracteristicaEstabelecimento.NumTelefone)));
    }
}