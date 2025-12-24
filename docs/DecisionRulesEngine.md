# Decision Rules Engine Documentation

## Overview

The Decision Rules Engine is a middleware-style pipeline system for processing import pre-notification decisions. Each notification type (CVEDA, CVEDP, CHEDPP, CED) has its own pipeline of decision rules that execute in sequence.

## Architecture

### Core Components

#### 1. `IDecisionRule` Interface
```csharp
public delegate DecisionResolutionResult DecisionRuleDelegate(DecisionResolutionContext context);

public interface IDecisionRule
{
    DecisionResolutionResult Execute(DecisionResolutionContext context, DecisionRuleDelegate next);
}
```

Each rule receives:
- `context`: Contains the notification and related data
- `next`: A delegate to call the next rule in the pipeline

Each rule can:
- **Short-circuit**: Return a result immediately without calling `next`
- **Continue**: Call `next(context)` to pass control to the next rule
- **Post-process**: Call `next(context)`, inspect the result, then return

#### 2. `DecisionRulesEngine` Class
Executes the pipeline by building a middleware chain from the list of rules.

```csharp
public sealed class DecisionRulesEngine
{
    public DecisionResolutionResult Resolve(DecisionResolutionContext context)
    {
        // Builds middleware chain and executes
    }
}
```

#### 3. `DecisionRulesEngineFactory` Class
Creates the appropriate engine with the correct rule pipeline for each CHED type.

```csharp
public sealed class DecisionRulesEngineFactory : IDecisionRulesEngineFactory
{
    public DecisionRulesEngine Get(string? notificationType)
    {
        return notificationType switch
        {
            ImportNotificationType.Cveda => CreateEngineForCveda(),
            ImportNotificationType.Cvedp => CreateEngineForCvedp(),
            ImportNotificationType.Chedpp => CreateEngineForChedpp(),
            ImportNotificationType.Ced => CreateEngineForCed(),
            _ => throw new ArgumentOutOfRangeException(...)
        };
    }
}
```

#### 4. `DecisionResolutionContext` Class
Contains all data needed for decision resolution:
- `Notification`: The import pre-notification
- `MatchingResult`: Matching data from external systems
- `CommodityRiskResult`: Risk assessment data

## Rule Types

### 1. Validation Rules
Check preconditions and return error decisions if validation fails.

**Pattern**: Short-circuit on failure
```csharp
public DecisionResolutionResult Execute(DecisionResolutionContext context, DecisionRuleDelegate next)
{
    if (validationFails)
        return new DecisionResolutionResult(DecisionCode.X00, "Error message");
    
    return next(context);
}
```

### 2. Decision Rules
Apply business logic and return decisions based on notification data.

**Pattern**: Return decision or continue
```csharp
public DecisionResolutionResult Execute(DecisionResolutionContext context, DecisionRuleDelegate next)
{
    if (conditionMet)
        return new DecisionResolutionResult(DecisionCode.C02);
    
    return next(context);
}
```

### 3. Post-Processing Rules
Execute after other rules to log warnings or validate results.

**Pattern**: Call next first, then process
```csharp
public DecisionResolutionResult Execute(DecisionResolutionContext context, DecisionRuleDelegate next)
{
    var result = next(context);
    
    // Log warnings or validate
    LogWarnings(context);
    
    return result;
}
```

## Common Rules

These rules are shared across multiple CHED types:

### 1. `OrphanCheckCodeDecisionRule`
**Purpose**: Validates that the notification has a valid check code  
**Type**: Validation  
**Returns**: `X00` if check code is missing or invalid  
**Used in**: CVEDA, CVEDP, CHEDPP, CED

### 2. `UnlinkedNotificationDecisionRule`
**Purpose**: Validates that the notification is linked to a matching result  
**Type**: Validation  
**Returns**: `X00` if notification is not linked  
**Used in**: CVEDA, CVEDP, CHEDPP, CED

### 3. `WrongChedTypeDecisionRule`
**Purpose**: Validates that the notification type matches the expected CHED type  
**Type**: Validation  
**Returns**: `X00` if CHED type doesn't match  
**Used in**: CVEDA, CVEDP, CHEDPP, CED

### 4. `MissingPartTwoDecisionRule`
**Purpose**: Validates that the notification has Part Two data  
**Type**: Validation  
**Returns**: `X00` if Part Two is missing  
**Used in**: CVEDA, CVEDP, CED

