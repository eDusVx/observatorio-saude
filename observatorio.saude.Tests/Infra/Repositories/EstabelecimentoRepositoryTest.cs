using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using observatorio.saude.Domain.Dto;
using observatorio.saude.Infra.Data;
using observatorio.saude.Infra.Models;
using observatorio.saude.Infra.Repositories;

namespace observatorio.saude.tests.Infra.Repositories;

public class EstabelecimentoRepositoryTest : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly EstabelecimentoRepository _repository;

    public EstabelecimentoRepositoryTest()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _repository = new EstabelecimentoRepository(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    private async Task SeedDatabaseAsync(int count)
    {
        var turnos = new List<TurnoModel>
        {
            new() { CodTurnoAtendimento = 1, DscrTurnoAtendimento = "MANHA" },
            new() { CodTurnoAtendimento = 2, DscrTurnoAtendimento = "TARDE" },
            new() { CodTurnoAtendimento = 3, DscrTurnoAtendimento = "MANHA E TARDE" }
        };
        await _context.TurnoModel.AddRangeAsync(turnos);

        var estabelecimentos = new List<EstabelecimentoModel>();
        for (var i = 1; i <= count; i++)
        {
            var codCnes = 100L + i;
            var codUnidade = $"UNIDADE_TESTE_{i}";
            long codTurno = i % 3 + 1;

            var estabelecimento = new EstabelecimentoModel
            {
                CodCnes = codCnes,
                DataExtracao = new DateTime(2025, 9, 8),
                CodUnidade = codUnidade,
                CodTurnoAtendimento = codTurno,
                CaracteristicaEstabelecimento = new CaracteristicaEstabelecimentoModel
                {
                    CodUnidade = codUnidade,
                    NmFantasia = $"Fantasia {i}",
                    NmRazaoSocial = $"Razao {i}"
                },
                Localizacao = new LocalizacaoModel
                {
                    CodUnidade = codUnidade,
                    Bairro = "Bairro Teste",
                    Endereco = "Rua X",
                    Numero = 10,
                    CodCep = 12345678,
                    CodUf = i % 2 == 0 ? 35 : 33
                },
                Organizacao = new OrganizacaoModel
                {
                    CodCnes = codCnes,
                    TpGestao = 'M',
                    DscrEsferaAdministrativa = "MUNICIPAL"
                },
                Servico = new ServicoModel
                {
                    CodCnes = codCnes,
                    StCentroCirurgico = true
                }
            };
            estabelecimentos.Add(estabelecimento);
        }

        await _context.EstabelecimentoModel.AddRangeAsync(estabelecimentos);
        await _context.SaveChangesAsync();
    }

    private async Task SeedGeoDatabaseAsync()
    {
        var estabelecimentos = new List<EstabelecimentoModel>
        {
            new()
            {
                CodCnes = 1, CodUnidade = "U1",
                Localizacao = new LocalizacaoModel
                {
                    CodUnidade = "U1", CodUf = 35, Latitude = -23.5M, Longitude = -46.6M, Endereco = "Rua A",
                    Numero = 1, Bairro = "B1"
                },
                CaracteristicaEstabelecimento = new CaracteristicaEstabelecimentoModel
                    { CodUnidade = "U1", NmFantasia = "Fantasia 1" }
            },
            new()
            {
                CodCnes = 2, CodUnidade = "U2",
                Localizacao = new LocalizacaoModel
                {
                    CodUnidade = "U2", CodUf = 35, Latitude = -24.5M, Longitude = -47.6M, Endereco = "Rua B",
                    Numero = 2, Bairro = "B2"
                },
                CaracteristicaEstabelecimento = new CaracteristicaEstabelecimentoModel
                    { CodUnidade = "U2", NmFantasia = "Fantasia 2" }
            },
            new()
            {
                CodCnes = 3, CodUnidade = "U3",
                Localizacao = new LocalizacaoModel
                {
                    CodUnidade = "U3", CodUf = 33, Latitude = -23.6M, Longitude = -46.5M, Endereco = "Rua C",
                    Numero = 3, Bairro = "B3"
                },
                CaracteristicaEstabelecimento = new CaracteristicaEstabelecimentoModel
                    { CodUnidade = "U3", NmFantasia = "Fantasia 3" }
            },
            new()
            {
                CodCnes = 4, CodUnidade = "U4",
                Localizacao = new LocalizacaoModel
                {
                    CodUnidade = "U4", CodUf = 33, Latitude = null, Longitude = null, Endereco = "Rua D", Numero = 4,
                    Bairro = "B4"
                },
                CaracteristicaEstabelecimento = new CaracteristicaEstabelecimentoModel
                    { CodUnidade = "U4", NmFantasia = "Fantasia 4" }
            }
        };

        await _context.EstabelecimentoModel.AddRangeAsync(estabelecimentos);
        await _context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetContagemPorEstadoAsync_QuandoDadosExistem_DeveAgruparEContarCorretamente()
    {
        var estabelecimentos = new List<EstabelecimentoModel>
        {
            new()
            {
                CodCnes = 1, CodUnidade = "U1", Localizacao = new LocalizacaoModel { CodUnidade = "U1", CodUf = 35 }
            },
            new()
            {
                CodCnes = 2, CodUnidade = "U2", Localizacao = new LocalizacaoModel { CodUnidade = "U2", CodUf = 35 }
            },
            new()
            {
                CodCnes = 3, CodUnidade = "U3", Localizacao = new LocalizacaoModel { CodUnidade = "U3", CodUf = 35 }
            },
            new()
            {
                CodCnes = 4, CodUnidade = "U4", Localizacao = new LocalizacaoModel { CodUnidade = "U4", CodUf = 33 }
            },
            new()
            {
                CodCnes = 5, CodUnidade = "U5", Localizacao = new LocalizacaoModel { CodUnidade = "U5", CodUf = 33 }
            },
            new()
            {
                CodCnes = 6, CodUnidade = "U6", Localizacao = new LocalizacaoModel { CodUnidade = "U6", CodUf = null }
            },
            new()
            {
                CodCnes = 7, CodUnidade = "U7", Localizacao = new LocalizacaoModel { CodUnidade = "U7", CodUf = null }
            }
        };
        await _context.EstabelecimentoModel.AddRangeAsync(estabelecimentos);
        await _context.SaveChangesAsync();

        var result = await _repository.GetContagemPorEstadoAsync();

        result.Should().NotBeNull();
        result.Should().HaveCount(2);

        var resultadoLista = result.ToList();

        resultadoLista[0].CodUf.Should().Be(35);
        resultadoLista[0].TotalEstabelecimentos.Should().Be(3);

        resultadoLista[1].CodUf.Should().Be(33);
        resultadoLista[1].TotalEstabelecimentos.Should().Be(2);
    }

    [Fact]
    public async Task GetContagemTotalAsync_DeveRetornarContagemCorretaDeEstabelecimentos()
    {
        const int total = 7;
        await SeedDatabaseAsync(total);

        var result = await _repository.GetContagemTotalAsync();

        result.Should().NotBeNull();
        result.TotalEstabelecimentos.Should().Be(total);
    }

    [Fact]
    public async Task GetPagedWithDetailsAsync_QuandoDadosExistem_DeveRetornarResultadoPaginadoCorretamente()
    {
        await SeedDatabaseAsync(12);
        const int pageNumber = 2;
        const int pageSize = 5;

        var result = await _repository.GetPagedWithDetailsAsync(pageNumber, pageSize);

        result.Should().NotBeNull();
        result.Items.Should().HaveCount(pageSize);
        result.CurrentPage.Should().Be(pageNumber);
        result.PageSize.Should().Be(pageSize);
        result.TotalCount.Should().Be(12);
        result.TotalPages.Should().Be(3);
        result.Items.First().CodCnes.Should().Be(106);
        result.Items.Last().CodCnes.Should().Be(110);
        result.Items.First().CaracteristicaEstabelecimento.Should().NotBeNull();
        result.Items.First().Localizacao.Should().NotBeNull();
        result.Items.First().Organizacao.Should().NotBeNull();
        result.Items.First().Turno.Should().NotBeNull();
        result.Items.First().Servico.Should().NotBeNull();
    }

    [Fact]
    public async Task GetPagedWithDetailsAsync_QuandoRequisitandoUltimaPagina_DeveRetornarItensRestantes()
    {
        await SeedDatabaseAsync(12);
        const int pageNumber = 3;
        const int pageSize = 5;

        var result = await _repository.GetPagedWithDetailsAsync(pageNumber, pageSize);

        result.Should().NotBeNull();
        result.TotalCount.Should().Be(12);
        result.TotalPages.Should().Be(3);
        result.Items.Should().HaveCount(2);
        result.Items.First().CodCnes.Should().Be(111);
        result.Items.Last().CodCnes.Should().Be(112);
    }

    [Fact]
    public async Task GetPagedWithDetailsAsync_QuandoNaoHaDados_DeveRetornarResultadoVazio()
    {
        var result = await _repository.GetPagedWithDetailsAsync(1, 10);

        result.Should().NotBeNull();
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task StreamAllForExportAsync_SemFiltro_DeveRetornarTodosOsItensMapeados()
    {
        await SeedDatabaseAsync(3);

        var resultList = new List<ExportEstabelecimentoDto>();
        await foreach (var item in _repository.StreamAllForExportAsync()) resultList.Add(item);

        resultList.Should().HaveCount(3);
        resultList.First().CodCnes.Should().Be(101);
        resultList.First().NomeFantasia.Should().Be("Fantasia 1");
        resultList.First().Endereco.Should().Be("Rua X, 10");
        resultList.First().CodUfParaMapeamento.Should().Be(33);
    }

    [Fact]
    public async Task GetWithCoordinatesAsync_DeveAplicarFiltroDeCoordenadasCorretamente()
    {
        await SeedGeoDatabaseAsync();
        double minLat = -24.0, maxLat = -23.0;
        double minLon = -47.0, maxLon = -46.0;

        var result = await _repository.GetWithCoordinatesAsync(
            null, minLat, maxLat, minLon, maxLon);

        result.Should().HaveCount(2);
        result.Should().Contain(e => e.NomeFantasia == "Fantasia 1");
        result.Should().Contain(e => e.NomeFantasia == "Fantasia 3");
        result.Should().NotContain(e => e.NomeFantasia == "Fantasia 2");
    }

    [Fact]
    public async Task GetWithCoordinatesAsync_DeveAplicarFiltroDeUfAposFiltroGeo()
    {
        await SeedGeoDatabaseAsync();
        double minLat = -24.0, maxLat = -23.0;
        double minLon = -47.0, maxLon = -46.0;

        var result = await _repository.GetWithCoordinatesAsync(
            35, minLat, maxLat, minLon, maxLon);

        result.Should().ContainSingle();
        result.First().NomeFantasia.Should().Be("Fantasia 1");
    }

    [Fact]
    public async Task GetWithCoordinatesAsync_DeveExcluirRegistrosSemCoordenadas()
    {
        await SeedGeoDatabaseAsync();

        var result = await _repository.GetWithCoordinatesAsync();

        result.Should().HaveCount(3);
        result.Should().NotContain(e => e.NomeFantasia == "Fantasia 4");
    }
}