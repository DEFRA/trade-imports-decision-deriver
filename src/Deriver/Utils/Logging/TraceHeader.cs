using System.ComponentModel.DataAnnotations;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Utils.Logging;

public class TraceHeader
{
    [ConfigurationKeyName("TraceHeader")]
    [Required]
    public required string Name { get; set; }
}