### 5. `TerminalStatusDecisionRule`
**Purpose**: Handles terminal notification statuses  
**Type**: Decision  
**Returns**:
- `X00` for Cancelled, Replaced, Deleted, SplitConsignment statuses
- Continues to next rule for other statuses

**Used in**: CVEDA, CVEDP, CHEDPP, CED

**Logic**:
```csharp
switch (notification.Status)
{
    case ImportNotificationStatus.Cancelled:
    case ImportNotificationStatus.Replaced:
    case ImportNotificationStatus.Deleted:
    case ImportNotificationStatus.SplitConsignment:
        return new DecisionResolutionResult(DecisionCode.X00);
    default:
        return next(context);
}
```

### 6. `AmendDecisionRule`
**Purpose**: Handles Amend status logic  
**Type**: Decision  
**Returns**:
- `X00` if status is Amend and no previous decision exists
- Previous decision if status is Amend and previous decision exists
- Continues to next rule for other statuses

**Used in**: CVEDA, CVEDP, CED

**Logic**:
```csharp
if (notification.Status == ImportNotificationStatus.Amend)
{
    if (notification.Decision?.DecisionCode == null)
        return new DecisionResolutionResult(DecisionCode.X00);
    
    return new DecisionResolutionResult(notification.Decision.DecisionCode);
}
return next(context);
```

### 7. `InspectionRequiredDecisionRule`
**Purpose**: Determines if inspection is required based on status  
**Type**: Decision  
**Returns**:
- `H01` (Hold for Documentary Check) for Submitted status
- `H02` (Hold for Identity and Physical Check) for InProgress status
- Continues to next rule for other statuses

**Used in**: CVEDA, CVEDP, CED

**Logic**:
```csharp
switch (notification.Status)
{
    case ImportNotificationStatus.Submitted:
        return new DecisionResolutionResult(DecisionCode.H01);
    case ImportNotificationStatus.InProgress:
        return new DecisionResolutionResult(DecisionCode.H02);
    default:
        return next(context);
}
```

### 8. `CommodityCodeValidationRule`
**Purpose**: Validates commodity codes and logs warnings  
**Type**: Post-processing  
**Returns**: Result from next rule (after logging warnings)  
**Used in**: CVEDA, CVEDP, CHEDPP, CED

**Logic**:
- Calls `next(context)` first
- Checks if any commodity has invalid or missing commodity code
- Logs warnings if issues found
- Returns the result from next

### 9. `CommodityWeightOrQuantityValidationRule`
**Purpose**: Validates commodity weight/quantity and logs warnings  
**Type**: Post-processing  
**Returns**: Result from next rule (after logging warnings)  
**Used in**: CVEDA, CVEDP, CHEDPP, CED

**Logic**:
- Calls `next(context)` first
- Checks if any commodity has invalid or missing weight/quantity
- Logs warnings if issues found
- Returns the result from next

## CHED-Specific Rules

### CVEDA Rules

#### `CvedaDecisionRule`
**Purpose**: Applies CVEDA-specific business logic  
**Type**: Decision  
**Returns**: Decision based on consignment decision and not acceptable actions

**Logic**:
1. Checks consignment decision from matching result
2. If consignment decision is not "Acceptable":
   - Maps not acceptable actions to decision codes
   - Returns appropriate decision code (N01-N07)
3. If consignment decision is "Acceptable":
   - Returns `C02` (Release)
4. If no consignment decision:
   - Returns `X00` (Error)

**Decision Mapping**:
- `RequiredForImport` → `N01`
- `ProhibitedForImport` → `N02`
- `HorsePassportRequired` → `N03`
- `NotAcceptableForTranshipment` → `N04`
- `NotAcceptableForTransit` → `N05`
- `NotAcceptableForTemporaryAdmission` → `N06`
- `NotAcceptableForImportEuApprovalRequired` → `N07`

### CVEDP Rules

#### `CvedpIuuCheckRule`
**Purpose**: Checks for IUU (Illegal, Unreported, Unregulated) fishing violations  
**Type**: Decision  
**Returns**:
- `E03` if IUU check fails (goods for transit/export)
- Continues to next rule if IUU check passes

**Logic**:
```csharp
if (notification.PartTwo?.ControlAuthority?.IuuOption == ControlAuthorityIuuOption.IuuOptionB)
    return new DecisionResolutionResult(DecisionCode.E03);

return next(context);
```

