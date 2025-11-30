using FluentAssertions;
using Moq;
using observatorio.saude.Application.Queries.GetNumeroEstabelecimentos;
using observatorio.saude.Application.Services.Clients;
using observatorio.saude.Domain.Dto;
using observatorio.saude.Domain.Interface;
using observatorio.saude.Infra.Services.Response.Ibge;

namespace observatorio.saude.tests.Application.Queries.GetNumeroEstabelecimentos;

public class GetNumeroEstabelecimentosEstadoHandlerTest
{
    private readonly GetContagemEstabelecimentosPorEstadoHandler _handler;
    private readonly Mock<IIbgeApiClient> _ibgeClientMock;
    private readonly Mock<IEstabelecimentoRepository> _repoMock;

    public GetNumeroEstabelecimentosEstadoHandlerTest()
    {
        _repoMock = new Mock<IEstabelecimentoRepository>();
        _ibgeClientMock = new Mock<IIbgeApiClient>();
        _handler = new GetContagemEstabelecimentosPorEstadoHandler(_repoMock.Object, _ibgeClientMock.Object);
    }

    [Fact]
    public async Task Handle_QuandoDadosCompletos_DeveRetornarDtoCorreto()
    {
        var contagemRepo = new List<NumeroEstabelecimentoEstadoDto>
        {
            new()
            {
                NomeUf = "Distrito Federal",
                Regiao = "Centro-Oeste",
                SiglaUf = "DF", CodUf = 35, TotalEstabelecimentos = 1500
            },
            new()
            {
                NomeUf = "Distrito Federal",
                Regiao = "Centro-Oeste",
                SiglaUf = "DF", CodUf = 33, TotalEstabelecimentos = 1200
            },
            new()
            {
                NomeUf = "Distrito Federal",
                Regiao = "Centro-Oeste",
                SiglaUf = "DF", CodUf = 99, TotalEstabelecimentos = 50
            }
        };

        var ufsIbge = new List<UfDataResponse>
        {
            new()
            {
                Id = 35, Nome = "São Paulo", Sigla = "SP",
                Regiao = new RegiaoResponse { Id = 3, Nome = "Sudeste", Sigla = "SE" }
            },
            new()
            {
                Id = 33, Nome = "Rio de Janeiro", Sigla = "RJ",
                Regiao = new RegiaoResponse { Id = 3, Nome = "Sudeste", Sigla = "SE" }
            }
        };

        var populacaoIbge = new List<IbgeUfResponse>
        {
            new()
            {
                Resultados = new List<Resultado>
                {
                    new()
                    {
                        Series = new List<Serie>
                        {
                            new()
                            {
                                Localidade = new Localidade { Id = "35" },
                                SerieData = new Dictionary<string, string> { { "2025", "45000000" } }
                            }
                        }
                    }
                }
            },
            new()
            {
                Resultados = new List<Resultado>
                {
                    new()
                    {
                        Series = new List<Serie>
                        {
                            new()
                            {
                                Localidade = new Localidade { Id = "33" },
                                SerieData = new Dictionary<string, string> { { "2025", "17000000" } }
                            }
                        }
                    }
                }
            }
        };

        var populacaoZeroResultado = new PopulacaoUfResultado(2025, populacaoIbge);

        _repoMock.Setup(r => r.GetContagemPorEstadoAsync(null)).ReturnsAsync(contagemRepo);
        _ibgeClientMock.Setup(c => c.FindUfsAsync()).ReturnsAsync(ufsIbge);
        _ibgeClientMock.Setup(c => c.FindPopulacaoUfAsync(null)).ReturnsAsync(populacaoZeroResultado);

        var result = await _handler.Handle(new GetNumerostabelecimentosPorEstadoQuery(), CancellationToken.None);

        result.Should().NotBeNull().And.HaveCount(3);
        var spResult = result.FirstOrDefault(x => x.SiglaUf == "SP");
        spResult.Should().NotBeNull();
        spResult.CodUf.Should().Be(35);
        spResult.NomeUf.Should().Be("São Paulo");
        spResult.SiglaUf.Should().Be("SP");
        spResult.Regiao.Should().Be("Sudeste");
        spResult.TotalEstabelecimentos.Should().Be(1500);
        spResult.Populacao.Should().Be(45000000);
        spResult.CoberturaEstabelecimentos.Should().Be(Math.Round(1500d / 45000000 * 100000, 2));

        var rjResult = result.FirstOrDefault(x => x.SiglaUf == "RJ");
        rjResult.Should().NotBeNull();
        rjResult.CodUf.Should().Be(33);
        rjResult.NomeUf.Should().Be("Rio de Janeiro");
        rjResult.SiglaUf.Should().Be("RJ");
        rjResult.Regiao.Should().Be("Sudeste");
        rjResult.TotalEstabelecimentos.Should().Be(1200);
        rjResult.Populacao.Should().Be(17000000);
        rjResult.CoberturaEstabelecimentos.Should().Be(Math.Round(1200d / 17000000 * 100000, 2));

        var ufSemDados = result.FirstOrDefault(x => x.CodUf == 99);
        ufSemDados.Should().NotBeNull();
        ufSemDados.TotalEstabelecimentos.Should().Be(50);
        ufSemDados.Populacao.Should().Be(0);
        ufSemDados.CoberturaEstabelecimentos.Should().Be(0);

        _repoMock.Verify(r => r.GetContagemPorEstadoAsync(null), Times.Once);
        _ibgeClientMock.Verify(c => c.FindUfsAsync(), Times.Once);
        _ibgeClientMock.Verify(c => c.FindPopulacaoUfAsync(null), Times.Once);
    }
}