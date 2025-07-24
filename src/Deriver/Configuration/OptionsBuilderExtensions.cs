using Microsoft.Extensions.Options;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Configuration;

public static class OptionsBuilderExtensions
{
    public static OptionsBuilder<T> ValidateOptions<T>(this OptionsBuilder<T> builder, bool validateOnStart = true)
        where T : class
    {
        return validateOnStart
            ? builder.ValidateDataAnnotations().ValidateOnStart()
            : builder.ValidateDataAnnotations();
    }
}
