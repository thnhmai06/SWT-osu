using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ConditionCoverage;

/// <summary>Cấp id và gom thông tin điều kiện/decision cho một assembly.</summary>
public sealed class Registry(string assembly)
{
    public string Assembly { get; } = assembly;
    public List<ConditionInfo> Conditions { get; } = new();
    public List<DecisionInfo> Decisions { get; } = new();

    /// <summary>Tên class probe, duy nhất theo assembly để tránh đụng nhau qua InternalsVisibleTo.</summary>
    public string ProbeName => ProbeSource.NameFor(Assembly);
}

/// <summary>
/// Bọc mỗi điều kiện nguyên tử (lá của cây &amp;&amp; / || / ! / &amp; / |) trong decision bằng probe ghi nhận true/false.
/// Giữ nguyên thứ tự đánh giá và short-circuit: chỉ thay <c>x</c> bằng <c>P.C(id, x)</c> (hoặc dạng ternary cho điều kiện có khai báo biến).
/// </summary>
public sealed class ConditionRewriter : CSharpSyntaxRewriter
{
    private readonly SemanticModel model;
    private readonly string file;
    private readonly Registry registry;
    private readonly HashSet<SyntaxNode> roots = new();
    private readonly Dictionary<SyntaxNode, string> rootKinds = new();

    public ConditionRewriter(SemanticModel model, string file, Registry registry)
    {
        this.model = model;
        this.file = file;
        this.registry = registry;
        collectRoots(model.SyntaxTree.GetRoot());
    }

    public static SyntaxNode Rewrite(SemanticModel model, string file, Registry registry)
        => new ConditionRewriter(model, file, registry).Visit(model.SyntaxTree.GetRoot())!;

    private void collectRoots(SyntaxNode treeRoot)
    {
        foreach (var node in treeRoot.DescendantNodes())
        {
            switch (node)
            {
                case IfStatementSyntax s: addRoot(s.Condition, "if"); break;
                case WhileStatementSyntax s: addRoot(s.Condition, "while"); break;
                case DoStatementSyntax s: addRoot(s.Condition, "do-while"); break;
                case ForStatementSyntax { Condition: { } c }: addRoot(c, "for"); break;
                case ConditionalExpressionSyntax s: addRoot(s.Condition, "?:"); break;
                case BinaryExpressionSyntax b when isCombinator(b): addRoot(top(b), "expr"); break;
            }
        }
    }

    private void addRoot(ExpressionSyntax e, string kind)
    {
        if (roots.Add(e)) rootKinds[e] = kind;
    }

    /// <summary>Leo lên qua (), ! và combinator cha để lấy gốc của cây boolean.</summary>
    private ExpressionSyntax top(ExpressionSyntax n)
    {
        while (n.Parent is ParenthesizedExpressionSyntax
                   or PrefixUnaryExpressionSyntax { RawKind: (int)SyntaxKind.LogicalNotExpression }
                   or BinaryExpressionSyntax)
        {
            if (n.Parent is BinaryExpressionSyntax pb && !isCombinator(pb)) break;
            if (n.Parent is PrefixUnaryExpressionSyntax pu && !isBoolNot(pu)) break;
            n = (ExpressionSyntax)n.Parent;
        }

        return n;
    }

    private bool isBool(ExpressionSyntax e)
    {
        var info = model.GetTypeInfo(e);
        return info.Type?.SpecialType == SpecialType.System_Boolean && info.ConvertedType?.SpecialType == SpecialType.System_Boolean;
    }

    private bool isCombinator(BinaryExpressionSyntax b)
    {
        if (!b.IsKind(SyntaxKind.LogicalAndExpression) && !b.IsKind(SyntaxKind.LogicalOrExpression)
                                                       && !b.IsKind(SyntaxKind.BitwiseAndExpression) && !b.IsKind(SyntaxKind.BitwiseOrExpression))
            return false;

        // hai vế đều là bool => chắc chắn là toán tử dựng sẵn (operator người dùng định nghĩa cần vế không phải bool).
        return isBool(b.Left) && isBool(b.Right);
    }

