using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Protected;
using observatorio.saude.Infra.Services.Clients.Ibge;
using observatorio.saude.Infra.Services.Response.Ibge;

namespace observatorio.saude.Tests.Infra.Services.Clients.Ibge;

public class IbgeApiClientTest
{
    private const int _maxTentativasCliente = 3;
    private readonly IbgeApiClient _client;
    private readonly Mock<IConfiguration> _configMock;
    private readonly int _currentYear = DateTime.Now.Year;
    private readonly Mock<HttpMessageHandler> _handlerMock;
    private readonly HttpClient _httpClient;

    public IbgeApiClientTest()
    {
        _configMock = new Mock<IConfiguration>();
        _handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        _httpClient = new HttpClient(_handlerMock.Object);

        var sectionMock = new Mock<IConfigurationSection>();

        sectionMock.Setup(s => s.Value).Returns("http://api.ibge.gov.br/populacao/{ano}");
        _configMock.Setup(c => c.GetSection("Ibge:FindPopulacaoUf")).Returns(sectionMock.Object);

        var sectionMock2 = new Mock<IConfigurationSection>();
        sectionMock2.Setup(s => s.Value).Returns("http://api.ibge.gov.br/ufs");
        _configMock.Setup(c => c.GetSection("Ibge:FindUf")).Returns(sectionMock2.Object);

        _client = new IbgeApiClient(_configMock.Object, _httpClient);
    }

    private HttpResponseMessage CreateHttpResponse(HttpStatusCode statusCode, object content)
    {
        var jsonContent = JsonSerializer.Serialize(content);
        return new HttpResponseMessage
        {
            StatusCode = statusCode,
            Content = new StringContent(jsonContent)
        };
    }

    private void SetupMockedResponse(HttpStatusCode statusCode, object content)
    {
        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(() => CreateHttpResponse(statusCode, content))
            .Verifiable();
    }

    [Fact]
    public async Task FindPopulacaoUfAsync_QuandoSucessoNaPrimeiraTentativa_DeveRetornarDadosCorretos()
    {
        var mockResponse = new List<IbgeUfResponse>
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
                                SerieData = new Dictionary<string, string> { { "2024", "10000" } }
                            }
                        }
                    }
                }
            }
        };
        SetupMockedResponse(HttpStatusCode.OK, mockResponse);

        var result = await _client.FindPopulacaoUfAsync(_currentYear);

        result.Should().NotBeNull();

        result.Dados.Should().NotBeNull().And.HaveCount(1);
        result.AnoEncontrado.Should().Be(_currentYear);

        _handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.RequestUri!.ToString() == $"http://api.ibge.gov.br/populacao/{_currentYear}"
            ),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    [Fact]
    public async Task FindPopulacaoUfAsync_QuandoRespostaVazia_DeveTentarFallbackERetornarVazio()
    {
        var mockResponse = new List<IbgeUfResponse>();
        SetupMockedResponse(HttpStatusCode.OK, mockResponse);

        var result = await _client.FindPopulacaoUfAsync(_currentYear);

        result.Should().NotBeNull();

        result.Dados.Should().NotBeNull().And.BeEmpty();
        result.AnoEncontrado.Should().BeNull();

        _handlerMock.Protected().Verify(
            "SendAsync",
            Times.Exactly(_maxTentativasCliente),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    [Fact]
    public async Task FindPopulacaoUfAsync_QuandoJsonNulo_DeveTentarFallbackERetornarVazio()
    {
        var jsonContent = "null";

        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(() => new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(jsonContent)
            })
            .Verifiable();

        var result = await _client.FindPopulacaoUfAsync(_currentYear);

        result.Should().NotBeNull();
        result.Dados.Should().NotBeNull().And.BeEmpty();
        result.AnoEncontrado.Should().BeNull();

        _handlerMock.Protected().Verify(
            "SendAsync",
            Times.Exactly(_maxTentativasCliente),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    [Fact]
    public async Task FindPopulacaoUfAsync_QuandoErroServidor500_DeveLancarExcecao()
    {
        SetupMockedResponse(HttpStatusCode.InternalServerError, "{\"message\":\"Server Error\"}");

        Func<Task> act = async () => await _client.FindPopulacaoUfAsync(_currentYear);

        await act.Should().ThrowAsync<HttpRequestException>();

        _handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>()
        );
    }
}