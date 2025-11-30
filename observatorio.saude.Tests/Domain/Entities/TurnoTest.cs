using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using observatorio.saude.Domain.Entities;

namespace observatorio.saude.tests.Domain.Entities;

public class TurnoTest
{
    private static Turno CriarEntidadeValida()
    {
        return new Turno
        {
            CodTurnoAtendimento = 1,
            DscrTurnoAtendimento = "MANHA"
        };
    }

    private static (bool IsValid, ICollection<ValidationResult> Results) ValidarModelo(Turno entidade)
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
    public void CodTurnoAtendimentoQuandoForDefaultDeveSerInvalido()
    {
        var entidade = CriarEntidadeValida();
        entidade.CodTurnoAtendimento = 0;

        var (isValid, results) = ValidarModelo(entidade);

        isValid.Should().BeFalse();
        results.Should().HaveCount(1);
        results.First().MemberNames.Should().Contain(nameof(Turno.CodTurnoAtendimento));
    }
}