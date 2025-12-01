using System.Diagnostics.CodeAnalysis;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Authentication;

[ExcludeFromCodeCoverage]
public static class Scopes
{
    public const string Read = "read";
    public const string Write = "write";
    public const string Execute = "execute";
}