    private bool isBoolNot(PrefixUnaryExpressionSyntax u)
        => u.IsKind(SyntaxKind.LogicalNotExpression) && isBool(u.Operand);

    public override SyntaxNode? Visit(SyntaxNode? node)
    {
        if (node is null) return null;

        // Lambda chuyển thành Expression<T> (vd. Realm LINQ) phải giữ nguyên vì provider dịch cây biểu thức.
        if (node is LambdaExpressionSyntax or AnonymousMethodExpressionSyntax && isExpressionTree(node))
            return node;

        if (node is AttributeListSyntax or EnumMemberDeclarationSyntax) return node;
        if (node is ParameterSyntax { Default: not null }) return node;

        if (node is ExpressionSyntax e && roots.Contains(e) && shouldInstrument(e))
        {
            int before = registry.Conditions.Count;
            int decisionId = registry.Decisions.Count;
            var result = instrument(e, decisionId);

            if (registry.Conditions.Count == before) return result;

            registry.Decisions.Add(new DecisionInfo(decisionId, registry.Assembly, file, lineOf(e), rootKinds[e], compact(e)));
            return result;
        }

        return base.Visit(node);
    }

    private bool isExpressionTree(SyntaxNode lambda)
    {
        var t = model.GetTypeInfo(lambda).ConvertedType as INamedTypeSymbol;
        return t?.OriginalDefinition.ToDisplayString() == "System.Linq.Expressions.Expression<TDelegate>";
    }

    private bool shouldInstrument(ExpressionSyntax e)
    {
        if (model.GetConstantValue(e).HasValue) return false;

        // trong thuộc tính / giá trị mặc định / const: không chạy lúc runtime.
        foreach (var a in e.Ancestors())
        {
            if (a is AttributeSyntax or EnumMemberDeclarationSyntax) return false;
            if (a is ParameterSyntax) return false;
            if (a is LambdaExpressionSyntax or AnonymousMethodExpressionSyntax && isExpressionTree(a)) return false;
        }

        return true;
    }

    private ExpressionSyntax instrument(ExpressionSyntax e, int decisionId)
    {
        switch (e)
        {
            case ParenthesizedExpressionSyntax p:
                return p.WithExpression(instrument(p.Expression, decisionId));

            case PrefixUnaryExpressionSyntax u when isBoolNot(u):
                return u.WithOperand(instrument(u.Operand, decisionId));

            case BinaryExpressionSyntax b when isCombinator(b):
                return b.WithLeft(instrument(b.Left, decisionId)).WithRight(instrument(b.Right, decisionId));

            default:
                return leaf(e, decisionId);
        }
    }

    private ExpressionSyntax leaf(ExpressionSyntax e, int decisionId)
    {
        var inner = (ExpressionSyntax)(base.Visit(e) ?? e);

        if (!isBool(e) || model.GetConstantValue(e).HasValue) return inner;

        int id = registry.Conditions.Count;
        registry.Conditions.Add(new ConditionInfo(id, decisionId, registry.Assembly, file, lineOf(e), compact(e), namespaceOf(e), typeOf(e), memberOf(e), inUiFramework(e)));

        var bare = inner.WithoutTrivia();
        string p = $"global::{registry.ProbeName}";
        ExpressionSyntax wrapped;

        if (declaresVariables(e))
        {
            // Giữ definite-assignment của pattern/out var: (leaf ? (P.T(id) || true) : (P.F(id) && false))
            wrapped = SyntaxFactory.ParseExpression($"(({bare}) ? ({p}.T({id}) || true) : ({p}.F({id}) && false))");
        }
        else
        {
            wrapped = SyntaxFactory.ParseExpression($"{p}.C({id}, {bare})");
        }

        return wrapped.WithTriviaFrom(e);
    }

