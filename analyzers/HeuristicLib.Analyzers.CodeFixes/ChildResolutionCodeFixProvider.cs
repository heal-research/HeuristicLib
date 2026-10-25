using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HEAL.HeuristicLib.Analyzers.CodeFixes;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ChildResolutionCodeFixProvider))]
[Shared]
public sealed class ChildResolutionCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => [ChildResolutionAnalyzer.DiagnosticId];

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var model = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null || model is null)
            return;

        foreach (var diagnostic in context.Diagnostics)
        {
            var construction = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true).FirstAncestorOrSelf<InvocationExpressionSyntax>();
            if (construction?.Expression is MemberAccessExpressionSyntax member
                && TryResolveConstruction(model, construction, member, context.CancellationToken, out var replacement))
                await RegisterIfCompilesAsync(context, root, diagnostic, construction, replacement).ConfigureAwait(false);
        }
    }

    // A hook builds its executor directly, so the fix resolves the child through the scope it was given.
    private static bool TryResolveConstruction(SemanticModel model, InvocationExpressionSyntax construction, MemberAccessExpressionSyntax member,
        CancellationToken cancellationToken, out ExpressionSyntax replacement)
    {
        replacement = construction;
        if (construction.ArgumentList.Arguments.Count != 1
            || model.GetSymbolInfo(construction, cancellationToken).Symbol is not IMethodSymbol { Name: "CreateExecutor" } hook)
            return false;
        var scope = construction.ArgumentList.Arguments[0].Expression;
        if (model.GetTypeInfo(scope, cancellationToken).Type is not INamedTypeSymbol scopeType || !ExecutionTypes.Is(scopeType, "ResolutionScope"))
            return false;

        // A leaf hook names its role. A run-generic operator hook returns its executor untyped, as ResolveOperator does; a
        // run-generic algorithm hook returns the executor its typed scope's Resolve infers.
        replacement = !scopeType.IsGenericType
            ? CreateResolve(model, member.Expression, scope, hook.ReturnType)
            : CreateInvocation(scope, SyntaxFactory.IdentifierName(ExecutionTypes.Is(hook.ReturnType, "IExecutor") ? "ResolveOperator" : "Resolve"), member.Expression);
        return true;
    }

    private static async Task RegisterIfCompilesAsync(CodeFixContext context, SyntaxNode root, Diagnostic diagnostic, ExpressionSyntax target, ExpressionSyntax replacement)
    {
        var annotation = new SyntaxAnnotation();
        var changedRoot = root.ReplaceNode(target, replacement.WithTriviaFrom(target).WithAdditionalAnnotations(annotation));
        var changed = context.Document.WithSyntaxRoot(changedRoot);
        var changedModel = await changed.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (changedModel is null || changedModel.GetDiagnostics(changedRoot.GetAnnotatedNodes(annotation).Single().Span, context.CancellationToken)
            .Any(static error => error.Severity == DiagnosticSeverity.Error))
            return;
        context.RegisterCodeFix(CodeAction.Create(
            title: "Resolve child through the scope",
            createChangedDocument: _ => Task.FromResult(changed),
            equivalenceKey: nameof(ChildResolutionCodeFixProvider)), diagnostic);
    }

    private static InvocationExpressionSyntax CreateResolve(SemanticModel model, ExpressionSyntax source, ExpressionSyntax scope, ITypeSymbol execution)
    {
        // The common configuration contract supports inference. Role contracts need the execution's run arguments.
        var sourceType = model.GetTypeInfo(source).Type as INamedTypeSymbol;
        var commonContract = model.Compilation.GetTypeByMetadataName("HEAL.HeuristicLib.Execution.IConfigurationNode`1");
        var inferArguments = sourceType is not null && (SymbolEqualityComparer.Default.Equals(sourceType.OriginalDefinition, commonContract)
            || sourceType.AllInterfaces.Any(contract => SymbolEqualityComparer.Default.Equals(contract.OriginalDefinition, commonContract)));
        SimpleNameSyntax resolve = SyntaxFactory.IdentifierName("Resolve");
        if (!inferArguments && execution is INamedTypeSymbol { IsGenericType: true } role)
            resolve = SyntaxFactory.GenericName(SyntaxFactory.Identifier("Resolve"), SyntaxFactory.TypeArgumentList(
                SyntaxFactory.SeparatedList(role.TypeArguments.Select(argument => SyntaxFactory.ParseTypeName(
                    argument.ToMinimalDisplayString(model, source.SpanStart))))));

        return CreateInvocation(scope, resolve, source);
    }

    private static InvocationExpressionSyntax CreateInvocation(ExpressionSyntax scope, SimpleNameSyntax method, ExpressionSyntax source) =>
        SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, scope.WithoutTrivia(), method),
            SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(source.WithoutTrivia()))));
}
