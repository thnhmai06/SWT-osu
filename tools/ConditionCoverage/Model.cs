namespace ConditionCoverage;

/// <summary>Một điều kiện nguyên tử (toán hạng lá của một decision) đã được gắn probe. <c>UiFramework</c>: nằm trong type dẫn xuất từ <c>osu.Framework.Graphics.Drawable</c>.</summary>
public sealed record ConditionInfo(
    int Id,
    int DecisionId,
    string Assembly,
    string File,
    int Line,
    string Text,
    string Namespace,
    string Type,
    string Member,
    bool UiFramework = false);

/// <summary>Một decision (biểu thức boolean điều khiển luồng hoặc cây &amp;&amp;/||).</summary>
public sealed record DecisionInfo(int Id, string Assembly, string File, int Line, string Kind, string Text);

public sealed class InstrumentMap
{
    public string Assembly { get; set; } = "";
    public List<DecisionInfo> Decisions { get; set; } = new();
    public List<ConditionInfo> Conditions { get; set; } = new();
}
