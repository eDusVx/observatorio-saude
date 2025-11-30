using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using observatorio.saude.Domain.Dto;
using observatorio.saude.Domain.Interface;
using observatorio.saude.Domain.Utils;
using observatorio.saude.Infra.Data;
using observatorio.saude.Infra.Models;

namespace observatorio.saude.Infra.Repositories;

public class EstabelecimentoRepository(ApplicationDbContext context) : IEstabelecimentoRepository
{
    private readonly ApplicationDbContext _context = context;

    public async Task<IEnumerable<NumeroEstabelecimentoEstadoDto>> GetContagemPorEstadoAsync(long? codUf = null)
    {
        var query = _context.EstabelecimentoModel
            .AsNoTracking();

        if (codUf.HasValue)
            query = query.Where(e => e.Localizacao != null && e.Localizacao.CodUf == codUf.Value);

        var contagemPorEstado = await query
            .Where(e => e.Localizacao != null && e.Localizacao.CodUf != null)
            .GroupBy(e => e.Localizacao!.CodUf)
            .Select(g => new NumeroEstabelecimentoEstadoDto
            {
                CodUf = g.Key!.Value,
                TotalEstabelecimentos = g.Count()
            })
            .OrderByDescending(r => r.TotalEstabelecimentos)
            .ToListAsync();

        return contagemPorEstado;
    }

    public async Task<NumeroEstabelecimentosDto> GetContagemTotalAsync()
    {
        var total = await _context.EstabelecimentoModel.CountAsync();

        return new NumeroEstabelecimentosDto { TotalEstabelecimentos = total };
    }

    public async Task<PaginatedResult<EstabelecimentoModel>> GetPagedWithDetailsAsync(int pageNumber, int pageSize,
        long? codUf = null)
    {
        var query = _context.EstabelecimentoModel
            .Include(e => e.CaracteristicaEstabelecimento)
            .Include(e => e.Localizacao)
            .Include(e => e.Organizacao)
            .Include(e => e.Turno)
            .Include(e => e.Servico)
            .AsQueryable();

        if (codUf.HasValue)
            query = query.Where(e => e.Localizacao != null && e.Localizacao.CodUf == codUf.Value);
        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(e => e.CodCnes)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PaginatedResult<EstabelecimentoModel>(items, pageNumber, pageSize, totalCount);
    }

    public async IAsyncEnumerable<ExportEstabelecimentoDto> StreamAllForExportAsync(
        List<long>? codUfs = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var baseQuery = _context.EstabelecimentoModel.AsNoTracking();

        if (codUfs != null && codUfs.Count > 0)
            baseQuery = baseQuery.Where(e =>
                e.Localizacao != null && e.Localizacao.CodUf.HasValue && codUfs.Contains(e.Localizacao.CodUf.Value));

        baseQuery = baseQuery.OrderBy(e => e.Localizacao != null ? e.Localizacao.CodUf : null);

        var finalQuery = baseQuery
            .Select(e => new ExportEstabelecimentoDto
            {
                CodCnes = e.CodCnes,
                RazaoSocial = e.CaracteristicaEstabelecimento != null
                    ? e.CaracteristicaEstabelecimento.NmRazaoSocial ?? string.Empty
                    : string.Empty,
                NomeFantasia = e.CaracteristicaEstabelecimento != null
                    ? e.CaracteristicaEstabelecimento.NmFantasia ?? string.Empty
                    : string.Empty,
                Endereco = e.Localizacao != null
                    ? $"{e.Localizacao.Endereco ?? string.Empty}, {e.Localizacao.Numero.ToString() ?? string.Empty}"
                    : string.Empty,
                Bairro = e.Localizacao != null ? e.Localizacao.Bairro ?? string.Empty : string.Empty,
                Cep = e.Localizacao != null ? e.Localizacao.CodCep.ToString() ?? string.Empty : string.Empty,
                CodUfParaMapeamento = e.Localizacao != null ? e.Localizacao.CodUf : null,
                EsferaAdministrativa = e.Organizacao != null
                    ? e.Organizacao.DscrEsferaAdministrativa ?? string.Empty
                    : string.Empty
            });

        await foreach (var item in finalQuery.AsAsyncEnumerable().WithCancellation(cancellationToken))
            yield return item;
    }

    public async Task<IEnumerable<GeoFeatureData>> GetWithCoordinatesAsync(
        long? codUf = null,
        double? minLat = null,
        double? maxLat = null,
        double? minLon = null,
        double? maxLon = null,
        int zoom = 10)
    {
        var query = _context.EstabelecimentoModel
            .AsNoTracking()
            .Where(e => e.Localizacao != null && e.Localizacao.Latitude != null && e.Localizacao.Longitude != null);

        if (codUf.HasValue)
            query = query.Where(e => e.Localizacao != null && e.Localizacao.CodUf == codUf.Value);

        if (minLat.HasValue && maxLat.HasValue && minLon.HasValue && maxLon.HasValue)
        {
            var minLatDecimal = (decimal)minLat.Value;
            var maxLatDecimal = (decimal)maxLat.Value;
            var minLonDecimal = (decimal)minLon.Value;
            var maxLonDecimal = (decimal)maxLon.Value;

            query = query.Where(e =>
                e.Localizacao!.Latitude >= minLatDecimal &&
                e.Localizacao.Latitude <= maxLatDecimal &&
                e.Localizacao.Longitude >= minLonDecimal &&
                e.Localizacao.Longitude <= maxLonDecimal);
        }

        const int lowZoomLimit = 2000;
        const int highZoomLimit = 15000;
        const int zoomThreshold = 11;
        var limit = zoom < zoomThreshold ? lowZoomLimit : highZoomLimit;

        return await query
            .Select(e => new GeoFeatureData
            {
                Latitude = e.Localizacao!.Latitude!.Value,
                Longitude = e.Localizacao!.Longitude!.Value,
                NomeFantasia = e.CaracteristicaEstabelecimento != null
                    ? e.CaracteristicaEstabelecimento.NmFantasia
                    : null,
                Endereco = e.Localizacao!.Endereco,
                Numero = e.Localizacao!.Numero,
                Bairro = e.Localizacao!.Bairro,
                Cep = e.Localizacao!.CodCep
            })
            .Take(limit)
            .ToListAsync();
    }
}