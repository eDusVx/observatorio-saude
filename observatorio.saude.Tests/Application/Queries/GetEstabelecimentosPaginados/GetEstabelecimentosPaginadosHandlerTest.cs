using FluentAssertions;
using Moq;
using observatorio.saude.Application.Queries.GetEstabelecimentosPaginados;
using observatorio.saude.Application.Services.Clients;
using observatorio.saude.Domain.Interface;
using observatorio.saude.Domain.Utils;
using observatorio.saude.Infra.Models;
using observatorio.saude.Infra.Services.Response.Ibge;

namespace observatorio.saude.tests.Application.Queries.GetEstabelecimentosPaginados;

public class GetEstabelecimentosPaginadosHandlerTest
{
    private readonly GetEstabelecimentosPaginadosHandler _handler;
    private readonly Mock<IIbgeApiClient> _ibgeApiClientMock;
    private readonly Mock<IEstabelecimentoRepository> _repositoryMock;

    public GetEstabelecimentosPaginadosHandlerTest()
    {
        _repositoryMock = new Mock<IEstabelecimentoRepository>();
        _ibgeApiClientMock = new Mock<IIbgeApiClient>();
        _handler = new GetEstabelecimentosPaginadosHandler(_repositoryMock.Object, _ibgeApiClientMock.Object);
    }

    private PaginatedResult<EstabelecimentoModel> CriarResultadoFalsoDoRepositorio()
    {
        var itemModel = new EstabelecimentoModel
        {
            CodCnes = 1234567,
            DataExtracao = new DateTime(2025, 09, 08),
            CaracteristicaEstabelecimento = new CaracteristicaEstabelecimentoModel
                { CodUnidade = "1", NmFantasia = "Hospital de Teste" },
            Localizacao = new LocalizacaoModel { CodUnidade = "1", Bairro = "Bairro dos Testes", CodUf = 35 },
            Organizacao = new OrganizacaoModel { DscrEsferaAdministrativa = "1", TpGestao = 'M' },
            Turno = new TurnoModel { DscrTurnoAtendimento = "24 HORAS" },
            Servico = new ServicoModel { StCentroCirurgico = true, StFazAtendimentoAmbulatorialSus = false }
        };

        var items = new List<EstabelecimentoModel> { itemModel };

        return new PaginatedResult<EstabelecimentoModel>(items, 1, 10, 1);
    }

    [Fact]
    public async Task Handle_QuandoRepositorioRetornaDados_DeveMapearCorretamenteERetornarResultadoPaginado()
    {
        var query = new GetEstabelecimentosPaginadosQuery { PageNumber = 1, PageSize = 10 };
        var resultadoFalsoDoRepo = CriarResultadoFalsoDoRepositorio();

        _repositoryMock
            .Setup(r => r.GetPagedWithDetailsAsync(query.PageNumber, query.PageSize, null))
            .ReturnsAsync(resultadoFalsoDoRepo);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        result.CurrentPage.Should().Be(resultadoFalsoDoRepo.CurrentPage);
        result.PageSize.Should().Be(resultadoFalsoDoRepo.PageSize);
        result.TotalCount.Should().Be(resultadoFalsoDoRepo.TotalCount);

        var itemMapeado = result.Items.First();
        var itemOriginal = resultadoFalsoDoRepo.Items.First();

        itemMapeado.CodCnes.Should().Be(itemOriginal.CodCnes);
        itemMapeado.DataExtracao.Should().Be(itemOriginal.DataExtracao);
        itemMapeado.Caracteristicas!.NmFantasia.Should().Be(itemOriginal.CaracteristicaEstabelecimento!.NmFantasia);
        itemMapeado.Localizacao!.Bairro.Should().Be(itemOriginal.Localizacao!.Bairro);
        itemMapeado.Organizacao!.TpGestao.Should().Be(itemOriginal.Organizacao!.TpGestao);
        itemMapeado.Turno!.DscrTurnoAtendimento.Should().Be(itemOriginal.Turno!.DscrTurnoAtendimento);
        itemMapeado.Servico!.TemCentroCirurgico.Should().Be(itemOriginal.Servico!.StCentroCirurgico);
        itemMapeado.Servico!.FazAtendimentoAmbulatorialSus.Should()
            .Be(itemOriginal.Servico!.StFazAtendimentoAmbulatorialSus);

        _repositoryMock.Verify(r => r.GetPagedWithDetailsAsync(query.PageNumber, query.PageSize, null), Times.Once);
    }

