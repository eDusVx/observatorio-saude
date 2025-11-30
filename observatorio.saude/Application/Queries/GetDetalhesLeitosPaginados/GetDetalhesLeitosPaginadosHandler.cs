using MediatR;
using observatorio.saude.Application.Services.Clients;
using observatorio.saude.Domain.Dto;
using observatorio.saude.Domain.Interface;
using observatorio.saude.Domain.Utils;

namespace observatorio.saude.Application.Queries.GetDetalhesLeitosPaginados;

public class GetDetalhesLeitosPaginadosHandler(ILeitosRepository leitosRepository, IIbgeApiClient ibgeApiClient)
    : IRequestHandler<GetDetalhesLeitosPaginadosQuery, PaginatedResult<LeitosHospitalarDetalhadoDto>>
{
    private readonly IIbgeApiClient _ibgeApiClient = ibgeApiClient;
    private readonly ILeitosRepository _leitosRepository = leitosRepository;

    public async Task<PaginatedResult<LeitosHospitalarDetalhadoDto>> Handle(
        GetDetalhesLeitosPaginadosQuery request,
        CancellationToken cancellationToken)
    {
        long? codUf = null;
        if (!string.IsNullOrWhiteSpace(request.Uf))
        {
            var ufs = await _ibgeApiClient.FindUfsAsync();
            var ufEncontrada = ufs.FirstOrDefault(uf =>
                uf.Sigla.Equals(request.Uf, StringComparison.OrdinalIgnoreCase));

            if (ufEncontrada != null) codUf = ufEncontrada.Id;
        }

        var pagedResult = await _leitosRepository.GetDetailedPagedLeitosAsync(
            request.PageNumber,
            request.PageSize,
            request.Nome,
            request.CodCnes,
            request.Ano,
            request.Anomes,
            request.Tipo,
            codUf,
            cancellationToken);

        var ufsData = await _ibgeApiClient.FindUfsAsync();
        var ufMap = ufsData.ToDictionary(uf => uf.Id.ToString(), uf => uf.Sigla);

        foreach (var item in pagedResult.Items)
            if (item.Localizacao != null)
            {
                if (item.Localizacao.Uf != null && ufMap.TryGetValue(item.Localizacao.Uf, out var ufSigla))
                    item.Localizacao.Uf = ufSigla;
                else
                    item.Localizacao.Uf = "Não Informada";
            }

        return pagedResult;
    }
}