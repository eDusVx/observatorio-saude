using FluentAssertions;
using Moq;
using observatorio.saude.Application.Queries.GetIndicadoresLeitos;
using observatorio.saude.Application.Services.Clients;
using observatorio.saude.Domain.Dto;
using observatorio.saude.Domain.Interface;
using observatorio.saude.Infra.Services.Response.Ibge;

namespace observatorio.saude.tests.Application.Queries.GetIndicadoresLeitos;

public class GetIndicadoresLeitosPorEstadoHandlerTest
{
    private readonly GetIndicadoresLeitosPorEstadoHandler _handler;
    private readonly Mock<IIbgeApiClient> _ibgeApiClientMock;
    private readonly Mock<ILeitosRepository> _leitoRepositoryMock;

    private readonly List<IbgeUfResponse> _mockPopulacao = new()
    {
        new IbgeUfResponse
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
                            SerieData = new Dictionary<string, string> { { "2023", "44000000" } }
                        }
                    }
                }
            }
        },
        new IbgeUfResponse
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
                            SerieData = new Dictionary<string, string> { { "2023", "16000000" } }
                        }
                    }
                }
            }
        }
    };

    private readonly List<UfDataResponse> _mockUfs = new()
    {
        new UfDataResponse
        {
            Id = 35, Sigla = "SP", Nome = "São Paulo",
            Regiao = new RegiaoResponse { Id = 1, Sigla = "SE", Nome = "Sudeste" }
        },
        new UfDataResponse
        {
            Id = 33, Sigla = "RJ", Nome = "Rio de Janeiro",
            Regiao = new RegiaoResponse { Id = 1, Sigla = "SE", Nome = "Sudeste" }
        },
        new UfDataResponse
        {
            Id = 29, Sigla = "BA", Nome = "Bahia",
            Regiao = new RegiaoResponse { Id = 3, Sigla = "NE", Nome = "Nordeste" }
        }
    };

    public GetIndicadoresLeitosPorEstadoHandlerTest()
    {
        var populacaoZeroResultado = new PopulacaoUfResultado(2023, _mockPopulacao);

        _leitoRepositoryMock = new Mock<ILeitosRepository>();
        _ibgeApiClientMock = new Mock<IIbgeApiClient>();

        _ibgeApiClientMock.Setup(c => c.FindUfsAsync()).ReturnsAsync(_mockUfs);
        _ibgeApiClientMock.Setup(c => c.FindPopulacaoUfAsync(It.IsAny<int?>())).ReturnsAsync(populacaoZeroResultado);

        _handler = new GetIndicadoresLeitosPorEstadoHandler(_leitoRepositoryMock.Object, _ibgeApiClientMock.Object);
    }

    private List<IndicadoresLeitosEstadoDto> GetMockIndicadores(int ano)
    {
        return new List<IndicadoresLeitosEstadoDto>
        {
            new() { CodUf = 35, TotalLeitos = 44000, LeitosSus = 4000, Criticos = 1000 },
            new() { CodUf = 33, TotalLeitos = 16000, LeitosSus = 8000, Criticos = 500 },
            new() { CodUf = 29, TotalLeitos = 0, LeitosSus = 0, Criticos = 0 },
            new() { CodUf = 99, TotalLeitos = 1000, LeitosSus = 100, Criticos = 50 }
        };
    }

    [Fact]
    public async Task Handle_QuandoSemFiltroUF_DeveChamarRepositorioComCodUfsNulo()
    {
        var anoAtual = DateTime.Now.Year;
        var query = new GetIndicadoresLeitosPorEstadoQuery { Ufs = null, Ano = null };
        var mockIndicadores = GetMockIndicadores(anoAtual);

        _leitoRepositoryMock
            .Setup(r => r.GetIndicadoresPorEstadoAsync(anoAtual, null, null, null))
            .ReturnsAsync(mockIndicadores);

        await _handler.Handle(query, CancellationToken.None);

        _leitoRepositoryMock.Verify(r => r.GetIndicadoresPorEstadoAsync(anoAtual, null, null, null), Times.Once);
    }

    [Fact]
    public async Task Handle_QuandoComFiltroUF_DeveMapearESolicitarCodUfsCorretos()
    {
        var ano = 2023;
        var ufsFiltro = new List<string> { "sP", "rj" };
        var codUfsEsperado = new List<long> { 35, 33 };
        var query = new GetIndicadoresLeitosPorEstadoQuery { Ufs = ufsFiltro, Ano = ano };
        var mockIndicadores = GetMockIndicadores(ano).Where(i => i.CodUf == 35 || i.CodUf == 33).ToList();

        _leitoRepositoryMock
            .Setup(r => r.GetIndicadoresPorEstadoAsync(ano, null,
                It.Is<List<long>>(c => c.SequenceEqual(codUfsEsperado)), null))
            .ReturnsAsync(mockIndicadores);

        await _handler.Handle(query, CancellationToken.None);

        _ibgeApiClientMock.Verify(c => c.FindUfsAsync(), Times.AtLeast(2));
        _leitoRepositoryMock.Verify(
            r => r.GetIndicadoresPorEstadoAsync(ano, null, It.Is<List<long>>(c => c.SequenceEqual(codUfsEsperado)),
                null),
            Times.Once);
    }

    [Fact]
    public async Task Handle_DeveCalcularIndicadoresEMapearDadosIbgeCorretamente()
    {
        var ano = 2023;
        var query = new GetIndicadoresLeitosPorEstadoQuery { Ano = ano };
        var mockIndicadores = GetMockIndicadores(ano);

        _leitoRepositoryMock
            .Setup(r => r.GetIndicadoresPorEstadoAsync(ano, null, null, null))
            .ReturnsAsync(mockIndicadores);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull().And.HaveCount(4);
        result.Should().BeInDescendingOrder(x => x.TotalLeitos);

        var sp = result.First(x => x.CodUf == 35);
        sp.NomeUf.Should().Be("São Paulo");
        sp.SiglaUf.Should().Be("SP");
        sp.Regiao.Should().Be("Sudeste");
        sp.Populacao.Should().Be(44000000);
        sp.CoberturaLeitosPor1kHab.Should().Be(1.00);

        var rj = result.First(x => x.CodUf == 33);
        rj.NomeUf.Should().Be("Rio de Janeiro");
        rj.Populacao.Should().Be(16000000);
        rj.CoberturaLeitosPor1kHab.Should().Be(1.00);

        var ba = result.First(x => x.CodUf == 29);
        ba.NomeUf.Should().Be("Bahia");
        ba.TotalLeitos.Should().Be(0);
        ba.CoberturaLeitosPor1kHab.Should().Be(0);

        var ufNaoMapeada = result.First(x => x.CodUf == 99);
        ufNaoMapeada.NomeUf.Should().BeNull();
        ufNaoMapeada.Populacao.Should().Be(0);
        ufNaoMapeada.CoberturaLeitosPor1kHab.Should().Be(0);

        _ibgeApiClientMock.Verify(c => c.FindPopulacaoUfAsync(ano), Times.Once);
        _ibgeApiClientMock.Verify(c => c.FindUfsAsync(), Times.Once);
    }

    [Fact]
    public async Task Handle_QuandoAnoNulo_DeveBuscarAnoAtual()
    {
        var anoAtual = DateTime.Now.Year;
        var query = new GetIndicadoresLeitosPorEstadoQuery { Ano = null };
        var mockIndicadores = GetMockIndicadores(anoAtual);

        _leitoRepositoryMock
            .Setup(r => r.GetIndicadoresPorEstadoAsync(anoAtual, null, null, null))
            .ReturnsAsync(mockIndicadores);

        await _handler.Handle(query, CancellationToken.None);

        _leitoRepositoryMock.Verify(r => r.GetIndicadoresPorEstadoAsync(anoAtual, null, null, null), Times.Once);
    }
}