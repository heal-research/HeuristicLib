using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace HEAL.HeuristicLib.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ExecutorFactoryAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "HLib0001";

    private const string ExecutionNamespace = "HEAL.HeuristicLib.Execution";

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "Resolve child configurations through the scope",
        messageFormat: "Do not prepare a child executor factory directly. Use ResolutionScope.Resolve(...) to preserve its wrappers and execution state.",
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
            || target.Name != "CreateExecutorFactory"
            || target.IsStatic
            || target.Parameters.Length != 0
            || !IsExecutionType(target.ReturnType, "ExecutorFactory")
            || !HasContract(target.ContainingType, "IConfigurationNode"))
            return;

        // A bridge delegates to its own preparation hook. A child of the same type is still a different receiver.
        if (IsSelfReceiver(invocation.Expression) || !IsConstructionContext(context, invocation) || IsResolutionPreparation(context, invocation))
            return;

        context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.GetLocation()));
    }

    private static bool IsConstructionContext(SyntaxNodeAnalysisContext context, InvocationExpressionSyntax invocation)
    {
        for (var symbol = context.SemanticModel.GetEnclosingSymbol(invocation.SpanStart, context.CancellationToken);
             symbol is not null; symbol = symbol.ContainingSymbol)
        {
            if (symbol is IMethodSymbol method && (IsExecutionType(method.ReturnType, "ExecutorFactory", "WrapperExecutorFactory", "CompositeExecutorFactory") || IsWrapperConstructor(method.ReturnType)
                || method.Parameters.Any(static parameter => IsExecutionType(parameter.Type, "ResolutionScope"))))
                return true;
            if (symbol is INamedTypeSymbol type && HasContract(type, "IExecutor"))
                return true;
        }
        return false;
    }

    private static bool IsResolutionPreparation(SyntaxNodeAnalysisContext context, InvocationExpressionSyntax invocation)
    {
        if (invocation.Expression is not MemberAccessExpressionSyntax member
            || invocation.Ancestors().OfType<LambdaExpressionSyntax>().FirstOrDefault() is not { Parent: ArgumentSyntax argument } lambda
            || context.SemanticModel.GetOperation(argument, context.CancellationToken) is not IArgumentOperation preparation
            || preparation.Parameter?.Name != "createExecutorFactory"
            || preparation.Parent is not IInvocationOperation resolution
            || !IsExecutionType(resolution.TargetMethod.ContainingType, "ResolutionScope")
            || context.SemanticModel.GetOperation(lambda, context.CancellationToken) is not IAnonymousFunctionOperation callback)
            return false;
        var receiver = context.SemanticModel.GetSymbolInfo(member.Expression, context.CancellationToken).Symbol;
        return callback.Symbol.Parameters.Length == 1
            && SymbolEqualityComparer.Default.Equals(callback.Symbol.Parameters[0], receiver);
    }

    private static bool IsWrapperConstructor(ITypeSymbol type) =>
        type is INamedTypeSymbol { Name: "Func", TypeArguments.Length: 2 } function
        && function.ContainingNamespace.ToDisplayString() == "System"
        && SymbolEqualityComparer.Default.Equals(function.TypeArguments[0], function.TypeArguments[1])
        && function.TypeArguments[0] is INamedTypeSymbol execution && HasContract(execution, "IExecutor");

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
