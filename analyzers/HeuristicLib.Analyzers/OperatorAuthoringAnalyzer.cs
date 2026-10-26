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
    public const string StatefulStateDiagnosticId = "HLib0002";
    public const string ConfigurationMutationDiagnosticId = "HLib0003";
    public const string StateContractDiagnosticId = "HLib0004";

    private const string OperatorMetadataName = "HEAL.HeuristicLib.Operators.IOperator";
    private const string ConfigurationMetadataName = "HEAL.HeuristicLib.Execution.IConfigurationNode";
    private const string CreateScopedStateMethodName = "CreateScopedState";
    private const string GetScopedStateMethodName = "GetScopedState";

    private static readonly DiagnosticDescriptor StatefulStateRule = new(
        id: StatefulStateDiagnosticId,
        title: "Stateful operator state must not contain execution graph dependencies",
        messageFormat: "Scoped state '{0}' exposes execution graph dependency '{1}'. Use the explicit executor operator path instead.",
        category: "Architecture",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor ScopedStateOwnerRule = new(
        id: StatefulStateDiagnosticId,
        title: "Scoped state must be requested by the configuration it belongs to",
        messageFormat: "Scoped state is requested for '{0}' instead of this. A configuration requests only its own state; another configuration's state stays with its own build.",
        category: "Architecture",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor ObservationPointOwnerRule = new(
        id: StatefulStateDiagnosticId,
        title: "A configuration must resolve only its own observation points",
        messageFormat: "Observation point '{0}' is not declared by this configuration. Resolve only this configuration's own observation points.",
        category: "Architecture",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor ConfigurationMutationRule = new(
        id: ConfigurationMutationDiagnosticId,
        title: "Configurations must not be mutated after construction",
        messageFormat: "Configuration member '{0}' is mutated outside construction. Store mutable execution data in scoped state or an executor instead.",
        category: "Architecture",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor StateContractRule = new(
        id: StateContractDiagnosticId,
        title: "Stateful operator state must be a dedicated state type",
        messageFormat: "'{0}' is a framework contract type and cannot be operator state. Check the type argument order: the state type parameter is always last, so omitting an earlier argument such as TProblem or TSearchState shifts a framework contract into the TState position.",
        category: "Architecture",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [StatefulStateRule, ScopedStateOwnerRule, ObservationPointOwnerRule, ConfigurationMutationRule, StateContractRule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(startContext =>
        {
            var forbiddenTypes = GetForbiddenTypes(startContext.Compilation);
            var stateContractTypes = GetStateContractTypes(startContext.Compilation, forbiddenTypes);
            var operatorContract = startContext.Compilation.GetTypeByMetadataName(OperatorMetadataName);
            var configurationContract = startContext.Compilation.GetTypeByMetadataName(ConfigurationMetadataName);
            var observationPoint = startContext.Compilation.GetTypeByMetadataName("HEAL.HeuristicLib.Inspection.ObservationPoint`1");
            startContext.RegisterSymbolAction(
                symbolContext => AnalyzeStatefulOperator(symbolContext, forbiddenTypes, stateContractTypes, operatorContract),
                SymbolKind.NamedType);

            startContext.RegisterSyntaxNodeAction(
                syntaxContext => AnalyzeScopedStateRequest(syntaxContext, forbiddenTypes),
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

    private static ImmutableArray<INamedTypeSymbol> GetStateContractTypes(Compilation compilation, ImmutableArray<INamedTypeSymbol> forbiddenTypes)
    {
        var metadataNames = new[]
        {
            "HEAL.HeuristicLib.Problems.IProblem`2",
            "HEAL.HeuristicLib.SearchSpaces.ISearchSpace",
            "HEAL.HeuristicLib.Algorithms.ISearchState",
            "HEAL.HeuristicLib.Operators.IOperatorExecutor"
        };

        return [.. forbiddenTypes.Concat(metadataNames
            .Select(compilation.GetTypeByMetadataName)
            .Where(static type => type is not null)
            .Cast<INamedTypeSymbol>())];
    }

    private static void AnalyzeStatefulOperator(SymbolAnalysisContext context, ImmutableArray<INamedTypeSymbol> forbiddenTypes, ImmutableArray<INamedTypeSymbol> stateContractTypes, INamedTypeSymbol? operatorContract)
    {
        if (context.Symbol is not INamedTypeSymbol operatorType || operatorContract is null || !TryGetOperatorStateType(operatorType, operatorContract, out var stateType))
        {
            return;
        }

        if (stateType.TypeKind == TypeKind.TypeParameter)
        {
            return;
        }

        var stateContract = stateContractTypes.FirstOrDefault(candidate => IsOrImplementsDefinition(stateType, candidate));
        if (stateContract is not null)
        {
            var contractLocation = GetBaseTypeLocation(operatorType) ?? operatorType.Locations.FirstOrDefault(static candidate => candidate.IsInSource);
            if (contractLocation is not null)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    StateContractRule,
                    contractLocation,
                    stateType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
            }

            return;
        }

        ReportForbiddenStateMembers(context.ReportDiagnostic, stateType, forbiddenTypes, operatorType.Locations.FirstOrDefault());
    }

    // An operator that builds its own executor requests its state from the scope, so the request names the state type.
    private static void AnalyzeScopedStateRequest(SyntaxNodeAnalysisContext context, ImmutableArray<INamedTypeSymbol> forbiddenTypes)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        if (context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol is not IMethodSymbol { Name: GetScopedStateMethodName, IsStatic: false, TypeArguments.Length: 1 } request
            || !ExecutionTypes.Is(request.ContainingType, "ResolutionScope"))
        {
            return;
        }

        // A scope forwarding the request passes the configuration it received.
        if (!ExecutionTypes.Is(context.ContainingSymbol?.ContainingType, "ResolutionScope"))
        {
            AnalyzeRequestOwner(context, invocation, ScopedStateOwnerRule);
        }

        if (request.TypeArguments[0] is not INamedTypeSymbol stateType)
        {
            return;
        }

        var forbiddenStateType = forbiddenTypes.FirstOrDefault(candidate => IsOrImplements(stateType, candidate));
        if (forbiddenStateType is not null)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                StatefulStateRule,
                invocation.GetLocation(),
                stateType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                forbiddenStateType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
            return;
        }

        ReportForbiddenStateMembers(context.ReportDiagnostic, stateType, forbiddenTypes, invocation.GetLocation());
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

    // A configuration requests only its own state, so the request passes this.
    private static void AnalyzeRequestOwner(SyntaxNodeAnalysisContext context, InvocationExpressionSyntax invocation, DiagnosticDescriptor rule)
    {
        if (context.SemanticModel.GetOperation(invocation, context.CancellationToken) is not IInvocationOperation operation
            || operation.Arguments.FirstOrDefault(static argument => argument.Parameter?.Ordinal == 0) is not { } configuration)
        {
            return;
        }

        var value = configuration.Value;
        while (value is IConversionOperation conversion)
        {
            value = conversion.Operand;
        }

        if (value is IInstanceReferenceOperation { ReferenceKind: InstanceReferenceKind.ContainingTypeInstance })
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            rule,
            configuration.Syntax.GetLocation(),
            value.Syntax.ToString()));
    }

    private static void ReportForbiddenStateMembers(Action<Diagnostic> report, ITypeSymbol stateType, ImmutableArray<INamedTypeSymbol> forbiddenTypes, Location? fallback)
    {
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

            var location = member.Locations.FirstOrDefault(static candidate => candidate.IsInSource) ?? fallback;
            if (location is null)
            {
                continue;
            }

            report(Diagnostic.Create(
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

            var stateFactory = current.OriginalDefinition.GetMembers(CreateScopedStateMethodName)
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

    private static Location? GetBaseTypeLocation(INamedTypeSymbol operatorType)
    {
        foreach (var reference in operatorType.DeclaringSyntaxReferences)
        {
            if (reference.GetSyntax() is TypeDeclarationSyntax { BaseList.Types.Count: > 0 } declaration)
            {
                return declaration.BaseList.Types[0].GetLocation();
            }
        }

        return null;
    }

    private static bool IsOrImplementsDefinition(ITypeSymbol type, INamedTypeSymbol candidateDefinition)
    {
        if (SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, candidateDefinition))
        {
            return true;
        }

        if (type is not INamedTypeSymbol namedType)
        {
            return false;
        }

        for (var current = namedType.BaseType; current is not null; current = current.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(current.OriginalDefinition, candidateDefinition))
            {
                return true;
            }
        }

        return namedType.AllInterfaces.Any(@interface => SymbolEqualityComparer.Default.Equals(@interface.OriginalDefinition, candidateDefinition));
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
