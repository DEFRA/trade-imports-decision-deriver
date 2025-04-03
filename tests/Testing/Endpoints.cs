namespace Defra.TradeImportsDecisionDeriver.Testing;

public static class Endpoints
{
    public static class Gmrs
    {
        private const string Root = "/gmrs";

        public static string Get(string gmrId) => $"{Root}/{gmrId}";
    }
}
