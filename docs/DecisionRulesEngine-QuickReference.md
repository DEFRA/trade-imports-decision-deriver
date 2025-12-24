# Decision Rules Engine - Quick Reference Guide

## Rule Execution Order

### CVEDA Pipeline (10 rules)
| # | Rule | Type | Returns |
|---|------|------|---------|
| 1 | `OrphanCheckCodeDecisionRule` | Validation | X00 if invalid, else continue |
| 2 | `UnlinkedNotificationDecisionRule` | Validation | X00 if not linked, else continue |
| 3 | `WrongChedTypeDecisionRule` | Validation | X00 if wrong type, else continue |
| 4 | `MissingPartTwoDecisionRule` | Validation | X00 if missing, else continue |
| 5 | `TerminalStatusDecisionRule` | Decision | X00 for terminal statuses, else continue |
| 6 | `AmendDecisionRule` | Decision | Previous decision or X00 for Amend, else continue |
| 7 | `InspectionRequiredDecisionRule` | Decision | H01 for Submitted, H02 for InProgress, else continue |
| 8 | `CvedaDecisionRule` | Decision | C02, N01-N07, or X00 (terminal) |
| 9 | `CommodityCodeValidationRule` | Post-process | Logs warnings, returns result from next |
| 10 | `CommodityWeightOrQuantityValidationRule` | Post-process | Logs warnings, returns result from next |

### CVEDP Pipeline (11 rules)
| # | Rule | Type | Returns |
|---|------|------|---------|
| 1-7 | Same as CVEDA | | |
| 8 | `CvedpIuuCheckRule` | Decision | E03 if IUU Option B, else continue |
| 9 | `CvedpDecisionRule` | Decision | C02, N01-N07, or X00 (terminal) |
| 10 | `CommodityCodeValidationRule` | Post-process | Logs warnings, returns result from next |
| 11 | `CommodityWeightOrQuantityValidationRule` | Post-process | Logs warnings, returns result from next |

### CHEDPP Pipeline (7 rules)
| # | Rule | Type | Returns |
|---|------|------|---------|
| 1 | `OrphanCheckCodeDecisionRule` | Validation | X00 if invalid, else continue |
| 2 | `UnlinkedNotificationDecisionRule` | Validation | X00 if not linked, else continue |
| 3 | `WrongChedTypeDecisionRule` | Validation | X00 if wrong type, else continue |
| 4 | `TerminalStatusDecisionRule` | Decision | X00 for terminal statuses, else continue |
| 5 | `ChedppDecisionRule` | Decision | H01, H02, C02, or X00 (terminal) |
| 6 | `CommodityCodeValidationRule` | Post-process | Logs warnings, returns result from next |
| 7 | `CommodityWeightOrQuantityValidationRule` | Post-process | Logs warnings, returns result from next |

### CED Pipeline (10 rules)
| # | Rule | Type | Returns |
|---|------|------|---------|
| 1-7 | Same as CVEDA | | |
| 8 | `CedDecisionRule` | Decision | C02, N01-N07, or X00 (terminal) |
| 9 | `CommodityCodeValidationRule` | Post-process | Logs warnings, returns result from next |
| 10 | `CommodityWeightOrQuantityValidationRule` | Post-process | Logs warnings, returns result from next |

## Decision Codes

| Code | Category | Meaning | Used By |
|------|----------|---------|---------|
| **C02** | Release | Goods can proceed | All CHED types |
| **H01** | Hold | Hold for Documentary Check | All CHED types |
| **H02** | Hold | Hold for Identity and Physical Check | All CHED types |
| **N01** | Not Acceptable | Required for Import | CVEDA, CVEDP, CED |
| **N02** | Not Acceptable | Prohibited for Import | CVEDA, CVEDP, CED |
| **N03** | Not Acceptable | Horse Passport Required | CVEDA, CVEDP, CED |
| **N04** | Not Acceptable | Not Acceptable for Transhipment | CVEDA, CVEDP, CED |
| **N05** | Not Acceptable | Not Acceptable for Transit | CVEDA, CVEDP, CED |
| **N06** | Not Acceptable | Not Acceptable for Temporary Admission | CVEDA, CVEDP, CED |
| **N07** | Not Acceptable | Not Acceptable for Import EU Approval Required | CVEDA, CVEDP, CED |
| **E03** | Transit/Export | Goods for Transit/Export (IUU violation) | CVEDP only |
| **X00** | Error | Processing Error (internal error or invalid state) | All CHED types |

## Internal Further Details (Error Messages)

