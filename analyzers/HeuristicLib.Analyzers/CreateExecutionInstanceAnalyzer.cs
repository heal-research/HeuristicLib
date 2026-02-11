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

    if (!string.Equals(containingMethodSymbol.Name, CreateExecutionInstanceMethodName, StringComparison.Ordinal))
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

    // Must be calling either CreateExecutionInstance() or CreateExecutionInstance(ExecutionInstanceRegistry)
    if (!(targetMethod.Parameters.Length == 0
        || (targetMethod.Parameters.Length == 1 && IsExecutionInstanceRegistryParameter(targetMethod.Parameters[0]))))
      return;

    // Ignore self calls (including base/this)
    if (SymbolEqualityComparer.Default.Equals(targetMethod.ContainingType, containingMethodSymbol.ContainingType))
      return;

    context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.GetLocation()));
  }

  private static bool IsExecutionInstanceRegistryParameter(IParameterSymbol parameter)
  {
    return string.Equals(parameter.Type.Name, ExecutionInstanceRegistryTypeName, StringComparison.Ordinal);
  }
}
