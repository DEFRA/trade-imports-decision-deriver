using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Configuration;

[ExcludeFromCodeCoverage]
public class DataApiOptions
{
    public const string SectionName = "DataApi";

    [Required]
    public required string BaseAddress { get; init; }

    public string? Username { get; init; }

    public string? Password { get; init; }

    private string? BasicAuthCredential =>
        Username != null && Password != null
            ? Convert.ToBase64String(Encoding.UTF8.GetBytes($"{Username}:{Password}"))
            : null;

    public void Configure(HttpClient httpClient)
    {
        httpClient.BaseAddress = new Uri(BaseAddress);

        if (BasicAuthCredential != null)
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Basic",
                BasicAuthCredential
            );

        if (httpClient.BaseAddress.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
            httpClient.DefaultRequestVersion = HttpVersion.Version20;
    }
}
