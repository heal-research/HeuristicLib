using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace HEAL.HeuristicLib.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class OperatorAuthoringAnalyzer : DiagnosticAnalyzer
{
    public const string ConfigurationMutationDiagnosticId = "HLib0003";

    private const string ConfigurationMetadataName = "HEAL.HeuristicLib.Execution.IConfigurationNode";

    private static readonly DiagnosticDescriptor ConfigurationMutationRule = new(
        id: ConfigurationMutationDiagnosticId,
        title: "Configurations must not be mutated after construction",
        messageFormat: "Configuration member '{0}' is mutated outside construction. Store mutable execution data in an executor instead.",
        category: "Architecture",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [ConfigurationMutationRule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(startContext =>
        {
            var configurationContract = startContext.Compilation.GetTypeByMetadataName(ConfigurationMetadataName);
            startContext.RegisterSyntaxNodeAction(
                syntaxContext => AnalyzeAssignment(syntaxContext, configurationContract),
                SyntaxKind.SimpleAssignmentExpression,
                SyntaxKind.AddAssignmentExpression,
                SyntaxKind.SubtractAssignmentExpression,
                SyntaxKind.MultiplyAssignmentExpression,
                SyntaxKind.DivideAssignmentExpression,
                SyntaxKind.ModuloAssignmentExpression,
                SyntaxKind.AndAssignmentExpression,
                SyntaxKind.ExclusiveOrAssignmentExpression,
                SyntaxKind.OrAssignmentExpression,
                SyntaxKind.LeftShiftAssignmentExpression,
                SyntaxKind.RightShiftAssignmentExpression,
                SyntaxKind.CoalesceAssignmentExpression);

            startContext.RegisterSyntaxNodeAction(
                syntaxContext => AnalyzeIncrementOrDecrement(syntaxContext, configurationContract),
                SyntaxKind.PreIncrementExpression,
                SyntaxKind.PreDecrementExpression,
                SyntaxKind.PostIncrementExpression,
                SyntaxKind.PostDecrementExpression);
        });
    }

    private static bool IsOrImplements(ITypeSymbol type, INamedTypeSymbol candidate)
    {
        if (SymbolEqualityComparer.Default.Equals(type, candidate))
        {
            return true;
        }

        if (type is not INamedTypeSymbol namedType)
        {
            return false;
        }

        for (var current = namedType.BaseType; current is not null; current = current.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(current, candidate))
            {
                return true;
            }
        }

        return namedType.AllInterfaces.Any(@interface => SymbolEqualityComparer.Default.Equals(@interface, candidate));
    }

    private static void AnalyzeAssignment(SyntaxNodeAnalysisContext context, INamedTypeSymbol? configurationContract)
    {
        if (context.Node is AssignmentExpressionSyntax assignment && !SetsMemberOfNewInstance(assignment))
        {
            AnalyzeMutationTarget(context, assignment.Left, configurationContract);
        }
    }

    // A top-level assignment in an object or with initializer sets a member of the instance that expression creates.
    // Array initializers and nested member initializers can still reach the current instance.
    private static bool SetsMemberOfNewInstance(AssignmentExpressionSyntax assignment) =>
        assignment.Parent is InitializerExpressionSyntax { Parent: BaseObjectCreationExpressionSyntax or WithExpressionSyntax };

    private static void AnalyzeIncrementOrDecrement(SyntaxNodeAnalysisContext context, INamedTypeSymbol? configurationContract)
    {
        var operand = context.Node switch
        {
            PrefixUnaryExpressionSyntax prefix => prefix.Operand,
            PostfixUnaryExpressionSyntax postfix => postfix.Operand,
            _ => null
        };

        if (operand is not null)
        {
            AnalyzeMutationTarget(context, operand, configurationContract);
        }
    }

    // Every configuration is reusable settings: operators, algorithms and consumer-defined roles alike.
    private static void AnalyzeMutationTarget(SyntaxNodeAnalysisContext context, ExpressionSyntax target, INamedTypeSymbol? configurationContract)
    {
        var enclosingSymbol = context.SemanticModel.GetEnclosingSymbol(target.SpanStart, context.CancellationToken);
        if (enclosingSymbol is not IMethodSymbol method
            || method.MethodKind is MethodKind.Constructor or MethodKind.StaticConstructor or MethodKind.PropertySet or MethodKind.EventAdd or MethodKind.EventRemove
            || method.ContainingType is null
            || configurationContract is null
            || !IsOrImplements(method.ContainingType, configurationContract))
        {
            return;
        }

        var targetSymbol = context.SemanticModel.GetSymbolInfo(target, context.CancellationToken).Symbol;
        if (targetSymbol is not IFieldSymbol and not IPropertySymbol
            || targetSymbol.IsStatic
            || !IsDeclaredOnCurrentType(targetSymbol, method.ContainingType)
            || !IsCurrentInstanceAccess(target))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            ConfigurationMutationRule,
            target.GetLocation(),
            targetSymbol.Name));
    }

    private static bool IsDeclaredOnCurrentType(ISymbol member, INamedTypeSymbol containingType)
    {
        for (var current = containingType; current is not null; current = current.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(member.ContainingType, current))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsCurrentInstanceAccess(ExpressionSyntax expression) => expression switch
    {
        IdentifierNameSyntax => true,
        MemberAccessExpressionSyntax memberAccess
        => memberAccess.Expression is ThisExpressionSyntax or BaseExpressionSyntax,
        _ => false
    };
}
