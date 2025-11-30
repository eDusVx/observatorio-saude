using System.Runtime.CompilerServices;
using FluentAssertions;
using Moq;
using observatorio.saude.Application.Queries.ExportEstabelecimentos;
using observatorio.saude.Application.Services.Clients;
using observatorio.saude.Domain.Dto;
using observatorio.saude.Domain.Interface;
using observatorio.saude.Infra.Services.Response.Ibge;

namespace observatorio.saude.tests.Application.Queries.ExportEstabelecimentos;

public class StreamEstabelecimentosDetalhadosHandlerTest
{
    private readonly Mock<IEstabelecimentoRepository> _estabelecimentoRepositoryMock;
    private readonly StreamEstabelecimentosDetalhadosHandler _handler;
    private readonly Mock<IIbgeApiClient> _ibgeApiClientMock;

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
            Id = 11, Sigla = "RO", Nome = "Rondônia",
            Regiao = new RegiaoResponse { Id = 2, Sigla = "N", Nome = "Norte" }
        }
    };

    public StreamEstabelecimentosDetalhadosHandlerTest()
    {
        _estabelecimentoRepositoryMock = new Mock<IEstabelecimentoRepository>();
        _ibgeApiClientMock = new Mock<IIbgeApiClient>();

        _ibgeApiClientMock.Setup(c => c.FindUfsAsync()).ReturnsAsync(_mockUfs);

        _handler = new StreamEstabelecimentosDetalhadosHandler(_estabelecimentoRepositoryMock.Object,
            _ibgeApiClientMock.Object);
    }

    private static async IAsyncEnumerable<ExportEstabelecimentoDto> GetMockInputDataStream(
        [EnumeratorCancellation] CancellationToken cancellationToken = default,
        params (long? CodUf, string Uf)[] data)
    {
        foreach (var (codUf, uf) in data)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return new ExportEstabelecimentoDto { CodUfParaMapeamento = codUf, Uf = uf };
            await Task.Delay(1, cancellationToken);
        }
    }

    private static async Task<List<ExportEstabelecimentoDto>> ConsumeStreamAsync(
        IAsyncEnumerable<ExportEstabelecimentoDto> stream, CancellationToken cancellationToken = default)
    {
        var list = new List<ExportEstabelecimentoDto>();
        await foreach (var item in stream.WithCancellation(cancellationToken)) list.Add(item);
        return list;
    }

    [Fact]
    public async Task Handle_QuandoSemFiltroUF_DeveChamarRepositorioComFiltroNulo()
    {
        var query = new ExportEstabelecimentosDetalhadosQuery { Uf = null };
        var mockStream = GetMockInputDataStream(CancellationToken.None, (33, null!));

        _estabelecimentoRepositoryMock
            .Setup(r => r.StreamAllForExportAsync(It.IsAny<List<long>>(), CancellationToken.None))
            .Returns(mockStream);

        var resultStream = await _handler.Handle(query, CancellationToken.None);
        var result = await ConsumeStreamAsync(resultStream);

        _estabelecimentoRepositoryMock.Verify(r => r.StreamAllForExportAsync(null, CancellationToken.None), Times.Once);
        _estabelecimentoRepositoryMock.Verify(
            r => r.StreamAllForExportAsync(It.IsAny<List<long>>(), CancellationToken.None), Times.Once);

        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_QuandoComFiltroUFInvalido_DeveChamarRepositorioComFiltroNulo()
    {
        var query = new ExportEstabelecimentosDetalhadosQuery { Uf = ["XX, YY"] };
        var mockStream = GetMockInputDataStream(CancellationToken.None, (33, null!));

        _estabelecimentoRepositoryMock
            .Setup(r => r.StreamAllForExportAsync(null, CancellationToken.None))
            .Returns(mockStream);

        var resultStream = await _handler.Handle(query, CancellationToken.None);
        await ConsumeStreamAsync(resultStream);

        _estabelecimentoRepositoryMock.Verify(r => r.StreamAllForExportAsync(null, CancellationToken.None), Times.Once);
        _estabelecimentoRepositoryMock.Verify(
            r => r.StreamAllForExportAsync(It.IsAny<List<long>>(), CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task Handle_DeveMapearCodUfParaSiglaUfCorretamente()
    {
        var query = new ExportEstabelecimentosDetalhadosQuery { Uf = ["SP, RO"] };
        var mockStream = GetMockInputDataStream(CancellationToken.None, (35, "Lixo"), (99, "Lixo"), (null, "Lixo"));

        _estabelecimentoRepositoryMock
            .Setup(r => r.StreamAllForExportAsync(It.IsAny<List<long>>(), CancellationToken.None))
            .Returns(mockStream);

        var resultStream = await _handler.Handle(query, CancellationToken.None);
        var result = await ConsumeStreamAsync(resultStream);

        result.Should().HaveCount(3);
        result[0].Uf.Should().Be("SP");
        result[1].Uf.Should().Be("");
        result[2].Uf.Should().Be("Lixo");

        result[0].CodUfParaMapeamento.Should().Be(35);
    }
}