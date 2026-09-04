using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HEAL.HeuristicLib.Analyzers.CodeFixes;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CreateExecutionInstanceCodeFixProvider))]
[Shared]
public sealed class CreateExecutionInstanceCodeFixProvider : CodeFixProvider
{
    private const string EquivalenceKey = nameof(CreateExecutionInstanceCodeFixProvider);
    private const string ExecutionInstanceRegistryTypeName = "ExecutionInstanceRegistry";

    public override ImmutableArray<string> FixableDiagnosticIds
      => [CreateExecutionInstanceAnalyzer.DiagnosticId];

    public override FixAllProvider GetFixAllProvider()
      => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null)
            return;

        var diagnostic = context.Diagnostics.FirstOrDefault();
        if (diagnostic is null)
            return;

        var invocation = root
          .FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true)
          .FirstAncestorOrSelf<InvocationExpressionSyntax>();

        if (invocation is null)
            return;

        context.RegisterCodeFix(
          CodeAction.Create(
            title: "Replace with instanceRegistry.Resolve(...)",
            createChangedDocument: c => ReplaceWithResolveAsync(context.Document, invocation, c),
            equivalenceKey: EquivalenceKey),
          diagnostic);
    }

    private static async Task<Document> ReplaceWithResolveAsync(
      Document document,
      InvocationExpressionSyntax invocation,
      CancellationToken cancellationToken)
    {
        // The analyzer reports on CreateExecutionInstance calls, but keep the fixer defensive.
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            return document;

        if (!string.Equals(memberAccess.Name.Identifier.ValueText, "CreateExecutionInstance", StringComparison.Ordinal))
            return document;

        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        if (semanticModel is null)
            return document;

        var methodDeclaration = invocation.FirstAncestorOrSelf<MethodDeclarationSyntax>();
        if (methodDeclaration is null)
            return document;

        var containingMethodSymbol = semanticModel.GetDeclaredSymbol(methodDeclaration, cancellationToken);
        if (containingMethodSymbol is null)
            return document;

        var registryParamName = containingMethodSymbol.Parameters
          .FirstOrDefault(p => string.Equals(p.Type.Name, ExecutionInstanceRegistryTypeName, StringComparison.Ordinal))
          ?.Name;

        if (string.IsNullOrWhiteSpace(registryParamName))
            return document;

        // Resolve names the instance type's arguments, and none of them is inferable from the operator alone, so the
        // replacement has to spell them. The bypassed call already produces that instance type, which is where they
        // come from — this serves the bound and the generic creation shapes alike, and the four argument roles too.
        if (semanticModel.GetTypeInfo(invocation, cancellationToken).Type is not INamedTypeSymbol { IsGenericType: true } instanceType)
            return document;

        var typeArguments = SyntaxFactory.TypeArgumentList(
          SyntaxFactory.SeparatedList(
            instanceType.TypeArguments.Select(
              typeArgument => SyntaxFactory.ParseTypeName(
                typeArgument.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)))));

        // Replace with: <registryParam>.Resolve<...>(<expr>)
        var instanceExpression = memberAccess.Expression.WithoutTrivia();

        var newInvocation = SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
              SyntaxKind.SimpleMemberAccessExpression,
              SyntaxFactory.IdentifierName(registryParamName!),
              SyntaxFactory.GenericName(SyntaxFactory.Identifier("Resolve")).WithTypeArgumentList(typeArguments)))
          .WithArgumentList(
            SyntaxFactory.ArgumentList(
              SyntaxFactory.SingletonSeparatedList(
                SyntaxFactory.Argument(instanceExpression))))
          .WithTriviaFrom(invocation);

        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null)
            return document;

        return document.WithSyntaxRoot(root.ReplaceNode(invocation, newInvocation));
    }
}
