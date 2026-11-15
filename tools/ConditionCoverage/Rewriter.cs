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
        registry.Conditions.Add(new ConditionInfo(id, decisionId, registry.Assembly, file, lineOf(e), compact(e), namespaceOf(e), typeOf(e), memberOf(e), exclusionOf(e)));

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

    // ---- Phân loại phụ thuộc ngoài (không phải logic thuần) ----

    private static readonly (string Reason, string[] Namespaces)[] externalNamespaces =
    [
        ("Input", ["osu.Framework.Input", "osu.Game.Input"]),
        ("Host/IO", ["osu.Framework.Platform", "osu.Framework.IO", "osu.Framework.Audio", "osu.Framework.Graphics.Textures", "osu.Framework.Graphics.Rendering",
                     "osu.Framework.Graphics.Shaders", "osu.Framework.Threading", "osu.Game.Skinning", "osu.Game.IO"]),
        ("Database/Online", ["Realms", "osu.Game.Database", "osu.Game.Online", "System.Net"]),
    ];

    private static bool inNamespace(INamespaceSymbol? ns, string[] prefixes)
    {
        string s = ns?.ToDisplayString() ?? "";
        return prefixes.Any(p => s == p || s.StartsWith(p + "."));
    }

    private readonly Dictionary<ITypeSymbol, string> typeDependencyCache = new(SymbolEqualityComparer.Default);
    private readonly Dictionary<ITypeSymbol, string> typeSelfCache = new(SymbolEqualityComparer.Default);
    private readonly Dictionary<SyntaxNode, string> memberCache = new();

    /// <summary>Lý do loại (rỗng nếu là logic thuần): type chứa là UI/Input/Host/test infra, hoặc member phụ thuộc ngoài.</summary>
    private string exclusionOf(SyntaxNode n)
    {
        for (var t = model.GetEnclosingSymbol(n.SpanStart)?.ContainingType; t != null; t = t.ContainingType)
        {
            string r = selfReason(t);
            if (r.Length > 0) return r;
        }

        return memberReason(n);
    }

    /// <summary>Type tự nó là UI (Drawable không qua Component), kế thừa/triển khai input/host của framework, hoặc là scaffold test.</summary>
    private string selfReason(ITypeSymbol t)
    {
        if (typeSelfCache.TryGetValue(t, out string? cached)) return cached;

        string reason = "";
        if (inNamespace(t.ContainingNamespace, ["osu.Game.Tests"])) reason = "TestInfra";
        else
        {
            bool drawable = false, component = false, input = false, host = false;
            INamedTypeSymbol? drawableType = null;
            for (var b = t; b != null; b = b.BaseType)
            {
                string name = b.OriginalDefinition.ToDisplayString();
                if (name == "osu.Framework.Graphics.Drawable") drawableType = b as INamedTypeSymbol;
                drawable |= name == "osu.Framework.Graphics.Drawable";
                component |= name == "osu.Framework.Graphics.Component";
                host |= name == "osu.Framework.Game" || inNamespace(b.ContainingNamespace, ["osu.Framework.Platform"]);
                input |= inNamespace(b.ContainingNamespace, ["osu.Framework.Input"]);
            }

            // interface input mà chính Drawable đã triển khai thì mọi Component đều có, không phải tín hiệu riêng của type này.
            var inherited = drawableType?.AllInterfaces ?? [];
            input |= t.AllInterfaces.Any(i => inNamespace(i.ContainingNamespace, ["osu.Framework.Input"]) && !inherited.Contains(i, SymbolEqualityComparer.Default));
            reason = drawable && !component ? "UI" : host ? "Host/IO" : input ? "Input" : "";

            // Loại thêm: type không dựng/kiểm thử được bằng CFT thuần nếu thiếu thứ ngoài.
            if (reason.Length == 0 && isRealmBacked(t)) reason = "Database/Online";
            if (reason.Length == 0 && declaresDi(t)) reason = "DI";
            if (reason.Length == 0) reason = constructorReason(t);
        }

        return typeSelfCache[t] = reason;
    }

    private static bool isRealmBacked(ITypeSymbol t)
    {
        for (var b = t; b != null; b = b.BaseType)
            if (inNamespace(b.ContainingNamespace, ["Realms"])) return true;

        return t.AllInterfaces.Any(i => inNamespace(i.ContainingNamespace, ["Realms"]));
    }

    /// <summary>Type có member [Resolved]/[BackgroundDependencyLoader]: cần DI container của framework mới dựng/chạy được.</summary>
    private static bool declaresDi(ITypeSymbol t)
        => t.GetMembers().Any(m => m.GetAttributes().Any(a => a.AttributeClass?.Name is "ResolvedAttribute" or "BackgroundDependencyLoaderAttribute"));

    /// <summary>Mọi constructor tường minh đều đòi tham số là thứ ngoài (host/IO/online/DI…) thì không dựng được trong unit test thuần.</summary>
    private string constructorReason(ITypeSymbol t)
    {
        if (t is not INamedTypeSymbol { IsStatic: false } named) return "";

        var ctors = named.InstanceConstructors.Where(c => !c.IsImplicitlyDeclared).ToList();
        if (ctors.Count == 0) return "";

        string first = "";
        foreach (var c in ctors)
        {
            string reason = c.Parameters.Select(p => paramReason(p.Type)).FirstOrDefault(x => x.Length > 0) ?? "";
            if (reason.Length == 0) return "";
            if (first.Length == 0) first = reason;
        }

        return first;
    }

    private string paramReason(ITypeSymbol p)
    {
        string r = typeReason(p);
        if (r.Length > 0) return r;
        return p is INamedTypeSymbol && declaresDi(p) ? "DI" : "";
    }

    /// <summary>Member (hoặc type tham chiếu trong member) phụ thuộc: DI, UI, Input, Host/IO, Database/Online.</summary>
    private string memberReason(SyntaxNode n)
    {
        SyntaxNode? member = null;
        foreach (var a in n.Ancestors())
        {
            if (a is BaseTypeDeclarationSyntax) break;
            if (a is MemberDeclarationSyntax) member = a;
        }

        if (member == null) return "";
        if (memberCache.TryGetValue(member, out string? cached)) return cached;

        string reason = hasDiAttribute(member) ? "DI" : "";
        if (reason.Length == 0)
        {
            foreach (var id in member.DescendantNodes().OfType<SimpleNameSyntax>())
            {
                reason = symbolReason(model.GetSymbolInfo(id).Symbol);
                if (reason.Length > 0) break;
            }
        }

        return memberCache[member] = reason;
    }

    private bool hasDiAttribute(SyntaxNode member)
        => member is MemberDeclarationSyntax m && m.AttributeLists.SelectMany(l => l.Attributes)
                                                    .Any(a => a.Name.ToString().Split('.')[^1] is "BackgroundDependencyLoader" or "Resolved" or "Cached" or "BackgroundDependencyLoaderAttribute" or "ResolvedAttribute" or "CachedAttribute");

    private string symbolReason(ISymbol? s)
    {
        if (s == null) return "";
        if (s.GetAttributes().Any(a => a.AttributeClass?.Name is "ResolvedAttribute" or "BackgroundDependencyLoaderAttribute")) return "DI";

        ITypeSymbol? type = s switch
        {
            ITypeSymbol t => t,
            ILocalSymbol l => l.Type,
            IParameterSymbol p => p.Type,
            IFieldSymbol f => f.Type,
            IPropertySymbol pr => pr.Type,
            IMethodSymbol { MethodKind: MethodKind.Constructor } c => c.ContainingType,
            IMethodSymbol me => me.ContainingType,
            _ => null,
        };

        if (type == null) return "";
        string r = typeReason(type);
        if (r.Length > 0) return r;

        // member của type ngoài (vd. host.Window) cũng được tính qua ContainingType.
        return s is not ITypeSymbol && s.ContainingType != null ? typeReason(s.ContainingType) : "";
    }

    private string typeReason(ITypeSymbol type)
    {
        while (type is IArrayTypeSymbol arr) type = arr.ElementType;

        if (typeDependencyCache.TryGetValue(type, out string? cached)) return cached;

        string reason = "";
        foreach (var (r, namespaces) in externalNamespaces)
        {
            if (inNamespace(type.ContainingNamespace, namespaces)) { reason = r; break; }
        }

        if (reason.Length == 0 && type is INamedTypeSymbol)
        {
            bool drawable = false, component = false;
            for (var b = type; b != null; b = b.BaseType)
            {
                string name = b.OriginalDefinition.ToDisplayString();
                drawable |= name == "osu.Framework.Graphics.Drawable";
                component |= name == "osu.Framework.Graphics.Component";
            }

            if (drawable && !component) reason = "UI";
        }

        return typeDependencyCache[type] = reason;
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
