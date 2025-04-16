using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Configuration
{
    [ExcludeFromCodeCoverage]
    public class DataApiOptions
    {
        public const string SectionName = "DataApi";

        [Required]
        public required string BaseAddress { get; init; }

        public string? Username { get; init; }

        public string? Password { get; init; }

        public string? BasicAuthCredential =>
            Username != null && Password != null
                ? Convert.ToBase64String(Encoding.UTF8.GetBytes($"{Username}:{Password}"))
                : null;
    }
}
