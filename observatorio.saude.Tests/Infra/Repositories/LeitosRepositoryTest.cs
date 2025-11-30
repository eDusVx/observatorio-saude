using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using observatorio.saude.Infra.Data;
using observatorio.saude.Infra.Models;
using observatorio.saude.Infra.Repositories;

namespace observatorio.saude.tests.Infra.Repositories;

public class LeitosRepositoryTest : IDisposable
{
    private const int AnoBase = 2023;
    private const long CodCnes1 = 1001;
    private const long CodCnes2 = 2002;
    private const int CodUfSp = 35;
    private const int CodUfRj = 33;
    private const string CodUnidade1 = "501";
    private const string CodUnidade2 = "502";
    private readonly ApplicationDbContext _context;
    private readonly LeitosRepository _repository;

    public LeitosRepositoryTest()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _repository = new LeitosRepository(_context);

        SeedDatabase();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    private void SeedDatabase()
    {
        _context.Database.EnsureDeleted();
        _context.Database.EnsureCreated();

        _context.EstabelecimentoModel.AddRange(
            new EstabelecimentoModel
            {
                Localizacao = new LocalizacaoModel
                {
                    CodUnidade = CodUnidade1, CodUf = CodUfSp, Endereco = "Rua Teste 1", Numero = 100, Bairro = "Centro"
                },
                CodCnes = CodCnes1, CodUnidade = CodUnidade1
            },
            new EstabelecimentoModel
            {
                Localizacao = new LocalizacaoModel
                {
                    CodUnidade = CodUnidade2, CodUf = CodUfRj, Endereco = "Av Teste 2", Numero = 50, Bairro = "Praia"
                },
                CodCnes = CodCnes2, CodUnidade = CodUnidade2
            });

        _context.LeitosModel.AddRange(
            new LeitoModel
            {
                CodCnes = CodCnes1, Anomes = AnoBase * 100 + 4, QtdLeitosExistentes = 100, QtdLeitosSus = 50,
                QtdUtiTotalExist = 10, NmEstabelecimento = "HOSPITAL A"
            },
            new LeitoModel
            {
                CodCnes = CodCnes1, Anomes = AnoBase * 100 + 5, QtdLeitosExistentes = 120, QtdLeitosSus = 60,
                QtdUtiTotalExist = 15, NmEstabelecimento = "HOSPITAL A"
            },
            new LeitoModel
            {
                CodCnes = CodCnes2, Anomes = AnoBase * 100 + 2, QtdLeitosExistentes = 80, QtdLeitosSus = 20,
                QtdUtiTotalExist = 5, NmEstabelecimento = "CLINICA B"
            },
            new LeitoModel
            {
                CodCnes = CodCnes2, Anomes = AnoBase * 100 + 3, QtdLeitosExistentes = 90, QtdLeitosSus = 25,
                QtdUtiTotalExist = 8, NmEstabelecimento = "CLINICA B"
            },
            new LeitoModel
            {
                CodCnes = CodCnes1, Anomes = (AnoBase - 1) * 100 + 12, QtdLeitosExistentes = 50, QtdLeitosSus = 10,
                QtdUtiTotalExist = 2, NmEstabelecimento = "HOSPITAL A ANTIGO"
            });

        _context.SaveChanges();
    }

    [Fact]
    public async Task GetLeitosAgregadosAsync_DeveRetornarSomaDosLeitosMaisRecentesNoAno()
    {
        var result = await _repository.GetLeitosAgregadosAsync(AnoBase);

        result.Should().NotBeNull();
        result!.TotalLeitos.Should().Be(210);
        result.LeitosSus.Should().Be(85);
        result.TotalUti.Should().Be(23);
    }

    [Fact]
    public async Task GetLeitosAgregadosAsync_QuandoNaoHaDados_DeveRetornarNulo()
    {
        var result = await _repository.GetLeitosAgregadosAsync(9999);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetIndicadoresPorEstadoAsync_DeveRetornarDadosAgregadosPorEstadoSemFiltro()
    {
        var result = await _repository.GetIndicadoresPorEstadoAsync(AnoBase);

        result.Should().HaveCount(2);

        var sp = result.First(i => i.CodUf == CodUfSp);
        sp.TotalLeitos.Should().Be(120);
        sp.LeitosSus.Should().Be(60);
        sp.Criticos.Should().Be(15);

        var rj = result.First(i => i.CodUf == CodUfRj);
        rj.TotalLeitos.Should().Be(90);
        rj.LeitosSus.Should().Be(25);
        rj.Criticos.Should().Be(8);
    }

    [Fact]
    public async Task GetIndicadoresPorEstadoAsync_DeveFiltrarPorListaDeUfsCorretamente()
    {
        var codUfsFiltro = new List<long> { CodUfRj };

        var result = await _repository.GetIndicadoresPorEstadoAsync(AnoBase, null, codUfsFiltro);

        result.Should().ContainSingle();
        result.First().CodUf.Should().Be(CodUfRj);
        result.First().TotalLeitos.Should().Be(90);
    }

    [Fact]
    public async Task GetPagedLeitosAsync_DeveFiltrarPorNomeCorretamente()
    {
        var result = await _repository.GetPagedLeitosAsync(
            1, 10, "clINICA", null, AnoBase, null, null, null, CancellationToken.None);

        result.TotalCount.Should().Be(1);
        result.Items.First().NomeEstabelecimento.Should().Be("CLINICA B");
    }

    [Fact]
    public async Task GetPagedLeitosAsync_DeveFiltrarPorCodUfCorretamente()
    {
        var result = await _repository.GetPagedLeitosAsync(
            1, 10, null, null, AnoBase, null, null, CodUfRj, CancellationToken.None);

        result.TotalCount.Should().Be(1);
        result.Items.First().NomeEstabelecimento.Should().Be("CLINICA B");
        result.Items.First().LocalizacaoUf.Should().Be(CodUfRj.ToString());
    }
}