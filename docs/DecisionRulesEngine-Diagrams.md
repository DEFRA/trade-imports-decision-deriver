# Decision Rules Engine - Visual Flow Diagrams

## Pipeline Execution Flow

```
┌─────────────────────────────────────────────────────────────┐
│                    Request Arrives                          │
│              (Import Pre-Notification)                      │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│           DecisionRulesEngineFactory.Get()                  │
│                                                             │
│  Switch on notification type:                              │
│  • CVEDA  → CreateEngineForCveda()                         │
│  • CVEDP  → CreateEngineForCvedp()                         │
│  • CHEDPP → CreateEngineForChedpp()                        │
│  • CED    → CreateEngineForCed()                           │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│              DecisionRulesEngine Created                    │
│         with List<IDecisionRule> for CHED type             │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│           DecisionRulesEngine.Resolve()                     │
│                                                             │
│  Builds middleware chain:                                  │
│  Rule N → Rule N-1 → ... → Rule 2 → Rule 1                │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│              Execute Rule Pipeline                          │
│                                                             │
│  Each rule decides:                                        │
│  • Return result (short-circuit)                           │
│  • Call next(context) (continue)                           │
│  • Call next, inspect result, return (post-process)        │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│              DecisionResolutionResult                       │
│                                                             │
│  Contains:                                                 │
│  • DecisionCode (C02, H01, H02, N01-N07, E03, X00)        │
│  • InternalFurtherDetails (error messages)                 │
└─────────────────────────────────────────────────────────────┘
```

## Middleware Pattern

```
┌──────────────────────────────────────────────────────────────┐
│                    Rule 1 (Validation)                       │
│                                                              │
│  Execute(context, next):                                    │
│    if (validation fails)                                    │
│      return Error(X00)  ◄─── SHORT-CIRCUIT                 │
│    else                                                     │
│      return next(context) ──┐                              │
└─────────────────────────────┼───────────────────────────────┘
                              │
                              ▼
┌──────────────────────────────────────────────────────────────┐
│                    Rule 2 (Status Check)                     │
│                                                              │
│  Execute(context, next):                                    │
│    if (status is terminal)                                  │
│      return Error(X00)  ◄─── SHORT-CIRCUIT                 │
│    else                                                     │
│      return next(context) ──┐                              │
└─────────────────────────────┼───────────────────────────────┘
                              │
                              ▼
┌──────────────────────────────────────────────────────────────┐
│                    Rule 3 (Decision)                         │
│                                                              │
│  Execute(context, next):                                    │
│    if (condition met)                                       │
│      return Decision(C02)  ◄─── TERMINAL                   │
│    else                                                     │
│      return next(context) ──┐                              │
└─────────────────────────────┼───────────────────────────────┘
                              │
                              ▼
┌──────────────────────────────────────────────────────────────┐
│              Rule 4 (Post-Processing)                        │
│                                                              │
│  Execute(context, next):                                    │
│    result = next(context) ──┐  ◄─── CALL NEXT FIRST        │
│    LogWarnings(context)      │                              │
│    return result ────────────┘                              │
└──────────────────────────────────────────────────────────────┘
```

## CVEDA Pipeline Flow (10 rules)