    [Fact]
    public async Task Handle_QuandoRepositorioRetornaVazio_DeveRetornarResultadoPaginadoVazio()
    {
        var query = new GetEstabelecimentosPaginadosQuery { PageNumber = 1, PageSize = 10 };
        var resultadoVazioDoRepo =
            new PaginatedResult<EstabelecimentoModel>(new List<EstabelecimentoModel>(), 1, 10, 0);

        _repositoryMock
            .Setup(r => r.GetPagedWithDetailsAsync(query.PageNumber, query.PageSize, null))
            .ReturnsAsync(resultadoVazioDoRepo);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);

        _repositoryMock.Verify(r => r.GetPagedWithDetailsAsync(query.PageNumber, query.PageSize, null), Times.Once);
    }

    [Fact]
    public async Task Handle_QuandoFiltroPorUfEhInformado_DeveFiltrarPeloCodigoDaUf()
    {
        const string ufSigla = "SP";
        const int ufId = 35;
        var query = new GetEstabelecimentosPaginadosQuery { PageNumber = 1, PageSize = 10, Uf = ufSigla };

        var ufsIbge = new List<UfDataResponse>
        {
            new()
            {
                Id = ufId, Sigla = ufSigla, Nome = "São Paulo",
                Regiao = new RegiaoResponse { Id = 3, Sigla = "SE", Nome = "Sudeste" }
            }
        };
        _ibgeApiClientMock
            .Setup(c => c.FindUfsAsync())
            .ReturnsAsync(ufsIbge);

        var resultadoFalsoDoRepo = CriarResultadoFalsoDoRepositorio();
        _repositoryMock
            .Setup(r => r.GetPagedWithDetailsAsync(query.PageNumber, query.PageSize, ufId))
            .ReturnsAsync(resultadoFalsoDoRepo);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);

        _repositoryMock.Verify(r => r.GetPagedWithDetailsAsync(
            It.IsAny<int>(),
            It.IsAny<int>(),
            ufId), Times.Once);

        _repositoryMock.Verify(r => r.GetPagedWithDetailsAsync(
            It.IsAny<int>(),
            It.IsAny<int>(),
            null), Times.Never);
    }

    [Fact]
    public async Task Handle_QuandoUfEhInvalida_DeveIgnorarOFiltrarEChamarORepositorioSemFiltro()
    {
        const string ufInvalida = "XX";
        var query = new GetEstabelecimentosPaginadosQuery { PageNumber = 1, PageSize = 10, Uf = ufInvalida };

        _ibgeApiClientMock
            .Setup(c => c.FindUfsAsync())
            .ReturnsAsync(new List<UfDataResponse>());

        var resultadoFalsoDoRepo = CriarResultadoFalsoDoRepositorio();
        _repositoryMock
            .Setup(r => r.GetPagedWithDetailsAsync(query.PageNumber, query.PageSize, null))
            .ReturnsAsync(resultadoFalsoDoRepo);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        result.Items.First().CodCnes.Should().Be(resultadoFalsoDoRepo.Items.First().CodCnes);

        _ibgeApiClientMock.Verify(c => c.FindUfsAsync(), Times.Once);

        _repositoryMock.Verify(r => r.GetPagedWithDetailsAsync(
            It.IsAny<int>(),
            It.IsAny<int>(),
            null), Times.Once);
    }
}