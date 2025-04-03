using Defra.TradeImportsDecisionDeriver.Api.Domain;

namespace Defra.TradeImportsDecisionDeriver.Api.Services;

public class GmrService : IGmrService
{
    public Task<Gmr?> GetGmr(string gmrId) => Task.FromResult<Gmr?>(null);
}
