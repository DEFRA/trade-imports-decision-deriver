namespace Defra.TradeImportsDecisionDeriver.Testing;

public static class Endpoints
{
    public static class Decision
    {
        private const string Root = "/decision";

        public static string Get(string mrn) => $"{Root}/{mrn}/draft";

        public static string Post(string mrn) => $"{Root}/{mrn}";
    }

    public static class Admin
    {
        private const string Root = "/admin";

        public static class DeadLetterQueue
        {
            private const string SubRoot = $"{Root}/dlq";

            public static string Redrive() => $"{SubRoot}/redrive";

            public static string RemoveMessage(string? messageId = null) =>
                $"{SubRoot}/remove-message?messageId={messageId}";

            public static string Drain() => $"{SubRoot}/drain";
        }
    }
}
