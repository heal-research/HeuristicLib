using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace HEAL.HeuristicLib.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ExecutionFactoryAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "HLib0001";

    private const string ExecutionNamespace = "HEAL.HeuristicLib.Execution";

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "Resolve child configurations through the scope",
        messageFormat: "Do not prepare a child execution factory directly. Use ResolutionScope.Resolve(...) to preserve its wrappers and execution state.",
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
            || target.Name != "CreateExecutionFactory"
            || target.IsStatic
            || target.Parameters.Length != 0
            || !IsExecutionType(target.ReturnType, "ExecutionFactory")
            || !HasContract(target.ContainingType, "IConfigurationNode"))
            return;

        // A bridge delegates to its own preparation hook. A child of the same type is still a different receiver.
        if (IsSelfReceiver(invocation.Expression) || !IsConstructionContext(context, invocation) || IsPreparationAdapter(context, invocation))
            return;

        context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.GetLocation()));
    }

    private static bool IsConstructionContext(SyntaxNodeAnalysisContext context, InvocationExpressionSyntax invocation)
    {
        for (var symbol = context.SemanticModel.GetEnclosingSymbol(invocation.SpanStart, context.CancellationToken);
             symbol is not null; symbol = symbol.ContainingSymbol)
        {
            if (symbol is IMethodSymbol method && (IsExecutionType(method.ReturnType, "ExecutionFactory", "WrapperExecutionFactory", "CompositeExecutionFactory")
                || method.Parameters.Any(static parameter => IsExecutionType(parameter.Type, "ResolutionScope"))))
                return true;
            if (symbol is INamedTypeSymbol type && HasContract(type, "IExecutionNode"))
                return true;
        }
        return false;
    }

    private static bool IsPreparationAdapter(SyntaxNodeAnalysisContext context, InvocationExpressionSyntax invocation)
    {
        if (invocation.Expression is not MemberAccessExpressionSyntax member)
            return false;

        // Resolve owns preparation when its adapter calls the hook on the adapter's own source parameter.
        // Calling some other child in that callback is still a bypass.
        foreach (var lambda in invocation.Ancestors().OfType<LambdaExpressionSyntax>())
        {
            if (lambda.Parent is not ArgumentSyntax { Parent: ArgumentListSyntax { Parent: InvocationExpressionSyntax resolve } }
                || context.SemanticModel.GetSymbolInfo(resolve, context.CancellationToken).Symbol is not IMethodSymbol method
                || method.Name is not ("Resolve" or "TryResolve")
                || !IsExecutionType(method.ContainingType, "ResolutionScope"))
                continue;

            var receiver = context.SemanticModel.GetSymbolInfo(member.Expression, context.CancellationToken).Symbol;
            if (context.SemanticModel.GetSymbolInfo(lambda, context.CancellationToken).Symbol is IMethodSymbol adapter
                && adapter.Parameters.Any(parameter => SymbolEqualityComparer.Default.Equals(parameter, receiver)))
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
        IsExecutionType(type, name) || type.AllInterfaces.Any(contract => IsExecutionType(contract, name));

    private static bool IsExecutionType(ITypeSymbol type, params string[] names) =>
        type.ContainingNamespace.ToDisplayString() == ExecutionNamespace && names.Contains(type.Name);
}
