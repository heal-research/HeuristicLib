using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace HEAL.HeuristicLib.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class OperatorAuthoringAnalyzer : DiagnosticAnalyzer
{
    public const string StatefulStateDiagnosticId = "HLib0002";
    public const string ConfigurationMutationDiagnosticId = "HLib0003";

    private const string OperatorMetadataName = "HEAL.HeuristicLib.Operators.IOperator";
    private const string CreateInitialStateMethodName = "CreateInitialState";

    private static readonly DiagnosticDescriptor StatefulStateRule = new(
        id: StatefulStateDiagnosticId,
        title: "Stateful operator state must not contain execution graph dependencies",
        messageFormat: "State member '{0}' exposes execution graph dependency '{1}'. Use the explicit execution instance operator path instead.",
        category: "Architecture",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor ConfigurationMutationRule = new(
        id: ConfigurationMutationDiagnosticId,
        title: "Operator configurations must not be mutated during operation logic",
        messageFormat: "Operator configuration member '{0}' is mutated during operation logic. Store mutable execution data in state or an explicit execution instance instead.",
        category: "Architecture",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [StatefulStateRule, ConfigurationMutationRule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(startContext =>
        {
            var forbiddenTypes = GetForbiddenTypes(startContext.Compilation);
            var operatorContract = startContext.Compilation.GetTypeByMetadataName(OperatorMetadataName);
            startContext.RegisterSymbolAction(
                symbolContext => AnalyzeStatefulOperator(symbolContext, forbiddenTypes, operatorContract),
                SymbolKind.NamedType);

            startContext.RegisterSyntaxNodeAction(
                syntaxContext => AnalyzeAssignment(syntaxContext, operatorContract),
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
                syntaxContext => AnalyzeIncrementOrDecrement(syntaxContext, operatorContract),
                SyntaxKind.PreIncrementExpression,
                SyntaxKind.PreDecrementExpression,
                SyntaxKind.PostIncrementExpression,
                SyntaxKind.PostDecrementExpression);
        });
    }

    private static ImmutableArray<INamedTypeSymbol> GetForbiddenTypes(Compilation compilation)
    {
        var metadataNames = new[]
        {
            "HEAL.HeuristicLib.Execution.IExecutionInstance",
            "HEAL.HeuristicLib.Execution.IExecutionInstanceResolvable",
            "HEAL.HeuristicLib.Execution.ExecutionInstanceRegistry",
            "HEAL.HeuristicLib.Operators.IOperator"
        };

        return [.. metadataNames
            .Select(compilation.GetTypeByMetadataName)
            .Where(static type => type is not null)
            .Cast<INamedTypeSymbol>()];
    }

    private static void AnalyzeStatefulOperator(SymbolAnalysisContext context, ImmutableArray<INamedTypeSymbol> forbiddenTypes, INamedTypeSymbol? operatorContract)
    {
        if (context.Symbol is not INamedTypeSymbol operatorType || operatorContract is null || !TryGetOperatorStateType(operatorType, operatorContract, out var stateType))
        {
            return;
        }

        if (stateType.TypeKind == TypeKind.TypeParameter)
        {
            return;
        }

        foreach (var member in GetStateMembers(stateType))
        {
            var memberType = GetMemberType(member);
            if (memberType is null)
            {
                continue;
            }

            if (!TryFindForbiddenType(memberType, forbiddenTypes, new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default), out var forbiddenType))
            {
                continue;
            }

            var location = member.Locations.FirstOrDefault(static candidate => candidate.IsInSource) ?? operatorType.Locations.FirstOrDefault();
            if (location is null)
            {
                continue;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                StatefulStateRule,
                location,
                member.Name,
                forbiddenType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
        }
    }

    private static bool TryGetOperatorStateType(INamedTypeSymbol type, INamedTypeSymbol operatorContract, out ITypeSymbol stateType)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            if (!IsOrImplements(current, operatorContract))
            {
                continue;
            }

            var stateFactory = current.OriginalDefinition.GetMembers(CreateInitialStateMethodName)
                .OfType<IMethodSymbol>()
                .FirstOrDefault(static method => !method.IsStatic && method.Parameters.Length == 0 && method.ReturnType is ITypeParameterSymbol);
            if (stateFactory?.ReturnType is not ITypeParameterSymbol stateParameter
                || stateParameter.TypeParameterKind != TypeParameterKind.Type
                || !SymbolEqualityComparer.Default.Equals(stateParameter.ContainingSymbol, current.OriginalDefinition)
                || stateParameter.Ordinal >= current.TypeArguments.Length)
            {
                continue;
            }

            stateType = current.TypeArguments[stateParameter.Ordinal];
            return true;
        }

        stateType = null!;
        return false;
    }

    private static IEnumerable<ISymbol> GetStateMembers(ITypeSymbol stateType)
    {
        if (stateType is not INamedTypeSymbol namedState)
        {
            yield break;
        }

        for (var current = namedState; current is not null; current = current.BaseType)
        {
            if (!current.Locations.Any(static location => location.IsInSource))
            {
                continue;
            }

            foreach (var member in current.GetMembers().Where(static member => member switch
                     {
                         IFieldSymbol field => !field.IsStatic && !field.IsImplicitlyDeclared,
                         IPropertySymbol property => !property.IsStatic && !property.IsIndexer,
                         _ => false
                     }))
            {
                yield return member;
            }
        }
    }

    private static ITypeSymbol? GetMemberType(ISymbol member) => member switch
    {
        IFieldSymbol field => field.Type,
        IPropertySymbol property => property.Type,
        _ => null
    };

    private static bool TryFindForbiddenType(ITypeSymbol type, ImmutableArray<INamedTypeSymbol> forbiddenTypes, HashSet<ITypeSymbol> visited, out INamedTypeSymbol forbiddenType)
    {
        var matchingForbiddenType = forbiddenTypes.FirstOrDefault(candidate => IsOrImplements(type, candidate));
        if (matchingForbiddenType is not null)
        {
            forbiddenType = matchingForbiddenType;
            return true;
        }

        if (!visited.Add(type))
        {
            forbiddenType = null!;
            return false;
        }

        if (type is IArrayTypeSymbol arrayType && TryFindForbiddenType(arrayType.ElementType, forbiddenTypes, visited, out forbiddenType))
        {
            return true;
        }

        if (type is not INamedTypeSymbol namedType)
        {
            forbiddenType = null!;
            return false;
        }

        foreach (var typeArgument in namedType.TypeArguments)
        {
            if (TryFindForbiddenType(typeArgument, forbiddenTypes, visited, out forbiddenType))
            {
                return true;
            }
        }

        if (!namedType.Locations.Any(static location => location.IsInSource))
        {
            forbiddenType = null!;
            return false;
        }

        foreach (var member in GetStateMembers(namedType))
        {
            var memberType = GetMemberType(member);
            if (memberType is not null && TryFindForbiddenType(memberType, forbiddenTypes, visited, out forbiddenType))
            {
                return true;
            }
        }

        forbiddenType = null!;
        return false;
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

    private static void AnalyzeAssignment(SyntaxNodeAnalysisContext context, INamedTypeSymbol? operatorContract)
    {
        if (context.Node is AssignmentExpressionSyntax assignment)
        {
            AnalyzeMutationTarget(context, assignment.Left, operatorContract);
        }
    }

    private static void AnalyzeIncrementOrDecrement(SyntaxNodeAnalysisContext context, INamedTypeSymbol? operatorContract)
    {
        var operand = context.Node switch
        {
            PrefixUnaryExpressionSyntax prefix => prefix.Operand,
            PostfixUnaryExpressionSyntax postfix => postfix.Operand,
            _ => null
        };

        if (operand is not null)
        {
            AnalyzeMutationTarget(context, operand, operatorContract);
        }
    }

    private static void AnalyzeMutationTarget(SyntaxNodeAnalysisContext context, ExpressionSyntax target, INamedTypeSymbol? operatorContract)
    {
        var enclosingSymbol = context.SemanticModel.GetEnclosingSymbol(target.SpanStart, context.CancellationToken);
        if (enclosingSymbol is not IMethodSymbol method
            || method.MethodKind is MethodKind.Constructor or MethodKind.StaticConstructor or MethodKind.PropertySet or MethodKind.EventAdd or MethodKind.EventRemove
            || method.ContainingType is null
            || operatorContract is null
            || !IsOrImplements(method.ContainingType, operatorContract))
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
