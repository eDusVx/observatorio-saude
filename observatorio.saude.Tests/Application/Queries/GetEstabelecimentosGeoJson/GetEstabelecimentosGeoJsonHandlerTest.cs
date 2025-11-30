using FluentAssertions;
using Moq;
using observatorio.saude.Application.Queries.GetEstabelecimentosGeoJson;
using observatorio.saude.Application.Services.Clients;
using observatorio.saude.Domain.Dto;
using observatorio.saude.Domain.Interface;
using observatorio.saude.Infra.Services.Response.Ibge;

namespace observatorio.saude.tests.Application.Queries.GetEstabelecimentosGeoJson;

public class GetEstabelecimentosGeoJsonQueryHandlerTest
{
    private readonly Mock<IEstabelecimentoRepository> _estabelecimentoRepositoryMock;
    private readonly GetEstabelecimentosGeoJsonQueryHandler _handler;
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
        }
    };

    public GetEstabelecimentosGeoJsonQueryHandlerTest()
    {
        _estabelecimentoRepositoryMock = new Mock<IEstabelecimentoRepository>();
        _ibgeApiClientMock = new Mock<IIbgeApiClient>();

        _ibgeApiClientMock.Setup(c => c.FindUfsAsync()).ReturnsAsync(_mockUfs);

        _handler = new GetEstabelecimentosGeoJsonQueryHandler(
            _estabelecimentoRepositoryMock.Object,
            _ibgeApiClientMock.Object);
    }

    private List<GeoFeatureData> GetMockEstabelecimentosData()
    {
        return new List<GeoFeatureData>
        {
            new()
            {
                NomeFantasia = "Hospital Teste A",
                Endereco = "Rua Principal",
                Numero = 100,
                Bairro = "Centro",
                Cep = 01000 - 000,
                Latitude = -23.55M,
                Longitude = -46.63M
            },
            new()
            {
                NomeFantasia = null,
                Endereco = "Av. Secundária",
                Numero = 0,
                Bairro = "Bairro B",
                Cep = 20000 - 000,
                Latitude = -22.90M,
                Longitude = -43.17M
            }
        };
    }

    [Fact]
    public async Task Handle_QuandoSemFiltroUF_DeveChamarRepositorioComCodUfNulo()
    {
        var query = new GetEstabelecimentosGeoJsonQuery
        {
            Uf = null,
            MinLatitude = -90, MaxLatitude = 90, MinLongitude = -180, MaxLongitude = 180, Zoom = 10
        };
        var mockData = GetMockEstabelecimentosData();

        _estabelecimentoRepositoryMock
            .Setup<Task<IEnumerable<GeoFeatureData>>>(r => r.GetWithCoordinatesAsync(
                null,
                (double)query.MinLatitude,
                (double)query.MaxLatitude,
                (double)query.MinLongitude,
                (double)query.MaxLongitude,
                query.Zoom))
            .ReturnsAsync(mockData);

        await _handler.Handle(query, CancellationToken.None);

        _estabelecimentoRepositoryMock.Verify(r => r.GetWithCoordinatesAsync(
            null,
            (double)query.MinLatitude, (double)query.MaxLatitude,
            (double)query.MinLongitude, (double)query.MaxLongitude,
            query.Zoom), Times.Once);

        _ibgeApiClientMock.Verify(c => c.FindUfsAsync(), Times.Never);
    }

    [Fact]
    public async Task Handle_QuandoComFiltroUFValido_DeveChamarRepositorioComCodUfCorreto()
    {
        var query = new GetEstabelecimentosGeoJsonQuery
        {
            Uf = "RJ", Zoom = 10,
            MinLatitude = -90, MaxLatitude = 90, MinLongitude = -180, MaxLongitude = 180
        };
        var mockData = GetMockEstabelecimentosData();
        long codUfEsperado = 33;

        _estabelecimentoRepositoryMock
            .Setup<Task<IEnumerable<GeoFeatureData>>>(r => r.GetWithCoordinatesAsync(
                codUfEsperado,
                It.IsAny<double>(), It.IsAny<double>(),
                It.IsAny<double>(), It.IsAny<double>(),
                It.IsAny<int>()))
            .ReturnsAsync(mockData);

        await _handler.Handle(query, CancellationToken.None);

        _ibgeApiClientMock.Verify(c => c.FindUfsAsync(), Times.Once);

        _estabelecimentoRepositoryMock.Verify(r => r.GetWithCoordinatesAsync(
            codUfEsperado,
            (double)query.MinLatitude, (double)query.MaxLatitude,
            (double)query.MinLongitude, (double)query.MaxLongitude,
            query.Zoom), Times.Once);
    }

    [Fact]
    public async Task Handle_QuandoComFiltroUFInvalido_DeveChamarRepositorioComCodUfNulo()
    {
        var query = new GetEstabelecimentosGeoJsonQuery
        {
            Uf = "XX", Zoom = 10,
            MinLatitude = -90, MaxLatitude = 90, MinLongitude = -180, MaxLongitude = 180
        };
        var mockData = GetMockEstabelecimentosData();

        _estabelecimentoRepositoryMock
            .Setup<Task<IEnumerable<GeoFeatureData>>>(r => r.GetWithCoordinatesAsync(
                null,
                It.IsAny<double>(), It.IsAny<double>(),
                It.IsAny<double>(), It.IsAny<double>(),
                It.IsAny<int>()))
            .ReturnsAsync(mockData);

        await _handler.Handle(query, CancellationToken.None);

        _ibgeApiClientMock.Verify(c => c.FindUfsAsync(), Times.Once);

        _estabelecimentoRepositoryMock.Verify(r => r.GetWithCoordinatesAsync(
            null,
            (double)query.MinLatitude, (double)query.MaxLatitude,
            (double)query.MinLongitude, (double)query.MaxLongitude,
            query.Zoom), Times.Once);
    }

    [Fact]
    public async Task Handle_DeveMapearDadosCorretamenteParaGeoJsonFeatureCollection()
    {
        var query = new GetEstabelecimentosGeoJsonQuery
        {
            Uf = null,
            MinLatitude = -90, MaxLatitude = 90, MinLongitude = -180, MaxLongitude = 180, Zoom = 10
        };
        var mockData = GetMockEstabelecimentosData();

        _estabelecimentoRepositoryMock
            .Setup<Task<IEnumerable<GeoFeatureData>>>(r => r.GetWithCoordinatesAsync(
                null,
                It.IsAny<double>(), It.IsAny<double>(),
                It.IsAny<double>(), It.IsAny<double>(),
                It.IsAny<int>()))
            .ReturnsAsync(mockData);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.Features.Should().HaveCount(2);

        var feature1 = result.Features[0];
        feature1.Geometry.Coordinates.Should().BeEquivalentTo(new[] { -46.63D, -23.55D });
        feature1.Properties["nome"].Should().Be("Hospital Teste A");
        feature1.Properties["endereco"].Should().Be("Rua Principal, 100");

        var feature2 = result.Features[1];
        feature2.Geometry.Coordinates.Should().BeEquivalentTo(new[] { -43.17D, -22.90D });
        feature2.Properties["nome"].Should().Be("Nome não informado");
        feature2.Properties["endereco"].Should().Be("Av. Secundária, 0");
    }
}