#### `CvedpDecisionRule`
**Purpose**: Applies CVEDP-specific business logic  
**Type**: Decision  
**Returns**: Decision based on consignment decision and not acceptable actions

**Logic**: Same as `CvedaDecisionRule` (checks consignment decision and maps not acceptable actions)

### CHEDPP Rules

#### `ChedppDecisionRule`
**Purpose**: Applies CHEDPP-specific business logic  
**Type**: Decision  
**Returns**: Decision based on HMI/PHSI checks and inspection requirements

**Logic**:
1. Checks if any commodity has HMI or PHSI checks required
2. If HMI check is `Required` or `Inconclusive`:
   - Returns `H01` (Hold for Documentary Check)
3. If PHSI check is `Required` or `Inconclusive`:
   - Returns `H02` (Hold for Identity and Physical Check)
4. If status is `Submitted`:
   - Returns `H01` (Hold for Documentary Check)
5. If status is `InProgress`:
   - Returns `H02` (Hold for Identity and Physical Check)
6. If status is `Amend`:
   - Returns previous decision or `X00` if no previous decision
7. Otherwise:
   - Returns `C02` (Release)

**HMI/PHSI Check Mapping**:
- `Required` → Hold decision
- `Inconclusive` → Hold decision
- `NotRequired` → Continue
- `null` → Continue

### CED Rules

#### `CedDecisionRule`
**Purpose**: Applies CED-specific business logic  
**Type**: Decision  
**Returns**: Decision based on consignment decision and not acceptable actions

**Logic**: Same as `CvedaDecisionRule` (checks consignment decision and maps not acceptable actions)

## Pipeline Configurations

### CVEDA Pipeline (10 rules)
1. `OrphanCheckCodeDecisionRule` - Validate check code
2. `UnlinkedNotificationDecisionRule` - Validate linked notification
3. `WrongChedTypeDecisionRule` - Validate CHED type
4. `MissingPartTwoDecisionRule` - Validate Part Two
5. `TerminalStatusDecisionRule` - Handle terminal statuses
6. `AmendDecisionRule` - Handle Amend status
7. `InspectionRequiredDecisionRule` - Check if inspection required
8. `CvedaDecisionRule` - Apply CVEDA business logic
9. `CommodityCodeValidationRule` - Log commodity code warnings
10. `CommodityWeightOrQuantityValidationRule` - Log weight/quantity warnings

### CVEDP Pipeline (11 rules)
1. `OrphanCheckCodeDecisionRule` - Validate check code
2. `UnlinkedNotificationDecisionRule` - Validate linked notification
3. `WrongChedTypeDecisionRule` - Validate CHED type
4. `MissingPartTwoDecisionRule` - Validate Part Two
5. `TerminalStatusDecisionRule` - Handle terminal statuses
6. `AmendDecisionRule` - Handle Amend status
7. `InspectionRequiredDecisionRule` - Check if inspection required
8. `CvedpIuuCheckRule` - Check IUU violations
9. `CvedpDecisionRule` - Apply CVEDP business logic
10. `CommodityCodeValidationRule` - Log commodity code warnings
11. `CommodityWeightOrQuantityValidationRule` - Log weight/quantity warnings

### CHEDPP Pipeline (7 rules)
1. `OrphanCheckCodeDecisionRule` - Validate check code
2. `UnlinkedNotificationDecisionRule` - Validate linked notification
3. `WrongChedTypeDecisionRule` - Validate CHED type
4. `TerminalStatusDecisionRule` - Handle terminal statuses
5. `ChedppDecisionRule` - Apply CHEDPP business logic (includes hold logic)
6. `CommodityCodeValidationRule` - Log commodity code warnings
7. `CommodityWeightOrQuantityValidationRule` - Log weight/quantity warnings

### CED Pipeline (10 rules)
1. `OrphanCheckCodeDecisionRule` - Validate check code
2. `UnlinkedNotificationDecisionRule` - Validate linked notification
3. `WrongChedTypeDecisionRule` - Validate CHED type
4. `MissingPartTwoDecisionRule` - Validate Part Two
5. `TerminalStatusDecisionRule` - Handle terminal statuses
6. `AmendDecisionRule` - Handle Amend status
7. `InspectionRequiredDecisionRule` - Check if inspection required
8. `CedDecisionRule` - Apply CED business logic
9. `CommodityCodeValidationRule` - Log commodity code warnings
10. `CommodityWeightOrQuantityValidationRule` - Log weight/quantity warnings

