using Defra.TradeImportsDecisionDeriver.Api.Domain;

namespace Defra.TradeImportsDecisionDeriver.Api.Services;

public interface IGmrService
{
    Task<Gmr?> GetGmr(string gmrId);
}
