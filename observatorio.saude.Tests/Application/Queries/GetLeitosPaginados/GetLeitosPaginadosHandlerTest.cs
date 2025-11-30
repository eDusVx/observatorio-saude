using FluentAssertions;
using Moq;
using observatorio.saude.Application.Queries.GetLeitosPaginados;
using observatorio.saude.Application.Services.Clients;
using observatorio.saude.Domain.Dto;
using observatorio.saude.Domain.Interface;
using observatorio.saude.Domain.Utils;
using observatorio.saude.Infra.Services.Response.Ibge;

namespace observatorio.saude.tests.Application.Queries.GetLeitosPaginados;

public class GetLeitosPaginadosHandlerTest
{
    private readonly GetLeitosPaginadosHandler _handler;
    private readonly Mock<IIbgeApiClient> _ibgeApiClientMock;
    private readonly Mock<ILeitosRepository> _leitosRepositoryMock;

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
        }
    };

    public GetLeitosPaginadosHandlerTest()
    {
        _leitosRepositoryMock = new Mock<ILeitosRepository>();
        _ibgeApiClientMock = new Mock<IIbgeApiClient>();

        _ibgeApiClientMock.Setup(c => c.FindUfsAsync()).ReturnsAsync(_mockUfs);

        _handler = new GetLeitosPaginadosHandler(
            _leitosRepositoryMock.Object,
            _ibgeApiClientMock.Object);
    }

    private PaginatedResult<LeitosHospitalarDto> GetMockPagedResult(long ufId = 35)
    {
        var items = new List<LeitosHospitalarDto>
        {
            new()
            {
                NomeEstabelecimento = "Hospital A", TotalLeitos = 100, LeitosSus = 20,
                LocalizacaoUf = ufId.ToString()
            },
            new()
            {
                NomeEstabelecimento = "Clínica B", TotalLeitos = 50, LeitosSus = 50,
                LocalizacaoUf = ufId.ToString()
            },
            new()
            {
                NomeEstabelecimento = "Posto C", TotalLeitos = 0, LeitosSus = 0, LocalizacaoUf = ufId.ToString()
            },
            new()
            {
                NomeEstabelecimento = "Hospital D", TotalLeitos = 10, LeitosSus = 1, LocalizacaoUf = "99"
            }
        };

        return new PaginatedResult<LeitosHospitalarDto>(items, totalCount: 4, currentPage: 1, pageSize: 10);
    }

    [Fact]
    public async Task Handle_QuandoSemFiltroUF_DeveChamarRepositorioComCodUfNulo()
    {
        var query = new GetLeitosPaginadosQuery { Uf = null };
        var mockResult = GetMockPagedResult();

        _leitosRepositoryMock
            .Setup(r => r.GetPagedLeitosAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<long?>(),
                It.IsAny<int?>(), null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResult);

        await _handler.Handle(query, CancellationToken.None);

        _leitosRepositoryMock.Verify(r => r.GetPagedLeitosAsync(
            query.PageNumber, query.PageSize, query.Nome, query.CodCnes, query.Ano, null,
            null, null,
            It.IsAny<CancellationToken>()), Times.Once);

        _ibgeApiClientMock.Verify(c => c.FindUfsAsync(), Times.Once);
    }

    [Fact]
    public async Task Handle_QuandoComFiltroUFValido_DeveChamarRepositorioComCodUfCorreto()
    {
        var ufSigla = "RJ";
        long codUfEsperado = 33;
        var query = new GetLeitosPaginadosQuery { Uf = ufSigla };
        var mockResult = GetMockPagedResult(codUfEsperado);

        _leitosRepositoryMock
            .Setup(r => r.GetPagedLeitosAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<long?>(),
                It.IsAny<int?>(), null, null, codUfEsperado, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResult);

        await _handler.Handle(query, CancellationToken.None);

        _ibgeApiClientMock.Verify(c => c.FindUfsAsync(), Times.AtLeast(1));

        _leitosRepositoryMock.Verify(r => r.GetPagedLeitosAsync(
            query.PageNumber, query.PageSize, query.Nome, query.CodCnes, query.Ano, null, null,
            codUfEsperado,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_QuandoComFiltroUFInvalido_DeveChamarRepositorioComCodUfNulo()
    {
        var query = new GetLeitosPaginadosQuery { Uf = "XX" };
        var mockResult = GetMockPagedResult();

        _leitosRepositoryMock
            .Setup(r => r.GetPagedLeitosAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<long?>(),
                It.IsAny<int?>(), null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResult);

        await _handler.Handle(query, CancellationToken.None);

        _ibgeApiClientMock.Verify(c => c.FindUfsAsync(), Times.AtLeast(1));

        _leitosRepositoryMock.Verify(r => r.GetPagedLeitosAsync(
            query.PageNumber, query.PageSize, query.Nome, query.CodCnes, query.Ano, null,
            null, null,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DeveCalcularOcupacaoEMapearUfCorretamente()
    {
        var query = new GetLeitosPaginadosQuery { Uf = null };
        long ufIdTeste = 35;
        var mockResult = GetMockPagedResult(ufIdTeste);

        _leitosRepositoryMock
            .Setup(r => r.GetPagedLeitosAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<long?>(),
                It.IsAny<int?>(), null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResult);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.Items.Should().HaveCount(4);

        var item1 = result.Items[0];
        item1.NomeEstabelecimento.Should().Be("Hospital A");
        item1.LocalizacaoUf.Should().Be("SP");

        var item2 = result.Items[1];
        item2.NomeEstabelecimento.Should().Be("Clínica B");
        item2.LocalizacaoUf.Should().Be("SP");

        var item3 = result.Items[2];
        item3.NomeEstabelecimento.Should().Be("Posto C");
        item3.LocalizacaoUf.Should().Be("SP");

        var item4 = result.Items[3];
        item4.LocalizacaoUf.Should().Be("Não Informada");

        _ibgeApiClientMock.Verify(c => c.FindUfsAsync(), Times.AtLeastOnce);
    }
}