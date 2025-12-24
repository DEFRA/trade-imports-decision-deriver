using Defra.TradeImportsDecisionDeriver.Deriver.Decisions;

namespace Defra.TradeImportsDecisionDeriver.TestFixtures;

public sealed class DecisionImportPreNotificationBuilder
{
    private string? _id;
    private DateTime? _updatedSource;
    private string? _notAcceptableAction;
    private readonly List<string> _notAcceptableReasons = new();
    private string? _consignmentDecision;
    private bool? _iuuCheckRequired;
    private string? _iuuOption;
    private string? _inspectionRequired;
    private string? _importNotificationType;
    private string? _status;
    private readonly List<DecisionCommodityComplement> _commodities = new();
    private readonly List<DecisionCommodityCheck.Check> _commodityChecks = new();
    private bool _hasPartTwo;

    private DecisionImportPreNotificationBuilder() { }

    public static DecisionImportPreNotificationBuilder Create() => new DecisionImportPreNotificationBuilder();

    public DecisionImportPreNotificationBuilder WithId(string id)
    {
        _id = id ?? throw new ArgumentNullException(nameof(id));
        return this;
    }

    public DecisionImportPreNotificationBuilder WithUpdatedSource(DateTime? updatedSource)
    {
        _updatedSource = updatedSource;
        return this;
    }

    public DecisionImportPreNotificationBuilder WithNotAcceptableAction(string? action)
    {
        _notAcceptableAction = action;
        return this;
    }

    public DecisionImportPreNotificationBuilder WithNotAcceptableReasons(string[] reasons)
    {
        _notAcceptableReasons.Clear();
        if (reasons != null)
            _notAcceptableReasons.AddRange(reasons);
        return this;
    }

    public DecisionImportPreNotificationBuilder AddNotAcceptableReason(string reason)
    {
        if (!string.IsNullOrEmpty(reason))
            _notAcceptableReasons.Add(reason);
        return this;
    }

    public DecisionImportPreNotificationBuilder WithConsignmentDecision(string? decision)
    {
        _consignmentDecision = decision;
        return this;
    }

    public DecisionImportPreNotificationBuilder WithIuuCheckRequired(bool? required)
    {
        _iuuCheckRequired = required;
        return this;
    }

    public DecisionImportPreNotificationBuilder WithIuuOption(string? option)
    {
        _iuuOption = option;
        return this;
    }

    public DecisionImportPreNotificationBuilder WithInspectionRequired(string? inspectionRequired)
    {
        _inspectionRequired = inspectionRequired;
        return this;
    }

    public DecisionImportPreNotificationBuilder WithImportNotificationType(string? type)
    {
        _importNotificationType = type;
        return this;
    }

    public DecisionImportPreNotificationBuilder WithStatus(string? status)
    {
        _status = status;
        return this;
    }

    public DecisionImportPreNotificationBuilder WithHasPartTwo(bool hasPartTwo)
    {
        _hasPartTwo = hasPartTwo;
        return this;
    }

    public DecisionImportPreNotificationBuilder AddCommodity(DecisionCommodityComplement commodity)
    {
        if (commodity is null)
            throw new ArgumentNullException(nameof(commodity));
        _commodities.Add(commodity);
        return this;
    }

    public DecisionImportPreNotificationBuilder AddCommodity(Action<DecisionCommodityComplementBuilder> configure)
    {
        var b = new DecisionCommodityComplementBuilder();
        configure(b);
        _commodities.Add(b.Build());
        return this;
    }

    public DecisionImportPreNotificationBuilder AddCommodityCheck(DecisionCommodityCheck.Check check)
    {
        if (check is null)
            throw new ArgumentNullException(nameof(check));
        _commodityChecks.Add(check);
        return this;
    }

    public DecisionImportPreNotificationBuilder AddCommodityCheck(Action<DecisionCommodityCheckBuilder> configure)
    {
        var b = new DecisionCommodityCheckBuilder();
        configure(b);
        _commodityChecks.Add(b.Build());
        return this;
    }

    public DecisionImportPreNotification Build()
    {
        if (string.IsNullOrEmpty(_id))
            throw new InvalidOperationException("Id is required. Call WithId(...) before Build().");

        return new DecisionImportPreNotification
        {
            Id = _id!,
            UpdatedSource = _updatedSource,
            NotAcceptableAction = _notAcceptableAction,
            NotAcceptableReasons = _notAcceptableReasons.Count == 0 ? null : _notAcceptableReasons.ToArray(),
            ConsignmentDecision = _consignmentDecision,
            IuuCheckRequired = _iuuCheckRequired,
            IuuOption = _iuuOption,
            InspectionRequired = _inspectionRequired,
            ImportNotificationType = _importNotificationType,
            Status = _status,
            Commodities = _commodities.Count == 0 ? Array.Empty<DecisionCommodityComplement>() : _commodities.ToArray(),
            CommodityChecks =
                _commodityChecks.Count == 0 ? Array.Empty<DecisionCommodityCheck.Check>() : _commodityChecks.ToArray(),
            HasPartTwo = _hasPartTwo,
        };
    }

    // Nested builders for commodity & commodity check

    public sealed class DecisionCommodityComplementBuilder
    {
        private string? _hmiDecision;
        private string? _phsiDecision;
        private string? _commodityCode;

        public DecisionCommodityComplementBuilder WithHmiDecision(string? hmi)
        {
            _hmiDecision = hmi;
            return this;
        }

        public DecisionCommodityComplementBuilder WithCommodityCode(string? code)
        {
            _commodityCode = code;
            return this;
        }

        public DecisionCommodityComplementBuilder WithPhsiDecision(string? phsi)
        {
            _phsiDecision = phsi;
            return this;
        }

        public DecisionCommodityComplement Build() =>
            new DecisionCommodityComplement
            {
                CommodityCode = _commodityCode,
                HmiDecision = _hmiDecision,
                PhsiDecision = _phsiDecision,
            };
    }

    public sealed class DecisionCommodityCheckBuilder
    {
        private string? _type;
        private string? _status;

        public DecisionCommodityCheckBuilder WithType(string? type)
        {
            _type = type;
            return this;
        }

        public DecisionCommodityCheckBuilder WithStatus(string status)
        {
            _status = status ?? throw new ArgumentNullException(nameof(status));
            return this;
        }

        public DecisionCommodityCheck.Check Build() =>
            new DecisionCommodityCheck.Check
            {
                Type = _type,
                Status =
                    _status
                    ?? throw new InvalidOperationException("Status is required for DecisionCommodityCheck.Check"),
            };
    }
}
