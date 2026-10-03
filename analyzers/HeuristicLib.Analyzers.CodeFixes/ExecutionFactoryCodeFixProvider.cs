using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HEAL.HeuristicLib.Analyzers.CodeFixes;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ExecutionFactoryCodeFixProvider))]
[Shared]
public sealed class ExecutionFactoryCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => [ExecutionFactoryAnalyzer.DiagnosticId];

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var model = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null || model is null)
            return;

        foreach (var diagnostic in context.Diagnostics)
        {
            var factory = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true).FirstAncestorOrSelf<InvocationExpressionSyntax>();
            if (factory?.Expression is not MemberAccessExpressionSyntax member
                || model.GetTypeInfo(factory, context.CancellationToken).Type is not INamedTypeSymbol { Name: "ExecutionFactory", Arity: 1 } factoryType
                || factoryType.ContainingNamespace.ToDisplayString() != "HEAL.HeuristicLib.Execution")
                continue;

            ExpressionSyntax target = factory;
            while (target.Parent is ParenthesizedExpressionSyntax parentheses)
                target = parentheses;
            if (target.Parent is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Invoke" } invoke)
                target = invoke;

            ExpressionSyntax replacement;
            if (target.Parent is InvocationExpressionSyntax binding && binding.Expression == target && binding.ArgumentList.Arguments.Count == 1)
            {
                var scope = binding.ArgumentList.Arguments[0].Expression;
                if (model.GetTypeInfo(scope, context.CancellationToken).Type?.ToDisplayString() != "HEAL.HeuristicLib.Execution.ResolutionScope")
                    continue;
                replacement = CreateResolve(model, member.Expression, scope, factoryType.TypeArguments[0]);
                target = binding;
            }
            else if (target.Parent is ReturnStatementSyntax or ArrowExpressionClauseSyntax)
            {
                // A preparation hook returning its child's factory needs to resolve the child on each binding.
                var scopeName = "scope";
                var suffix = 1;
                while (model.LookupSymbols(factory.SpanStart, name: scopeName).Length != 0)
                    scopeName = "scope" + suffix++;
                replacement = SyntaxFactory.SimpleLambdaExpression(
                    SyntaxFactory.Parameter(SyntaxFactory.Identifier(scopeName)),
                    CreateResolve(model, member.Expression, SyntaxFactory.IdentifierName(scopeName), factoryType.TypeArguments[0]));
            }
            else
            {
                // A stored factory can escape or have several call sites. Do not offer a partial rewrite.
                continue;
            }

            var annotation = new SyntaxAnnotation();
            var changedRoot = root.ReplaceNode(target, replacement.WithTriviaFrom(target).WithAdditionalAnnotations(annotation));
            var changed = context.Document.WithSyntaxRoot(changedRoot);
            var changedModel = await changed.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
            if (changedModel is null || changedModel.GetDiagnostics(changedRoot.GetAnnotatedNodes(annotation).Single().Span, context.CancellationToken)
                .Any(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error))
                continue;
            context.RegisterCodeFix(CodeAction.Create(
                title: "Resolve child through the scope",
                createChangedDocument: _ => Task.FromResult(changed),
                equivalenceKey: nameof(ExecutionFactoryCodeFixProvider)), diagnostic);
        }
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

        return SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, scope.WithoutTrivia(), resolve),
            SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(source.WithoutTrivia()))));
    }
}
