namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions;

public enum DecisionCode
{
    C02,
    C03,
    C05,
    C06,
    C07,
    C08,

    H01,
    H02,

    X00,

    N01,
    N02,
    N03,
    N04,
    N07,

    E03,
}

public enum DecisionInternalFurtherDetail
{
    E80, // No Match (Including Cancelled or Replaced or Deleted or Partially Rejected or Split Consignment)
    E85, // Missing PHSI check
    E86, // No HMI check
    E87, // No Documents
    E88, // Cancelled or Replaced or Deleted
    E89, // Item with document references where none are valid format
    E90, // No Decision Finder found
    E94, // IUU not indicated in PartTwo?.ControlAuthority?.IuuCheckRequired but "H224" requested in Items[]?.Checks[]?.CheckCode
    E95, // Unexpected value in PartTwo?.Decision?.IuuOption
    E96, // Unexpected value in PartTwo?.Decision?.DecisionEnum
    E97, // Unexpected value in PartTwo?.Decision?.NotAcceptableAction

    // E98,    // Not implemented
    E99, // Other unexpected data error
}
