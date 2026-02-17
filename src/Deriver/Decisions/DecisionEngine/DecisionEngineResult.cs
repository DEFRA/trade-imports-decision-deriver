namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine;

public readonly record struct DecisionEngineResult
{
    private DecisionEngineResult(DecisionCode Code, DecisionInternalFurtherDetail? FurtherDetail = null)
    {
        this.Code = Code;
        this.FurtherDetail = FurtherDetail;
    }

    public DecisionCode Code { get; init; }
    public DecisionInternalFurtherDetail? FurtherDetail { get; init; }

    public static DecisionEngineResult Create(DecisionCode code, DecisionInternalFurtherDetail? furtherDetail = null)
    {
        return new DecisionEngineResult(code, furtherDetail);
    }

    public static DecisionEngineResult WrongChedType => new(DecisionCode.X00, DecisionInternalFurtherDetail.E84);

    public static DecisionEngineResult Unlinked => new(DecisionCode.X00, DecisionInternalFurtherDetail.E70);

    public static DecisionEngineResult H01 => new(DecisionCode.H01);
    public static DecisionEngineResult H02 => new(DecisionCode.H02);
    public static DecisionEngineResult H01E74 => new(DecisionCode.H01, DecisionInternalFurtherDetail.E74);
    public static DecisionEngineResult H01E80 => new(DecisionCode.H01, DecisionInternalFurtherDetail.E80);

    public static DecisionEngineResult H01E88 => new(DecisionCode.H01, DecisionInternalFurtherDetail.E88);
    public static DecisionEngineResult H01E85 => new(DecisionCode.H01, DecisionInternalFurtherDetail.E85);
    public static DecisionEngineResult H01E86 => new(DecisionCode.H01, DecisionInternalFurtherDetail.E86);

    public static DecisionEngineResult H02E80 => new(DecisionCode.H02, DecisionInternalFurtherDetail.E80);
    public static DecisionEngineResult H02E93 => new(DecisionCode.H02, DecisionInternalFurtherDetail.E93);
    public static DecisionEngineResult H02E94 => new(DecisionCode.H02, DecisionInternalFurtherDetail.E94);

    public static DecisionEngineResult H01E99 => new(DecisionCode.H01, DecisionInternalFurtherDetail.E99);

    public static DecisionEngineResult X00 => new(DecisionCode.X00);
    public static DecisionEngineResult X00E20 => new(DecisionCode.X00, DecisionInternalFurtherDetail.E20);
    public static DecisionEngineResult X00E99 => new(DecisionCode.X00, DecisionInternalFurtherDetail.E99);

    public static DecisionEngineResult X00E97 => new(DecisionCode.X00, DecisionInternalFurtherDetail.E97);

    public static DecisionEngineResult X00E96 => new(DecisionCode.X00, DecisionInternalFurtherDetail.E96);
    public static DecisionEngineResult X00E93 => new(DecisionCode.X00, DecisionInternalFurtherDetail.E93);
    public static DecisionEngineResult X00E94 => new(DecisionCode.X00, DecisionInternalFurtherDetail.E94);

    public static DecisionEngineResult X00E71 => new(DecisionCode.X00, DecisionInternalFurtherDetail.E71);
    public static DecisionEngineResult X00E72 => new(DecisionCode.X00, DecisionInternalFurtherDetail.E72);
    public static DecisionEngineResult X00E73 => new(DecisionCode.X00, DecisionInternalFurtherDetail.E73);
    public static DecisionEngineResult X00E75 => new(DecisionCode.X00, DecisionInternalFurtherDetail.E75);

    public static DecisionEngineResult X00E83 => new(DecisionCode.X00, DecisionInternalFurtherDetail.E83);

    public static DecisionEngineResult X00E87 => new(DecisionCode.X00, DecisionInternalFurtherDetail.E87);

    public static DecisionEngineResult X00E88 => new(DecisionCode.X00, DecisionInternalFurtherDetail.E88);
    public static DecisionEngineResult C02 => new(DecisionCode.C02);
    public static DecisionEngineResult C03 => new(DecisionCode.C03);
    public static DecisionEngineResult C05 => new(DecisionCode.C05);
    public static DecisionEngineResult C06 => new(DecisionCode.C06);
    public static DecisionEngineResult C07 => new(DecisionCode.C07);
    public static DecisionEngineResult C08 => new(DecisionCode.C08);
    public static DecisionEngineResult N01 => new(DecisionCode.N01);
    public static DecisionEngineResult N02 => new(DecisionCode.N02);

    public static DecisionEngineResult N03 => new(DecisionCode.N03);

    public static DecisionEngineResult N04 => new(DecisionCode.N04);

    public static DecisionEngineResult N07 => new(DecisionCode.N07);

    public static DecisionEngineResult E03 => new(DecisionCode.E03);
}
