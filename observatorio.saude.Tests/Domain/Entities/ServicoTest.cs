using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using observatorio.saude.Domain.Entities;

namespace observatorio.saude.tests.Domain.Entities;

public class ServicoTests
{
    private static Servico CriarEntidadeValida()
    {
        return new Servico
        {
            CodCnes = 1234567,
            TemCentroCirurgico = true,
            FazAtendimentoAmbulatorialSus = true,
            TemCentroObstetrico = false
        };
    }

    private static (bool IsValid, ICollection<ValidationResult> Results) ValidarModelo(Servico entidade)
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
        results.First().MemberNames.Should().Contain(nameof(Servico.CodCnes));
    }
}