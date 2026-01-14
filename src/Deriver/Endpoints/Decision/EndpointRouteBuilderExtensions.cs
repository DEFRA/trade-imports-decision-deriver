using Defra.TradeImportsDataApi.Api.Client;
using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDecisionDeriver.Deriver.Authentication;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.Comparers;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.Processors;
using Defra.TradeImportsDecisionDeriver.Deriver.Matching;
using Defra.TradeImportsDecisionDeriver.Deriver.Utils.CorrelationId;
using Microsoft.AspNetCore.Mvc;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Endpoints.Decision;

public static class EndpointRouteBuilderExtensions
{
    public static void MapDecisionEndpoints(this IEndpointRouteBuilder app)
    {
        const string groupName = "Decisions";

        app.MapGet("decision/{mrn}/draft", Get)
            .WithName("GetDraftDecisionByMrn")
            .WithTags(groupName)
            .WithSummary("Get Draft ClearanceDecision")
            .WithDescription("Get a draft ClearanceDecision by MRN")
            .Produces<DecisionResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .RequireAuthorization(PolicyNames.Read);

        app.MapPost("decision/{mrn}", Post)
            .WithName("PostDecisionDecisionByMrn")
            .WithTags(groupName)
            .WithSummary("Post ClearanceDecision")
            .WithDescription("Gets and Persist a ClearanceDecision by MRN")
            .Produces<DecisionResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .RequireAuthorization(PolicyNames.Write);
    }

    /// <param name="mrn"></param>
    /// <param name="decisionService"></param>
    /// <param name="apiClient"></param>
    /// <param name="correlationIdGenerator"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpGet]
    private static Task<IResult> Get(
        [FromRoute] string mrn,
        [FromServices] IDecisionService decisionService,
        [FromServices] ITradeImportsDataApiClient apiClient,
        [FromServices] ICorrelationIdGenerator correlationIdGenerator,
        CancellationToken cancellationToken
    )
    {
        return ProcessMrn(
            mrn,
            decisionService,
            apiClient,
            correlationIdGenerator,
            PersistOption.DoNotPersist,
            cancellationToken
        );
    }

    /// <param name="mrn"></param>
    /// <param name="request"></param>
    /// <param name="decisionService"></param>
    /// <param name="apiClient"></param>
    /// <param name="correlationIdGenerator"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPost]
    private static Task<IResult> Post(
        [FromRoute] string mrn,
        [FromBody] DecisionRequest request,
        [FromServices] IDecisionService decisionService,
        [FromServices] ITradeImportsDataApiClient apiClient,
        [FromServices] ICorrelationIdGenerator correlationIdGenerator,
        CancellationToken cancellationToken
    )
    {
        return ProcessMrn(
            mrn,
            decisionService,
            apiClient,
            correlationIdGenerator,
            request.PersistOption,
            cancellationToken
        );
    }

    [HttpPost]
    private static async Task<IResult> ProcessMrn(
        string mrn,
        IDecisionService decisionService,
        ITradeImportsDataApiClient apiClient,
        ICorrelationIdGenerator correlationIdGenerator,
        PersistOption persist,
        CancellationToken cancellationToken
    )
    {
        var clearanceRequest = await apiClient.GetCustomsDeclaration(mrn, cancellationToken);

        if (clearanceRequest is null)
        {
            return Results.NotFound();
        }

        var notificationResponse = await apiClient.GetImportPreNotificationsByMrn(mrn, cancellationToken);

        var preNotifications = notificationResponse
            .ImportPreNotifications.Select(x => x.ImportPreNotification)
            .ToList();

        ////var decisionContext = new DecisionContext(
        ////    preNotifications.Select(x => x.ToDecisionImportPreNotification()).ToList(),
        ////    [new ClearanceRequestWrapper(mrn, clearanceRequest!.ClearanceRequest!)]
        ////);

        var decisionContext = new DecisionContext(
            preNotifications.Select(x => x.ToDecisionImportPreNotification()).ToList(),
            [
                new CustomsDeclarationWrapper(
                    mrn,
                    new CustomsDeclaration()
                    {
                        ClearanceDecision = clearanceRequest?.ClearanceDecision,
                        ClearanceRequest = clearanceRequest?.ClearanceRequest,
                    }
                ),
            ]
        );

        var decisionResult = decisionService.Process(decisionContext).FirstOrDefault();

        if (string.IsNullOrEmpty(decisionResult.Mrn))
        {
            return Results.NoContent();
        }

        var customsDeclaration = new CustomsDeclaration
        {
            ClearanceDecision = clearanceRequest?.ClearanceDecision,
            Finalisation = clearanceRequest?.Finalisation,
            ClearanceRequest = clearanceRequest?.ClearanceRequest,
            ExternalErrors = clearanceRequest?.ExternalErrors,
        };

        var isDifferent = !decisionResult.Decision.IsSameAs(customsDeclaration.ClearanceDecision);

        customsDeclaration.ClearanceDecision = decisionResult.Decision;

        var shouldPersist =
            persist == PersistOption.AlwaysPersist || (persist == PersistOption.PersistIfSame && !isDifferent);

        if (shouldPersist)
        {
            await apiClient.PutCustomsDeclaration(
                decisionResult.Mrn,
                customsDeclaration,
                clearanceRequest?.ETag,
                cancellationToken
            );
        }

        return Results.Ok(new DecisionResponse(isDifferent, shouldPersist, customsDeclaration.ClearanceDecision));
    }
}
