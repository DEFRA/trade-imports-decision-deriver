using System.Diagnostics.CodeAnalysis;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Authentication;

[ExcludeFromCodeCoverage]
public static class PolicyNames
{
    public const string Read = nameof(Scopes.Read);
    public const string Write = nameof(Scopes.Write);
    public const string Execute = nameof(Scopes.Execute);
}
