using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using observatorio.saude.Domain.Entities;

namespace observatorio.saude.tests.Domain.Entities;

public class EstabelecimentoTest
{
    private static Estabelecimento CriarEntidadeValida()
    {
        return new Estabelecimento
        {
            CodCnes = 1234567,
            DataExtracao = DateTime.Now,
            Caracteristicas = new CaracteristicaEstabelecimento { CodUnidade = "1" },
            Localizacao = new Localizacao { CodUnidade = "1234567" },
            Organizacao = new Organizacao(),
            Turno = new Turno(),
            Servico = new Servico()
        };
    }

    private static (bool IsValid, ICollection<ValidationResult> Results) ValidarModelo(Estabelecimento entidade)
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

    [Fact]
    public void CodCnesQuandoForDefaultDeveSerInvalido()
    {
        var entidade = CriarEntidadeValida();
        entidade.CodCnes = 0;

        var (isValid, results) = ValidarModelo(entidade);

        isValid.Should().BeFalse();
        results.Should().HaveCount(1);
        results.First().MemberNames.Should().Contain(nameof(Estabelecimento.CodCnes));
    }

    [Fact]
    public void DataExtracaoQuandoForDefaultDeveSerInvalida()
    {
        var entidade = CriarEntidadeValida();
        entidade.DataExtracao = default;

        var (isValid, results) = ValidarModelo(entidade);

        isValid.Should().BeFalse();
        results.Should().HaveCount(1);
        results.First().MemberNames.Should().Contain(nameof(Estabelecimento.DataExtracao));
    }
}