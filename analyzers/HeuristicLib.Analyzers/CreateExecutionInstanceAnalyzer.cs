using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace HEAL.HeuristicLib.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class CreateExecutionInstanceAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "HLib0001";

    private const string CreateExecutionInstanceMethodName = "CreateExecutionInstance";
    private const string ExecutionInstanceRegistryTypeName = "ExecutionInstanceRegistry";

    private static readonly DiagnosticDescriptor Rule = new(
      id: DiagnosticId,
      title: "Do not call CreateExecutionInstance directly",
      messageFormat: "Do not call CreateExecutionInstance directly inside CreateExecutionInstance. Use ExecutionInstanceRegistry.Resolve(...) instead.",
      category: "Architecture",
      defaultSeverity: DiagnosticSeverity.Error,
      isEnabledByDefault: true
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not InvocationExpressionSyntax invocation)
            return;

        var containingMethodSyntax = invocation.FirstAncestorOrSelf<MethodDeclarationSyntax>();
        if (containingMethodSyntax is null)
            return;

        var containingMethodSymbol = context.SemanticModel.GetDeclaredSymbol(containingMethodSyntax, context.CancellationToken);
        if (containingMethodSymbol is null)
            return;

        if (!IsCreationMethod(containingMethodSymbol))
            return;

        // Only if this CreateExecutionInstance has an ExecutionInstanceRegistry parameter (name doesn't matter).
        if (!containingMethodSymbol.Parameters.Any(IsExecutionInstanceRegistryParameter))
            return;

        var symbolInfo = context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken);
        var targetMethod =
          symbolInfo.Symbol as IMethodSymbol
          ?? symbolInfo.CandidateSymbols.OfType<IMethodSymbol>().FirstOrDefault();

        if (targetMethod is null)
            return;

        if (!string.Equals(targetMethod.Name, CreateExecutionInstanceMethodName, StringComparison.Ordinal))
            return;

        // Type parameters are not value parameters, so a generic creation method matches this shape too.
        if (targetMethod.Parameters.Length != 1 || !IsExecutionInstanceRegistryParameter(targetMethod.Parameters[0]))
            return;

        // Reaching its own overload through this, base, or an implicit receiver is how a bridge works, not a bypass.
        // The receiver is what says so: comparing containing types instead would both miss a base call, whose target
        // is declared on the base, and wave through a child that happens to share its holder's type.
        if (IsSelfReceiver(invocation.Expression))
            return;

        context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.GetLocation()));
    }

    /// <remarks>
    /// An explicit interface implementation carries the qualified name, so matching only <see cref="ISymbol.Name"/>
    /// would skip every bridge written that way.
    /// </remarks>
    private static bool IsCreationMethod(IMethodSymbol method)
    {
        return string.Equals(method.Name, CreateExecutionInstanceMethodName, StringComparison.Ordinal)
          || method.ExplicitInterfaceImplementations.Any(
               static implemented => string.Equals(implemented.Name, CreateExecutionInstanceMethodName, StringComparison.Ordinal));
    }

    private static bool IsSelfReceiver(ExpressionSyntax invoked)
    {
        return invoked switch
        {
            MemberAccessExpressionSyntax memberAccess => memberAccess.Expression is ThisExpressionSyntax or BaseExpressionSyntax,
            IdentifierNameSyntax or GenericNameSyntax => true,
            _ => false
        };
    }

    private static bool IsExecutionInstanceRegistryParameter(IParameterSymbol parameter)
    {
        return string.Equals(parameter.Type.Name, ExecutionInstanceRegistryTypeName, StringComparison.Ordinal);
    }
}
