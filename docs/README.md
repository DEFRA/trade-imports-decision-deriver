# Decision Rules Engine Documentation

## Overview

The Decision Rules Engine is a middleware-style pipeline system for processing import pre-notification decisions. It replaces the previous decorator pattern with a more flexible and maintainable architecture.

## Documentation Files

### 📘 [Decision Rules Engine](./DecisionRulesEngine.md)
**Comprehensive technical documentation**

Covers:
- Architecture and core components
- Rule types and patterns
- Common rules (validation, status handling, inspection decisions)
- CHED-specific rules (CVEDA, CVEDP, CHEDPP, CED)
- Decision code reference
- Extension guide
- Testing strategy

**Read this for**: Understanding the architecture, implementing new rules, or extending the system.

---

### 📊 [Visual Flow Diagrams](./DecisionRulesEngine-Diagrams.md)
**Visual representations of pipeline flows**

Includes:
- Pipeline execution flow
- Middleware pattern diagrams
- Complete pipeline flows for each CHED type
- Decision code categories
- Rule execution patterns
- Context flow diagrams

**Read this for**: Visual understanding of how rules execute and interact.

---

### ⚡ [Quick Reference Guide](./DecisionRulesEngine-QuickReference.md)
**Fast lookup reference**

Contains:
- Rule execution order by CHED type
- Decision codes table
- Internal error details
- Status handling matrix
- Consignment decisions by CHED type
- Not acceptable actions by CHED type
- CHEDPP check status mapping
- IUU logic (CVEDP)
- Troubleshooting guide
- File locations

**Read this for**: Quick lookups during development or debugging.

---

## Quick Start

### Understanding the Flow

1. **Request arrives** → Factory creates engine for CHED type
2. **Engine executes** → Rules run in sequence
3. **Each rule decides** → Return result or call next
4. **Result returned** → First non-null result or terminal decision

### Key Concepts

#### Middleware Pattern
Each rule receives a `next` delegate and decides whether to:
- **Short-circuit**: Return immediately without calling next
- **Continue**: Call next and return its result
- **Post-process**: Call next, inspect result, then return

#### Rule Types
- **Validation Rules**: Check preconditions, short-circuit on failure
- **Decision Rules**: Apply business logic, may be terminal
- **Post-Processing Rules**: Call next first, then log/validate

### Example Rule

```csharp
public class MyRule : IDecisionRule
{
    public DecisionResolutionResult Execute(
        DecisionResolutionContext context, 
        DecisionRuleDelegate next)
    {
        // Validation: short-circuit if fails
        if (context.Notification == null)
            return new DecisionResolutionResult(DecisionCode.X00);
        
        // Decision: return if match found
        if (context.Notification.Status == ImportNotificationStatus.Cancelled)
            return new DecisionResolutionResult(DecisionCode.X00);
        
        // Continue: no match, try next rule
        return next(context);
    }
}
```

---

## CHED Type Pipelines

### CVEDA (10 rules)
Validation → Status → Amend → Inspection → **CVEDA Decision** → Validation Logging

### CVEDP (11 rules)
Validation → Status → Amend → Inspection → **IUU Check** → **CVEDP Decision** → Validation Logging

### CHEDPP (7 rules)
Validation → Status → **CHEDPP Decision** (includes inspection logic) → Validation Logging

### CED (10 rules)
Validation → Status → Amend → Inspection → **CED Decision** → Validation Logging

---

## Common Tasks

### Adding a New Rule

1. Create class implementing `IDecisionRule`
2. Implement `Execute(context, next)` method
3. Add to appropriate pipeline(s) in factory
4. Write unit tests
5. Update documentation

### Modifying Rule Order

Edit the factory method for the CHED type:
```csharp
private static DecisionRulesEngine CreateEngineForCveda()
{
    var rules = new List<IDecisionRule>
    {
        new OrphanCheckCodeDecisionRule(),
        // ... reorder or add rules here
    };
    return new DecisionRulesEngine(rules);
}
```

### Debugging

1. Check rule execution order in factory
2. Verify previous rules call `next(context)`
3. Add logging in rules to trace execution
4. Use unit tests to isolate rule behavior
5. Check `InternalFurtherDetails` for error messages

---

## Decision Codes at a Glance

| Category | Codes | Meaning |
|----------|-------|---------|
| **Release** | C02 | Goods can proceed |
| **Hold** | H01-H02 | Goods held for inspection |
| **Rejection** | N01-N07 | Goods not acceptable |
| **Transit** | E03 | Goods for transit/export |
| **Error** | X00 | Processing error |