```
┌─────────────────────────────────────────────────────────────┐
│  1. OrphanCheckCodeDecisionRule                             │
│     ├─ Check code missing? → X00                           │
│     └─ Valid → Continue                                     │
└────────────────────────┬────────────────────────────────────┘
                         ▼
┌─────────────────────────────────────────────────────────────┐
│  2. UnlinkedNotificationDecisionRule                        │
│     ├─ Not linked? → X00                                   │
│     └─ Linked → Continue                                    │
└────────────────────────┬────────────────────────────────────┘
                         ▼
┌─────────────────────────────────────────────────────────────┐
│  3. WrongChedTypeDecisionRule                               │
│     ├─ Wrong type? → X00                                   │
│     └─ Correct type → Continue                              │
└────────────────────────┬────────────────────────────────────┘
                         ▼
┌─────────────────────────────────────────────────────────────┐
│  4. MissingPartTwoDecisionRule                              │
│     ├─ Part Two missing? → X00                             │
│     └─ Part Two exists → Continue                           │
└────────────────────────┬────────────────────────────────────┘
                         ▼
┌─────────────────────────────────────────────────────────────┐
│  5. TerminalStatusDecisionRule                              │
│     ├─ Cancelled/Replaced/Deleted/Split? → X00             │
│     └─ Other status → Continue                              │
└────────────────────────┬────────────────────────────────────┘
                         ▼
┌─────────────────────────────────────────────────────────────┐
│  6. AmendDecisionRule                                       │
│     ├─ Amend + no previous decision? → X00                 │
│     ├─ Amend + previous decision? → Previous decision      │
│     └─ Not Amend → Continue                                 │
└────────────────────────┬────────────────────────────────────┘
                         ▼
┌─────────────────────────────────────────────────────────────┐
│  7. InspectionRequiredDecisionRule                          │
│     ├─ Submitted? → H01                                    │
│     ├─ InProgress? → H02                                   │
│     └─ Other → Continue                                     │
└────────────────────────┬────────────────────────────────────┘
                         ▼
┌─────────────────────────────────────────────────────────────┐
│  8. CvedaDecisionRule                                       │
│     ├─ Not Acceptable? → N01-N07                           │
│     ├─ Acceptable? → C02                                   │
│     └─ Unknown → X00                                        │
└────────────────────────┬────────────────────────────────────┘
                         ▼
┌─────────────────────────────────────────────────────────────┐
│  9. CommodityCodeValidationRule                             │
│     └─ Log warnings if commodity code issues                │
└────────────────────────┬────────────────────────────────────┘
                         ▼
┌─────────────────────────────────────────────────────────────┐
│  10. CommodityWeightOrQuantityValidationRule                │
│      └─ Log warnings if weight/quantity issues              │
└────────────────────────┬────────────────────────────────────┘
                         ▼
                    [RESULT]
```

## CVEDP Pipeline Flow (11 rules)

```
┌─────────────────────────────────────────────────────────────┐
│  1-7. Same as CVEDA (validation, status, amend, inspection) │
└────────────────────────┬────────────────────────────────────┘
                         ▼
┌─────────────────────────────────────────────────────────────┐
│  8. CvedpIuuCheckRule                                       │
│     ├─ IUU Option B? → E03 (Transit/Export)               │
│     └─ No IUU issue → Continue                              │
└────────────────────────┬────────────────────────────────────┘
                         ▼
┌─────────────────────────────────────────────────────────────┐
│  9. CvedpDecisionRule                                       │
│     ├─ Not Acceptable? → N01-N07                           │
│     ├─ Acceptable? → C02                                   │
│     └─ Unknown → X00                                        │
└────────────────────────┬────────────────────────────────────┘
                         ▼
┌─────────────────────────────────────────────────────────────┐
│  10-11. Commodity validation (same as CVEDA)                │
└────────────────────────┬────────────────────────────────────┘
                         ▼
                    [RESULT]
```

## CHEDPP Pipeline Flow (7 rules)

```
┌─────────────────────────────────────────────────────────────┐
│  1. OrphanCheckCodeDecisionRule                             │
│     ├─ Check code missing? → X00                           │
│     └─ Valid → Continue                                     │
└────────────────────────┬────────────────────────────────────┘
                         ▼
┌─────────────────────────────────────────────────────────────┐
│  2. UnlinkedNotificationDecisionRule                        │
│     ├─ Not linked? → X00                                   │
│     └─ Linked → Continue                                    │
└────────────────────────┬────────────────────────────────────┘
                         ▼
┌─────────────────────────────────────────────────────────────┐
│  3. WrongChedTypeDecisionRule                               │
│     ├─ Wrong type? → X00                                   │
│     └─ Correct type → Continue                              │
└────────────────────────┬────────────────────────────────────┘
                         ▼
┌─────────────────────────────────────────────────────────────┐
│  4. TerminalStatusDecisionRule                              │
│     ├─ Cancelled/Replaced/Deleted/Split? → X00             │
│     └─ Other status → Continue                              │
└────────────────────────┬────────────────────────────────────┘
                         ▼
┌─────────────────────────────────────────────────────────────┐
│  5. ChedppDecisionRule                                      │
│     ├─ HMI Required/Inconclusive? → H01                    │
│     ├─ PHSI Required/Inconclusive? → H02                   │
│     ├─ Submitted? → H01                                    │
│     ├─ InProgress? → H02                                   │
│     ├─ Amend? → Previous decision or X00                   │
│     └─ Otherwise → C02                                      │
└────────────────────────┬────────────────────────────────────┘
                         ▼
┌─────────────────────────────────────────────────────────────┐
│  6-7. Commodity validation                                  │
└────────────────────────┬────────────────────────────────────┘
                         ▼
                    [RESULT]
```