## Decision Code Reference

### Release Codes (C-series)
- `C02` - Release (goods can proceed)

### Hold Codes (H-series)
- `H01` - Hold for Documentary Check
- `H02` - Hold for Identity and Physical Check

### Not Acceptable Codes (N-series)
- `N01` - Required for Import
- `N02` - Prohibited for Import
- `N03` - Horse Passport Required
- `N04` - Not Acceptable for Transhipment
- `N05` - Not Acceptable for Transit
- `N06` - Not Acceptable for Temporary Admission
- `N07` - Not Acceptable for Import EU Approval Required

### Transit/Export Codes (E-series)
- `E03` - Goods for Transit/Export (IUU violation)

### Error Codes (X-series)
- `X00` - Processing Error (internal error or invalid state)

## Extending the Engine

### Adding a New Rule

1. **Create the rule class**:
```csharp
public sealed class MyNewRule : IDecisionRule
{
    public DecisionResolutionResult Execute(
        DecisionResolutionContext context, 
        DecisionRuleDelegate next)
    {
        // Implement logic
        if (someCondition)
            return new DecisionResolutionResult(DecisionCode.C02);
        
        return next(context);
    }
}
```

2. **Add to pipeline in factory**:
```csharp
private static DecisionRulesEngine CreateEngineForCveda()
{
    var rules = new List<IDecisionRule>
    {
        new OrphanCheckCodeDecisionRule(),
        new MyNewRule(), // Add here
        // ... other rules
    };
    return new DecisionRulesEngine(rules);
}
```

3. **Write unit tests**:
```csharp
[Fact]
public void MyNewRule_ShouldReturnC02_WhenConditionMet()
{
    // Arrange
    var rule = new MyNewRule();
    var context = CreateContext();
    var nextCalled = false;
    DecisionRuleDelegate next = ctx => { nextCalled = true; return null; };
    
    // Act
    var result = rule.Execute(context, next);
    
    // Assert
    Assert.Equal(DecisionCode.C02, result.DecisionCode);
    Assert.False(nextCalled);
}
```

### Adding a New CHED Type

1. **Create CHED-specific rule**:
```csharp
public sealed class MyNewChedDecisionRule : IDecisionRule
{
    public DecisionResolutionResult Execute(
        DecisionResolutionContext context, 
        DecisionRuleDelegate next)
    {
        // Implement CHED-specific logic
    }
}
```

2. **Add factory method**:
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

3. **Update factory switch**:
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

## Testing Strategy

### Unit Tests
Test each rule in isolation:
- Mock the `next` delegate
- Verify correct decision code returned
- Verify `next` is called (or not called) as expected

### Integration Tests
Test complete pipelines:
- Create real context with notification data
- Execute pipeline
- Verify final decision code

### Test Coverage
Ensure tests cover:
- All notification statuses
- All consignment decisions
- All not acceptable actions
- Edge cases (null values, missing data)
- Rule execution order

## File Structure

```
src/Deriver/Decisions/V2/DecisionEngine/
├── IDecisionRule.cs                          # Rule interface
├── DecisionRulesEngine.cs                    # Pipeline executor
├── DecisionRulesEngineFactory.cs             # Factory
├── DecisionResolutionContext.cs              # Context
├── DecisionResolutionResult.cs               # Result
└── DecisionRules/
    ├── OrphanCheckCodeDecisionRule.cs        # Common validation
    ├── UnlinkedNotificationDecisionRule.cs   # Common validation
    ├── WrongChedTypeDecisionRule.cs          # Common validation
    ├── MissingPartTwoDecisionRule.cs         # Common validation
    ├── TerminalStatusDecisionRule.cs         # Common status
    ├── AmendDecisionRule.cs                  # Common status
    ├── InspectionRequiredDecisionRule.cs     # Common hold
    ├── CvedaDecisionRule.cs                  # CVEDA specific
    ├── CvedpIuuCheckRule.cs                  # CVEDP IUU
    ├── CvedpDecisionRule.cs                  # CVEDP specific
    ├── ChedppDecisionRule.cs                 # CHEDPP specific
    ├── CedDecisionRule.cs                    # CED specific
    ├── CommodityCodeValidationRule.cs        # Post-processing
    └── CommodityWeightOrQuantityValidationRule.cs  # Post-processing
```
