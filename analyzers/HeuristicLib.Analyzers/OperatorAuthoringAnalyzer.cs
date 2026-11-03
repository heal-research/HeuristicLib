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
    public const string ResourceGraphDiagnosticId = "HLib0002";
    public const string ConfigurationMutationDiagnosticId = "HLib0003";

    private const string ConfigurationMetadataName = "HEAL.HeuristicLib.Execution.IConfigurationNode";
    private const string ResourceHandleMetadataName = "HEAL.HeuristicLib.Execution.ResourceHandle`1";

    private static readonly DiagnosticDescriptor ResourceGraphRule = new(
        id: ResourceGraphDiagnosticId,
        title: "Shared resources must not contain execution graph dependencies",
        messageFormat: "Resource '{0}' exposes execution graph dependency '{1}'. Keep contextual dependencies in the executor instead.",
        category: "Architecture",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor ObservationPointOwnerRule = new(
        id: ResourceGraphDiagnosticId,
        title: "A configuration must resolve only its own observation points",
        messageFormat: "Observation point '{0}' is not declared by this configuration. Resolve only this configuration's own observation points.",
        category: "Architecture",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor ConfigurationMutationRule = new(
        id: ConfigurationMutationDiagnosticId,
        title: "Configurations must not be mutated after construction",
        messageFormat: "Configuration member '{0}' is mutated outside construction. Store mutable execution data in an executor instead.",
        category: "Architecture",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [ResourceGraphRule, ObservationPointOwnerRule, ConfigurationMutationRule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(startContext =>
        {
            var forbiddenTypes = GetForbiddenTypes(startContext.Compilation);
            var resourceHandle = startContext.Compilation.GetTypeByMetadataName(ResourceHandleMetadataName);
            var configurationContract = startContext.Compilation.GetTypeByMetadataName(ConfigurationMetadataName);
            var observationPoint = startContext.Compilation.GetTypeByMetadataName("HEAL.HeuristicLib.Inspection.ObservationPoint`1");
            startContext.RegisterSymbolAction(
                symbolContext => AnalyzeResourceHandle(symbolContext, forbiddenTypes, resourceHandle),
                SymbolKind.NamedType);

            startContext.RegisterSyntaxNodeAction(
                syntaxContext => AnalyzeResourceRequest(syntaxContext, forbiddenTypes),
                SyntaxKind.InvocationExpression);

            startContext.RegisterSyntaxNodeAction(
                syntaxContext => AnalyzeObservationPointResolution(syntaxContext, configurationContract, observationPoint),
                SyntaxKind.InvocationExpression);

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

    private static ImmutableArray<INamedTypeSymbol> GetForbiddenTypes(Compilation compilation)
    {
        var metadataNames = new[]
        {
            "HEAL.HeuristicLib.Execution.IExecutor",
            "HEAL.HeuristicLib.Execution.IConfigurationNode",
            "HEAL.HeuristicLib.Execution.ResolutionScope",
            "HEAL.HeuristicLib.Operators.IOperator"
        };

        return [.. metadataNames
            .Select(compilation.GetTypeByMetadataName)
            .Where(static type => type is not null)
            .Cast<INamedTypeSymbol>()];
    }

    private static void AnalyzeResourceHandle(SymbolAnalysisContext context, ImmutableArray<INamedTypeSymbol> forbiddenTypes, INamedTypeSymbol? resourceHandle)
    {
        if (context.Symbol is not INamedTypeSymbol type || resourceHandle is null)
            return;
        for (var current = type.BaseType; current is not null; current = current.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(current.OriginalDefinition, resourceHandle)
                && current.TypeArguments[0] is INamedTypeSymbol resource)
            {
                ReportForbiddenResourceMembers(context.ReportDiagnostic, type, forbiddenTypes, type.Locations.FirstOrDefault());
                ReportResource(context.ReportDiagnostic, resource, forbiddenTypes, type.Locations.FirstOrDefault());
                return;
            }
        }
    }

    private static void AnalyzeResourceRequest(SyntaxNodeAnalysisContext context, ImmutableArray<INamedTypeSymbol> forbiddenTypes)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        if (context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol is IMethodSymbol
            { Name: "Resource", IsStatic: false, TypeArguments.Length: 1 } request
            && ExecutionTypes.Is(request.ContainingType, "ResolutionScope", "RunResources", "AlgorithmRun")
            && request.TypeArguments[0] is INamedTypeSymbol resource)
            ReportResource(context.ReportDiagnostic, resource, forbiddenTypes, invocation.GetLocation());
    }

    private static void ReportResource(Action<Diagnostic> report, ITypeSymbol resource, ImmutableArray<INamedTypeSymbol> forbiddenTypes, Location? location)
    {
        var forbidden = forbiddenTypes.FirstOrDefault(candidate => IsOrImplements(resource, candidate));
        if (forbidden is not null && location is not null)
            report(Diagnostic.Create(ResourceGraphRule, location, resource.Name, forbidden.Name));
        else
            ReportForbiddenResourceMembers(report, resource, forbiddenTypes, location);
    }

    private static void AnalyzeObservationPointResolution(SyntaxNodeAnalysisContext context, INamedTypeSymbol? configurationContract, INamedTypeSymbol? observationPoint)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        if (configurationContract is null || observationPoint is null
            || context.ContainingSymbol?.ContainingType is not { } configuration
            || !IsOrImplements(configuration, configurationContract)
            || !invocation.Ancestors().OfType<MethodDeclarationSyntax>().Any(static method => method.Identifier.ValueText == "CreateExecutor")
            || context.SemanticModel.GetOperation(invocation, context.CancellationToken) is not IInvocationOperation operation
            || operation.TargetMethod.Name is not ("Resolve" or "ResolveOptional" or "TryResolve")
            || !ExecutionTypes.Is(operation.TargetMethod.ContainingType, "ResolutionScope")
            || operation.Arguments.FirstOrDefault(static argument => argument.Parameter?.Ordinal == 0) is not { } point)
        {
            return;
        }

        var value = Unwrap(point.Value);
        if (value.Type is not INamedTypeSymbol pointType
            || !SymbolEqualityComparer.Default.Equals(pointType.OriginalDefinition, observationPoint))
        {
            return;
        }

        var owner = value switch
        {
            IPropertyReferenceOperation property => property.Instance,
            IFieldReferenceOperation field => field.Instance,
            IInvocationOperation { TargetMethod.Name: "Of" } factory
                when factory.TargetMethod.ContainingType.ToDisplayString() == "HEAL.HeuristicLib.Inspection.ObservationPoint" =>
                factory.Arguments.FirstOrDefault(static argument => argument.Parameter?.Ordinal == 0)?.Value,
            _ => null
        };
        // Locals and helper results need data-flow analysis to establish ownership; do not reject valid aliases.
        if (owner is null || IsThis(owner))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(ObservationPointOwnerRule, point.Syntax.GetLocation(), value.Syntax.ToString()));
    }

    private static IOperation Unwrap(IOperation operation)
    {
        while (operation is IConversionOperation conversion)
            operation = conversion.Operand;
        return operation;
    }

    private static bool IsThis(IOperation operation) =>
        Unwrap(operation) is IInstanceReferenceOperation { ReferenceKind: InstanceReferenceKind.ContainingTypeInstance };

    private static void ReportForbiddenResourceMembers(Action<Diagnostic> report, ITypeSymbol resourceType, ImmutableArray<INamedTypeSymbol> forbiddenTypes, Location? fallback)
    {
        foreach (var member in GetResourceMembers(resourceType))
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

            var location = member.Locations.FirstOrDefault(static candidate => candidate.IsInSource) ?? fallback;
            if (location is null)
            {
                continue;
            }

            report(Diagnostic.Create(
                ResourceGraphRule,
                location,
                member.Name,
                forbiddenType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
        }
    }

    private static IEnumerable<ISymbol> GetResourceMembers(ITypeSymbol resourceType)
    {
        if (resourceType is not INamedTypeSymbol namedResource)
        {
            yield break;
        }

        for (var current = namedResource; current is not null; current = current.BaseType)
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

        foreach (var member in GetResourceMembers(namedType))
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

    private static void AnalyzeAssignment(SyntaxNodeAnalysisContext context, INamedTypeSymbol? configurationContract)
    {
        if (context.Node is AssignmentExpressionSyntax assignment)
        {
            AnalyzeMutationTarget(context, assignment.Left, configurationContract);
        }
    }

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