## CED Pipeline Flow (10 rules)

```
┌─────────────────────────────────────────────────────────────┐
│  1-7. Same as CVEDA (validation, status, amend, inspection) │
└────────────────────────┬────────────────────────────────────┘
                         ▼
┌─────────────────────────────────────────────────────────────┐
│  8. CedDecisionRule                                         │
│     ├─ Not Acceptable? → N01-N07                           │
│     ├─ Acceptable? → C02                                   │
│     └─ Unknown → X00                                        │
└────────────────────────┬────────────────────────────────────┘
                         ▼
┌─────────────────────────────────────────────────────────────┐
│  9-10. Commodity validation                                 │
└────────────────────────┬────────────────────────────────────┘
                         ▼
                    [RESULT]
```

## Decision Code Categories

```
┌─────────────────────────────────────────────────────────────┐
│                    DECISION CODES                           │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ┌─────────────┐                                           │
│  │ C02         │  Release (goods can proceed)              │
│  └─────────────┘                                           │
│                                                             │
│  ┌─────────────┐                                           │
│  │ H01         │  Hold for Documentary Check               │
│  │ H02         │  Hold for Identity and Physical Check     │
│  └─────────────┘                                           │
│                                                             │
│  ┌─────────────┐                                           │
│  │ N01         │  Required for Import                      │
│  │ N02         │  Prohibited for Import                    │
│  │ N03         │  Horse Passport Required                  │
│  │ N04         │  Not Acceptable for Transhipment          │
│  │ N05         │  Not Acceptable for Transit               │
│  │ N06         │  Not Acceptable for Temporary Admission   │
│  │ N07         │  Not Acceptable for Import EU Approval    │
│  └─────────────┘                                           │
│                                                             │
│  ┌─────────────┐                                           │
│  │ E03         │  Goods for Transit/Export (IUU)           │
│  └─────────────┘                                           │
│                                                             │
│  ┌─────────────┐                                           │
│  │ X00         │  Processing Error                         │
│  └─────────────┘                                           │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

## Rule Execution Patterns

### Pattern 1: Validation (Short-Circuit)
```
┌──────────────────┐
│  Validation Rule │
└────────┬─────────┘
         │
    ┌────▼────┐
    │ Valid?  │
    └─┬────┬──┘
      │    │
   NO │    │ YES
      │    │
      ▼    ▼
   [X00] [next(context)]
```

### Pattern 2: Decision (Conditional)
```
┌──────────────────┐
│  Decision Rule   │
└────────┬─────────┘
         │
    ┌────▼────────┐
    │ Condition?  │
    └─┬────┬──────┘
      │    │
   YES│    │ NO
      │    │
      ▼    ▼
  [Decision] [next(context)]
```

### Pattern 3: Post-Processing
```
┌──────────────────────┐
│ Post-Processing Rule │
└──────────┬───────────┘
           │
           ▼
    [result = next(context)]
           │
           ▼
    [Log/Validate]
           │
           ▼
      [return result]
```

## Context Flow

```
┌─────────────────────────────────────────────────────────────┐
│              DecisionResolutionContext                      │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  • Notification                                            │
│    ├─ ImportNotificationType (CVEDA/CVEDP/CHEDPP/CED)     │
│    ├─ Status (Submitted/InProgress/Amend/etc.)            │
│    ├─ PartTwo (consignment data)                          │
│    ├─ Decision (previous decision if Amend)               │
│    └─ Commodities (list of commodity data)                │
│                                                             │
│  • MatchingResult                                          │
│    ├─ ConsignmentDecision                                 │
│    └─ NotAcceptableActions                                │
│                                                             │
│  • CommodityRiskResult                                     │
│    ├─ HMI Check Status (CHEDPP only)                      │
│    └─ PHSI Check Status (CHEDPP only)                     │
│                                                             │
└─────────────────────────────────────────────────────────────┘
                         │
                         │ Passed through pipeline
                         ▼
┌─────────────────────────────────────────────────────────────┐
│              DecisionResolutionResult                       │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  • DecisionCode (C02, H01, H02, N01-N07, E03, X00)        │
│  • InternalFurtherDetails (error messages, warnings)       │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```
