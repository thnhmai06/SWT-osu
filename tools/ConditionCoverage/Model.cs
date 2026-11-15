namespace ConditionCoverage;

/// <summary>Một điều kiện nguyên tử (toán hạng lá của một decision) đã được gắn probe. <c>Exclusion</c> khác rỗng nếu điều kiện nằm trong code phụ thuộc ngoài (UI, Input, Host/IO, Database/Online, DI, TestInfra) nên không phải logic thuần.</summary>
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
    string Exclusion = "");

/// <summary>Một decision (biểu thức boolean điều khiển luồng hoặc cây &amp;&amp;/||).</summary>
public sealed record DecisionInfo(int Id, string Assembly, string File, int Line, string Kind, string Text);

public sealed class InstrumentMap
{
    public string Assembly { get; set; } = "";
    public List<DecisionInfo> Decisions { get; set; } = new();
    public List<ConditionInfo> Conditions { get; set; } = new();
}

/// <summary>Một type hoặc member bị loại khỏi logic thuần. <c>Group</c> giữ tiền tố <c>~</c> nếu nhóm bị ẩn khỏi báo cáo condition (--hide-skipped).</summary>
public sealed record ExcludedSpan(string File, int StartLine, int EndLine, string Group, string Kind);

/// <summary>Kết quả <c>list-excluded</c>: <c>Files</c> là mọi file đã quét (để biết file nào đã được phân loại), <c>Spans</c> là phần bị loại.</summary>
public sealed class ExcludedMap
{
    public string Assembly { get; set; } = "";
    public List<string> Files { get; set; } = new();
    public List<ExcludedSpan> Spans { get; set; } = new();
}
