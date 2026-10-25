using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace HEAL.HeuristicLib.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ChildResolutionAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "HLib0001";

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "Resolve child configurations through the scope",
        messageFormat: "Do not create a child's executor directly. Resolve the child through the scope to preserve its advice and scoped state.",
        category: "Architecture",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        if (context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol is not IMethodSymbol target
            || target.IsStatic
            || !IsConstructionHook(target)
            || !HasContract(target.ContainingType, "IConfigurationNode"))
            return;

        // A bridge delegates to its own hook. A child of the same type is still a different receiver.
        if (IsSelfReceiver(invocation.Expression) || !IsConstructionContext(context, invocation))
            return;

        context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.GetLocation()));
    }

    // Every configuration builds its executor in CreateExecutor, from the scope it receives.
    private static bool IsConstructionHook(IMethodSymbol target) =>
        target is { Name: "CreateExecutor", Parameters.Length: 1 }
        && ExecutionTypes.Is(target.Parameters[0].Type, "ResolutionScope")
        && IsExecutorType(target.ReturnType);

    private static bool IsExecutorType(ITypeSymbol type) => type switch
    {
        INamedTypeSymbol named => HasContract(named, "IExecutor"),
        ITypeParameterSymbol parameter => parameter.ConstraintTypes.Any(IsExecutorType),
        _ => false
    };

    private static bool IsConstructionContext(SyntaxNodeAnalysisContext context, InvocationExpressionSyntax invocation)
    {
        for (var symbol = context.SemanticModel.GetEnclosingSymbol(invocation.SpanStart, context.CancellationToken);
             symbol is not null; symbol = symbol.ContainingSymbol)
        {
            if (symbol is IMethodSymbol method && method.Parameters.Any(static parameter => ExecutionTypes.Is(parameter.Type, "ResolutionScope")))
                return true;
            if (symbol is INamedTypeSymbol type && HasContract(type, "IExecutor"))
                return true;
        }
        return false;
    }

    private static bool IsSelfReceiver(ExpressionSyntax expression)
    {
        if (expression is IdentifierNameSyntax or GenericNameSyntax)
            return true;
        if (expression is not MemberAccessExpressionSyntax member)
            return false;
        var receiver = member.Expression;
        while (receiver is ParenthesizedExpressionSyntax or CastExpressionSyntax)
            receiver = receiver is ParenthesizedExpressionSyntax parentheses ? parentheses.Expression : ((CastExpressionSyntax)receiver).Expression;
        return receiver is ThisExpressionSyntax or BaseExpressionSyntax;
    }

    private static bool HasContract(INamedTypeSymbol type, string name) =>
        ExecutionTypes.Is(type, name) || type.AllInterfaces.Any(contract => ExecutionTypes.Is(contract, name));
}