    private readonly Dictionary<ITypeSymbol, bool> uiCache = new(SymbolEqualityComparer.Default);

    /// <summary>Điều kiện nằm trong (hoặc lồng trong) type dẫn xuất từ <c>osu.Framework.Graphics.Drawable</c> nhưng không qua <c>Component</c>: code UI cần scene graph/game host nên không unit test thuần được.</summary>
    private bool inUiFramework(SyntaxNode n)
    {
        for (var t = model.GetEnclosingSymbol(n.SpanStart)?.ContainingType; t != null; t = t.ContainingType)
        {
            if (!uiCache.TryGetValue(t, out bool ui))
            {
                // Component kế thừa Drawable nhưng không hiển thị (logic thuần, vd. EditorBeatmap) nên không tính là UI.
                bool drawable = false, component = false;
                for (var b = t; b != null; b = b.BaseType)
                {
                    string name = b.OriginalDefinition.ToDisplayString();
                    drawable |= name == "osu.Framework.Graphics.Drawable";
                    component |= name == "osu.Framework.Graphics.Component";
                }

                ui = drawable && !component;
                uiCache[t] = ui;
            }

            if (ui) return true;
        }

        return false;
    }

    private static bool declaresVariables(ExpressionSyntax e)
        => e.DescendantNodesAndSelf().Any(n => n is SingleVariableDesignationSyntax or DiscardDesignationSyntax or ParenthesizedVariableDesignationSyntax);

    private static int lineOf(SyntaxNode n) => n.GetLocation().GetLineSpan().StartLinePosition.Line + 1;

    private static string compact(SyntaxNode n)
    {
        string s = string.Join(" ", n.WithoutTrivia().ToString().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return s.Length > 160 ? s[..157] + "..." : s;
    }

    private static string namespaceOf(SyntaxNode n)
    {
        var parts = n.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().Select(x => x.Name.ToString()).Reverse();
        string s = string.Join(".", parts);
        return s.Length == 0 ? "(global)" : s;
    }

    private static string typeOf(SyntaxNode n)
        => string.Join(".", n.Ancestors().OfType<BaseTypeDeclarationSyntax>().Select(x => x.Identifier.Text).Reverse().DefaultIfEmpty("(none)"));

    private static string memberOf(SyntaxNode n)
    {
        MemberDeclarationSyntax? member = null;
        AccessorDeclarationSyntax? accessor = null;

        foreach (var a in n.Ancestors())
        {
            if (a is BaseTypeDeclarationSyntax) break;
            if (a is AccessorDeclarationSyntax acc) accessor = acc;
            if (a is MemberDeclarationSyntax m) member = m;
        }

        static string ps(BaseParameterListSyntax? l) => l == null ? "" : string.Join(", ", l.Parameters.Select(p => p.Type?.ToString() ?? "?"));

        return member switch
        {
            MethodDeclarationSyntax m => $"{m.Identifier.Text}({ps(m.ParameterList)})",
            ConstructorDeclarationSyntax c => $".ctor({ps(c.ParameterList)})",
            DestructorDeclarationSyntax => "~finalizer()",
            OperatorDeclarationSyntax o => $"operator {o.OperatorToken.Text}({ps(o.ParameterList)})",
            ConversionOperatorDeclarationSyntax o => $"conversion operator {o.Type}({ps(o.ParameterList)})",
            PropertyDeclarationSyntax p => p.Identifier.Text + (accessor != null ? "." + accessor.Keyword.Text : ""),
            IndexerDeclarationSyntax => "this[]" + (accessor != null ? "." + accessor.Keyword.Text : ""),
            EventDeclarationSyntax ev => ev.Identifier.Text,
            FieldDeclarationSyntax f => f.Declaration.Variables.First().Identifier.Text + " (field initializer)",
            EventFieldDeclarationSyntax ef => ef.Declaration.Variables.First().Identifier.Text + " (field initializer)",
            null => "(no member)",
            _ => member.Kind().ToString(),
        };
    }
}