---

## Architecture Benefits

✅ **Flexible Control Flow** - Each rule controls execution  
✅ **Easy to Test** - Rules are independent and isolated  
✅ **Clear Separation** - Each rule has single responsibility  
✅ **Easy to Extend** - Add/remove/reorder rules easily  
✅ **Pre/Post Processing** - Rules can execute before or after next  
✅ **Short-Circuiting** - Early exit when decision is made  
✅ **Maintainable** - Clear pipeline structure per CHED type  

---

## File Structure

```
src/Deriver/Decisions/V2/DecisionEngine/
├── IDecisionRule.cs                          # Rule interface
├── DecisionRulesEngine.cs                    # Pipeline executor
├── DecisionRulesEngineFactory.cs             # Factory
└── DecisionRules/
    ├── OrphanCheckCodeDecisionRule.cs        # Common validation
    ├── UnlinkedNotificationDecisionRule.cs   # Common validation
    ├── WrongChedTypeDecisionRule.cs          # Common validation
    ├── MissingPartTwoDecisionRule.cs         # Common validation
    ├── TerminalStatusDecisionRule.cs         # Common status
    ├── AmendDecisionRule.cs                  # Common status
    ├── InspectionRequiredDecisionRule.cs     # Common inspection
    ├── CvedaDecisionRule.cs                  # CVEDA specific
    ├── CvedpIuuCheckRule.cs                  # CVEDP IUU
    ├── CvedpDecisionRule.cs                  # CVEDP specific
    ├── ChedppDecisionRule.cs                 # CHEDPP specific
    ├── CedDecisionRule.cs                    # CED specific
    ├── CommodityCodeValidationRule.cs        # Post-processing
    └── CommodityWeightOrQuantityValidationRule.cs  # Post-processing

docs/
├── README.md                                 # This file
├── DecisionRulesEngine.md                    # Detailed docs
├── DecisionRulesEngine-Diagrams.md           # Visual diagrams
└── DecisionRulesEngine-QuickReference.md     # Quick reference
```

---

## Testing

### Unit Tests
Test each rule in isolation with mocked `next` delegate.

### Integration Tests
Test complete pipelines with various notification scenarios.

### Test Coverage
- All notification statuses
- All consignment decisions
- All not acceptable actions
- Edge cases and error conditions
- Rule execution order

---

## Migration Notes

### From Previous Architecture

**Before**: Decorator pattern with nested resolvers
```csharp
new CommodityWeightOrQuantityResolver(
    new CommodityCodeResolver(
        new CvedaResolver()
    )
)
```

**After**: Middleware pipeline with explicit rule list
```csharp
new List<IDecisionRule>
{
    new OrphanCheckCodeDecisionRule(),
    new UnlinkedNotificationDecisionRule(),
    new WrongChedTypeDecisionRule(ImportNotificationType.Cveda),
    // ... more rules
    new CvedaDecisionRule(),
    new CommodityCodeValidationRule(),
    new CommodityWeightOrQuantityValidationRule()
}
```

**Benefits**:
- Clearer execution order
- Easier to add/remove rules
- Better testability
- More flexible control flow

---

## Support

For questions or issues:
1. Check the [Quick Reference](./DecisionRulesEngine-QuickReference.md) for common scenarios
2. Review the [Visual Diagrams](./DecisionRulesEngine-Diagrams.md) for flow understanding
3. Read the [Detailed Documentation](./DecisionRulesEngine.md) for architecture details
4. Check existing tests for examples

---

## Version History

### v2.0 - Middleware Pattern (Current)
- Introduced `IDecisionRule` with middleware pattern
- Replaced decorator pattern with explicit pipelines
- Added comprehensive documentation
- Improved testability and maintainability
- Renamed rules for clarity:
  - `OrphanCheckCodeDecisionRule` (check code validation)
  - `UnlinkedNotificationDecisionRule` (notification linking)
  - `WrongChedTypeDecisionRule` (CHED type validation)
  - `MissingPartTwoDecisionRule` (Part Two validation)
  - `TerminalStatusDecisionRule` (terminal status handling)
  - `AmendDecisionRule` (Amend status handling)
  - `InspectionRequiredDecisionRule` (inspection hold logic)

### v1.0 - Decorator Pattern (Legacy)
- Used decorator pattern for chaining resolvers
- Less flexible control flow
- Harder to test and maintain
