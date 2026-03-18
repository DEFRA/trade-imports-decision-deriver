using System.Runtime.CompilerServices;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Extensions;

public static class DecisionImportPreNotificationExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool StatusIsSubmittedOrInProgress(this DecisionImportPreNotification decisionImportPreNotification)
    {
        return decisionImportPreNotification.Status switch
        {
            ImportNotificationStatus.Submitted 
            or ImportNotificationStatus.InProgress
            => true,
            _ => false,
        };
    }
}
