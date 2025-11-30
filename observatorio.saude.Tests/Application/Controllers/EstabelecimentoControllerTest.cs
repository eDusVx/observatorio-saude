using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using observatorio.saude.Application.Controllers;
using observatorio.saude.Application.Queries.ExportEstabelecimentos;
using observatorio.saude.Application.Queries.GetEstabelecimentosGeoJson;
using observatorio.saude.Application.Queries.GetEstabelecimentosPaginados;
using observatorio.saude.Application.Queries.GetNumeroEstabelecimentos;
using observatorio.saude.Application.Services;
using observatorio.saude.Domain.Dto;
using observatorio.saude.Domain.Entities;
using observatorio.saude.Domain.Utils;

namespace observatorio.saude.tests.Application.Controllers;

public class EstabelecimentoControllerTest
{
    private readonly EstabelecimentoController _controller;
    private readonly Mock<IFileExportService> _fileExportServiceMock;
    private readonly Mock<IMediator> _mediatorMock;

    public EstabelecimentoControllerTest()
    {
        _mediatorMock = new Mock<IMediator>();
        _fileExportServiceMock = new Mock<IFileExportService>();
        _controller = new EstabelecimentoController(_mediatorMock.Object, _fileExportServiceMock.Object);

        var httpContext = new DefaultHttpContext();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    [Fact]
    public async Task GetContagemPorEstado_QuandoChamado_DeveRetornarOkComListaDeContagem()
    {
        var resultadoEsperado = new List<NumeroEstabelecimentoEstadoDto>
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
            }
        };

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetNumerostabelecimentosPorEstadoQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultadoEsperado);

        var actionResult = await _controller.GetNumeroPorEstado();

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        okResult.Value.Should().BeEquivalentTo(resultadoEsperado);

        _mediatorMock.Verify(
            m => m.Send(It.IsAny<GetNumerostabelecimentosPorEstadoQuery>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetEstabelecimentos_QuandoChamado_DeveRetornarOkComResultadoPaginado()
    {
        var paginadosQuery = new GetEstabelecimentosPaginadosQuery { PageNumber = 1, PageSize = 10 };

        var resultadoPaginadoEsperado = new PaginatedResult<Estabelecimento>(
            new List<Estabelecimento> { new() { CodCnes = 12345 } }, 1, 1, 10
        );

        _mediatorMock
            .Setup(m => m.Send(paginadosQuery, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultadoPaginadoEsperado);

        var actionResult = await _controller.GetEstabelecimentos(paginadosQuery);

        actionResult
            .Should().BeOfType<OkObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status200OK);

        actionResult
            .Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().Be(resultadoPaginadoEsperado);

        _mediatorMock.Verify(m => m.Send(paginadosQuery, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetNumero_QuandoChamado_DeveRetornarOkComNumeroTotal()
    {
        var resultadoEsperado = new NumeroEstabelecimentosDto { TotalEstabelecimentos = 5000 };

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetNumeroEstabelecimentosQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultadoEsperado);

        var actionResult = await _controller.GetNumero();

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        okResult.Value.Should().BeEquivalentTo(resultadoEsperado);

        _mediatorMock.Verify(m => m.Send(It.IsAny<GetNumeroEstabelecimentosQuery>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExportResumoPorEstado_QuandoTemDados_DeveRetornarFileResult()
    {
        var query = new ExportEstabelecimentosQuery { Formato = "csv" };
        var fileBytes = new byte[] { 1, 2, 3 };
        var exportResult = new ExportFileResult
        {
            FileData = fileBytes,
            FileName = "Estabelecimentos.csv",
            ContentType = "text/csv"
        };

        _mediatorMock
            .Setup(m => m.Send(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(exportResult);

        var actionResult = await _controller.ExportResumoPorEstado(query);

        var fileResult = actionResult.Should().BeOfType<FileContentResult>().Subject;
        fileResult.ContentType.Should().Be(exportResult.ContentType);
        fileResult.FileDownloadName.Should().Be(exportResult.FileName);
        fileResult.FileContents.Should().BeEquivalentTo(fileBytes);

        _mediatorMock.Verify(m => m.Send(query, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExportResumoPorEstado_QuandoNaoTemDados_DeveRetornarNoContent()
    {
        var query = new ExportEstabelecimentosQuery { Formato = "csv" };
        var exportResult = new ExportFileResult
        {
            FileData = Array.Empty<byte>(),
            FileName = string.Empty,
            ContentType = string.Empty
        };

        _mediatorMock
            .Setup(m => m.Send(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(exportResult);

        var actionResult = await _controller.ExportResumoPorEstado(query);

        actionResult.Should().BeOfType<NoContentResult>().Subject.StatusCode.Should()
            .Be(StatusCodes.Status204NoContent);

        _mediatorMock.Verify(m => m.Send(query, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExportStream_QuandoFormatoXlsx_DeveGerarArquivoXlsxECallServiceCorreto()
    {
        var query = new ExportEstabelecimentosDetalhadosQuery { Format = "xlsx" };

        var asyncDataStream = GetAsyncEnumerable();

        _mediatorMock
            .Setup(m => m.Send(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(asyncDataStream);

        await _controller.ExportStream(query, CancellationToken.None);

        var response = _controller.HttpContext.Response;

        response.ContentType.Should().Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        response.Headers["Content-Disposition"].ToString().Should().Contain("attachment; filename=\"estabelecimentos_");
        response.Headers["Content-Disposition"].ToString().Should().Contain(".xlsx\"");

        _mediatorMock.Verify(m => m.Send(query, It.IsAny<CancellationToken>()), Times.Once);

        _fileExportServiceMock.Verify(
            m => m.GenerateXlsxStreamAsync(asyncDataStream, response.Body), Times.Once);
        _fileExportServiceMock.Verify(
            m => m.GenerateCsvStreamAsync(It.IsAny<IAsyncEnumerable<ExportEstabelecimentoDto>>(), It.IsAny<Stream>()),
            Times.Never);
    }

    [Theory]
    [InlineData("csv")]
    [InlineData("invalido")]
    public async Task ExportStream_QuandoFormatoCsvOuPadrao_DeveGerarArquivoCsvECallServiceCorreto(string format)
    {
        var query = new ExportEstabelecimentosDetalhadosQuery { Format = format };
        var mockStream = GetAsyncEnumerable();

        _mediatorMock
            .Setup(m => m.Send(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockStream);

        await _controller.ExportStream(query, CancellationToken.None);

        var response = _controller.HttpContext.Response;
        response.ContentType.Should().Be("text/csv");
        response.Headers["Content-Disposition"].ToString().Should().Contain("attachment; filename=\"estabelecimentos_");
        response.Headers["Content-Disposition"].ToString().Should().Contain(".csv\"");

        _mediatorMock.Verify(m => m.Send(query, It.IsAny<CancellationToken>()), Times.Once);
        _fileExportServiceMock.Verify(
            m => m.GenerateCsvStreamAsync(mockStream, response.Body), Times.Once);
        _fileExportServiceMock.Verify(
            m => m.GenerateXlsxStreamAsync(mockStream, response.Body), Times.Never);
    }

    [Fact]
    public async Task GetGeoJson_QuandoChamado_DeveRetornarOkComGeoJsonFeatureCollection()
    {
        var query = new GetEstabelecimentosGeoJsonQuery { Uf = "DF", Zoom = 1 };
        var resultadoEsperado = new GeoJsonFeatureCollection(new List<GeoJsonFeature>());

        _mediatorMock
            .Setup(m => m.Send(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultadoEsperado);

        var actionResult = await _controller.GetGeoJson(query);

        var okResult = actionResult.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        okResult.Value.Should().BeEquivalentTo(resultadoEsperado);

        _mediatorMock.Verify(m => m.Send(query, It.IsAny<CancellationToken>()), Times.Once);
    }

    private static async IAsyncEnumerable<ExportEstabelecimentoDto> GetAsyncEnumerable()
    {
        yield return new ExportEstabelecimentoDto { CodCnes = 12345, NomeFantasia = "Hospital Teste A" };
        yield return new ExportEstabelecimentoDto { CodCnes = 67890, NomeFantasia = "Clínica Teste B" };
        await Task.CompletedTask;
    }
}