| Scenario | InternalFurtherDetails |
|----------|------------------------|
| Check code missing/invalid | "Orphan check code" |
| Notification not linked | "Notification not linked" |
| Wrong CHED type | "Wrong CHED type" |
| Part Two missing | "Part Two missing" |
| Cancelled status | "Notification cancelled" |
| Replaced status | "Notification replaced" |
| Deleted status | "Notification deleted" |
| Split consignment | "Split consignment" |
| Amend with no previous decision | "Amend status with no previous decision" |
| Unknown consignment decision | "Unknown consignment decision" |
| IUU Option B (CVEDP) | "IUU Option B detected" |

## Common Rule Behaviors

### Validation Rules
**Pattern**: Check condition → Return X00 if fails → Continue if passes

```csharp
if (validationFails)
    return new DecisionResolutionResult(DecisionCode.X00, "Error message");
return next(context);
```

### Decision Rules
**Pattern**: Check condition → Return decision if matches → Continue if no match

```csharp
if (conditionMet)
    return new DecisionResolutionResult(DecisionCode.H01);
return next(context);
```

### Post-Processing Rules
**Pattern**: Call next → Log/validate → Return result

```csharp
var result = next(context);
LogWarnings(context);
return result;
```

## Status Handling

| Status | TerminalStatusDecisionRule | AmendDecisionRule | InspectionRequiredDecisionRule |
|--------|---------------------------|-------------------|-------------------------------|
| **Cancelled** | X00 (terminal) | - | - |
| **Replaced** | X00 (terminal) | - | - |
| **Deleted** | X00 (terminal) | - | - |
| **SplitConsignment** | X00 (terminal) | - | - |
| **Amend** | Continue | Previous decision or X00 | - |
| **Submitted** | Continue | Continue | H01 |
| **InProgress** | Continue | Continue | H02 |
| **Other** | Continue | Continue | Continue |

## Consignment Decisions by CHED Type

### CVEDA, CVEDP, CED
| Consignment Decision | Not Acceptable Action | Decision Code |
|---------------------|----------------------|---------------|
| Not Acceptable | RequiredForImport | N01 |
| Not Acceptable | ProhibitedForImport | N02 |
| Not Acceptable | HorsePassportRequired | N03 |
| Not Acceptable | NotAcceptableForTranshipment | N04 |
| Not Acceptable | NotAcceptableForTransit | N05 |
| Not Acceptable | NotAcceptableForTemporaryAdmission | N06 |
| Not Acceptable | NotAcceptableForImportEuApprovalRequired | N07 |
| Acceptable | - | C02 |
| Unknown/Missing | - | X00 |

### CHEDPP
| HMI Check | PHSI Check | Status | Decision Code |
|-----------|------------|--------|---------------|
| Required | - | - | H01 |
| Inconclusive | - | - | H01 |
| - | Required | - | H02 |
| - | Inconclusive | - | H02 |
| NotRequired | NotRequired | Submitted | H01 |
| NotRequired | NotRequired | InProgress | H02 |
| NotRequired | NotRequired | Amend | Previous or X00 |
| NotRequired | NotRequired | Other | C02 |

## Not Acceptable Actions by CHED Type

### CVEDA, CVEDP, CED
| Not Acceptable Action | Decision Code |
|----------------------|---------------|
| RequiredForImport | N01 |
| ProhibitedForImport | N02 |
| HorsePassportRequired | N03 |
| NotAcceptableForTranshipment | N04 |
| NotAcceptableForTransit | N05 |
| NotAcceptableForTemporaryAdmission | N06 |
| NotAcceptableForImportEuApprovalRequired | N07 |

## CHEDPP Check Status Mapping

| Check Status | Decision | Notes |
|--------------|----------|-------|
| **Required** | H01 (HMI) or H02 (PHSI) | Inspection required |
| **Inconclusive** | H01 (HMI) or H02 (PHSI) | Treated same as Required |
| **NotRequired** | Continue to status check | No inspection needed |
| **null** | Continue to status check | No check data available |

## IUU Logic (CVEDP Only)

| IUU Option | Decision Code | Meaning |
|------------|---------------|---------|
| **IuuOptionB** | E03 | Goods for Transit/Export (IUU violation detected) |
| **Other** | Continue | No IUU issue, continue to decision rule |

## Inspection Required Logic

| Status | Decision Code | Meaning |
|--------|---------------|---------|
| **Submitted** | H01 | Hold for Documentary Check |
| **InProgress** | H02 | Hold for Identity and Physical Check |
| **Other** | Continue | No inspection hold, continue to next rule |

## Adding New CHED Types

### Step 1: Create CHED-Specific Rule
```csharp
public sealed class MyNewChedDecisionRule : IDecisionRule
{
    public DecisionResolutionResult Execute(
        DecisionResolutionContext context, 
        DecisionRuleDelegate next)
    {
        // Implement CHED-specific logic
        if (someCondition)
            return new DecisionResolutionResult(DecisionCode.C02);
        
        return new DecisionResolutionResult(DecisionCode.X00);
    }
}
```

