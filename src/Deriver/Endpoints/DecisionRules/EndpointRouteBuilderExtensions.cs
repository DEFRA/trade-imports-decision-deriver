using Defra.TradeImports.Api.Auth;
using Defra.TradeImportsDecisionDeriver.Deriver.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Endpoints.DecisionRules;

public static class EndpointRouteBuilderExtensions
{
    public static void MapDecisionRulesEndpoints(this IEndpointRouteBuilder app)
    {
        const string groupName = "DecisionRules";

        app.MapGet("admin/config/decision-rules", Get)
            .WithName("GetDecisionRulesOptions")
            .WithTags(groupName)
            .WithSummary("Get DecisionRulesOptions")
            .WithDescription("Get the decision rules configuration currently in use")
            .Produces<DecisionRulesOptions>()
            .ProducesProblem(StatusCodes.Status500InternalServerError);
    }

    /// <param name="options"></param>
    /// <returns></returns>
    [HttpGet]
    private static IResult Get([FromServices] IOptionsMonitor<DecisionRulesOptions> options)
    {
        return Results.Ok(options.CurrentValue);
    }
}
