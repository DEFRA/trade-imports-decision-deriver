namespace Defra.TradeImportsDecisionDeriver.Testing;

public static class Endpoints
{
    public static class Decision
    {
        private const string Root = "/decision";

        public static string Get(string mrn) => $"{Root}/{mrn}/draft";

        public static string Post(string mrn) => $"{Root}/{mrn}";
    }
}