### Step 2: Add Factory Method
```csharp
private static DecisionRulesEngine CreateEngineForMyNewChed()
{
    var rules = new List<IDecisionRule>
    {
        new OrphanCheckCodeDecisionRule(),
        new UnlinkedNotificationDecisionRule(),
        new WrongChedTypeDecisionRule(ImportNotificationType.MyNewChed),
        new MissingPartTwoDecisionRule(),
        new TerminalStatusDecisionRule(),
        new AmendDecisionRule(),
        new InspectionRequiredDecisionRule(),
        new MyNewChedDecisionRule(),
        new CommodityCodeValidationRule(),
        new CommodityWeightOrQuantityValidationRule()
    };
    return new DecisionRulesEngine(rules);
}
```

### Step 3: Update Factory Switch
```csharp
public DecisionRulesEngine Get(string? notificationType)
{
    return notificationType switch
    {
        ImportNotificationType.Cveda => CreateEngineForCveda(),
        ImportNotificationType.Cvedp => CreateEngineForCvedp(),
        ImportNotificationType.Chedpp => CreateEngineForChedpp(),
        ImportNotificationType.Ced => CreateEngineForCed(),
        ImportNotificationType.MyNewChed => CreateEngineForMyNewChed(),
        _ => throw new ArgumentOutOfRangeException(...)
    };
}
```

## Testing Checklist

### Unit Tests (Per Rule)
- [ ] Test short-circuit behavior (validation rules)
- [ ] Test continue behavior (when condition not met)
- [ ] Test decision return (when condition met)
- [ ] Test post-processing (logging/validation)
- [ ] Test with null/missing data
- [ ] Verify `next` is called (or not called) as expected

### Integration Tests (Per Pipeline)
- [ ] Test all notification statuses
- [ ] Test all consignment decisions
- [ ] Test all not acceptable actions
- [ ] Test HMI/PHSI checks (CHEDPP)
- [ ] Test IUU logic (CVEDP)
- [ ] Test Amend status with/without previous decision
- [ ] Test terminal statuses
- [ ] Test commodity validation warnings

### Edge Cases
- [ ] Null notification
- [ ] Missing Part Two
- [ ] Invalid check code
- [ ] Unlinked notification
- [ ] Wrong CHED type
- [ ] Unknown consignment decision
- [ ] Missing commodity data

## Troubleshooting

### Issue: Rule not executing
**Check**:
1. Is rule added to pipeline in factory?
2. Is rule in correct order?
3. Is previous rule calling `next(context)`?

### Issue: Wrong decision returned
**Check**:
1. Is rule logic correct?
2. Is rule in correct position in pipeline?
3. Are previous rules short-circuiting unexpectedly?

### Issue: Validation warnings not logged
**Check**:
1. Are post-processing rules at end of pipeline?
2. Are post-processing rules calling `next(context)` first?
3. Is logger configured correctly?

### Issue: X00 returned unexpectedly
**Check**:
1. Check InternalFurtherDetails for error message
2. Verify validation rules are passing
3. Check for null/missing data in context
4. Verify consignment decision is valid

## File Locations

```
src/Deriver/Decisions/V2/DecisionEngine/
├── IDecisionRule.cs
├── DecisionRulesEngine.cs
├── DecisionRulesEngineFactory.cs
├── DecisionResolutionContext.cs
├── DecisionResolutionResult.cs
└── DecisionRules/
    ├── OrphanCheckCodeDecisionRule.cs
    ├── UnlinkedNotificationDecisionRule.cs
    ├── WrongChedTypeDecisionRule.cs
    ├── MissingPartTwoDecisionRule.cs
    ├── TerminalStatusDecisionRule.cs
    ├── AmendDecisionRule.cs
    ├── InspectionRequiredDecisionRule.cs
    ├── CvedaDecisionRule.cs
    ├── CvedpIuuCheckRule.cs
    ├── CvedpDecisionRule.cs
    ├── ChedppDecisionRule.cs
    ├── CedDecisionRule.cs
    ├── CommodityCodeValidationRule.cs
    └── CommodityWeightOrQuantityValidationRule.cs

docs/
├── README.md
├── DecisionRulesEngine.md
├── DecisionRulesEngine-Diagrams.md
└── DecisionRulesEngine-QuickReference.md
```

## Quick Command Reference

### Run Tests
```bash
dotnet test
```

### Build Project
```bash
dotnet build
```

### Check for Compilation Errors
```bash
dotnet build --no-incremental
```

### Run Specific Test
```bash
dotnet test --filter "FullyQualifiedName~RuleName"
```
