using Microsoft.CodeAnalysis;
using static HEAL.HeuristicLib.Tests.Architecture.SourceCompilation;

namespace HEAL.HeuristicLib.Tests.Architecture;

/// <summary>
/// The configuration layer names only the candidate, and executors name only their invocation inputs and outputs.
/// </summary>
/// <remarks>
/// This split is what the arity reduction bought, and nothing else in the suite fails if it is lost: a role that
/// reintroduced <c>TSearchSpace</c> on its configuration contract would compile, pass every existing test, and
/// surface only as arity in a user's field and parameter declarations. The roles are discovered from the public
/// Operators namespace rather than listed, so a new role added there at the wrong arity is caught on the same terms
/// as a regression in an existing one.
/// </remarks>
public sealed class RoleContractArityTests
{
    private const string OperatorsNamespace = "HEAL.HeuristicLib.Operators";

    private static readonly IAssemblySymbol Main = CreateMainCompilation(FindRepositoryRoot()).Compilation.Assembly;

    private static readonly INamedTypeSymbol[] PublicTypes = [.. PublicTypesIn(Main.GlobalNamespace)];

    private static readonly INamedTypeSymbol[] RoleExecutions = RolesDerivedFrom(Contract($"{OperatorsNamespace}.IOperatorExecutor"));

    public static TheoryData<string> ExecutionContracts => [.. RoleExecutions.Select(type => type.ToDisplayString())];

    [Fact]
    public void EveryRole_SharesTheConfigurationContractAndDeclaresItsExecution()
    {
        INamedTypeSymbol[] expected = [Contract($"{OperatorsNamespace}.IOperator`1"), Contract($"{OperatorsNamespace}.IOperator`2")];
        RolesDerivedFrom(Contract($"{OperatorsNamespace}.IOperator"))
            .SequenceEqual(expected, SymbolEqualityComparer.Default).ShouldBeTrue();
        RoleExecutions.Length.ShouldBe(9);
    }

    [Fact]
    public void TheSharedConsumingContractNamesOperationThenCandidateAndBuildsInOneHook()
    {
        var contract = Contract($"{OperatorsNamespace}.IOperator`2");
        contract.TypeParameters.Select(argument => argument.Name).ShouldBe(["TOperation", "TCandidate"]);
        contract.AllInterfaces.Contains(Contract($"{OperatorsNamespace}.IOperator"), SymbolEqualityComparer.Default).ShouldBeTrue();
        PublicMethods(contract).ShouldNotContain(method => method.Name == "CreateExecutor");

        var hook = PublicMethods(Contract($"{OperatorsNamespace}.IOperator`1"))
            .Where(method => method.Name == "CreateExecutor").ShouldHaveSingleItem();
        hook.TypeParameters.Select(argument => argument.Name).ShouldBe(["TSearchSpace", "TProblem", "TSearchState"]);
        SymbolEqualityComparer.Default.Equals(hook.Parameters.ShouldHaveSingleItem().Type.OriginalDefinition,
            Contract("HEAL.HeuristicLib.Execution.ResolutionScope`4")).ShouldBeTrue();
        SymbolEqualityComparer.Default.Equals(hook.ReturnType, Contract("HEAL.HeuristicLib.Execution.IExecutor")).ShouldBeTrue();
        PublicMethods(contract).ShouldNotContain(method => method.Name == "Prepare");
    }

    [Fact]
    public void PublicOperationParametersComeFirstInTypesAndMethods()
    {
        var operationContract = Contract($"{OperatorsNamespace}.IOperation`1");
        var signatures = PublicTypes.Select(type => (Name: type.ToDisplayString(), Parameters: TypeParameters(type)))
            .Concat(PublicTypes.SelectMany(PublicMethods)
                .SelectMany(method => DeclaredParameterLists(method)
                    .Select(parameters => (Name: method.ToDisplayString(), Parameters: parameters))));
        foreach (var (name, parameters) in signatures)
        {
            var operation = Array.FindIndex(parameters, parameter =>
                parameter.ConstraintTypes.Any(constraint => SymbolEqualityComparer.Default.Equals(constraint.OriginalDefinition, operationContract)));
            if (operation >= 0)
                operation.ShouldBe(0, name);
        }
    }

    private static IEnumerable<IMethodSymbol> PublicMethods(INamedTypeSymbol type) =>
        type.GetMembers().OfType<IMethodSymbol>()
            .Where(method => method.DeclaredAccessibility == Accessibility.Public &&
                             method.MethodKind is not (MethodKind.Constructor or MethodKind.StaticConstructor));

    private static ITypeParameterSymbol[] TypeParameters(INamedTypeSymbol type) =>
        type.ContainingType is { } containingType
            ? [.. TypeParameters(containingType), .. type.TypeParameters]
            : [.. type.TypeParameters];

    /// <summary>
    /// An extension method lists the type parameters of its receiver before its own, as an extension block declares
    /// them, so each of the two lists must lead with its operation.
    /// </summary>
    private static IEnumerable<ITypeParameterSymbol[]> DeclaredParameterLists(IMethodSymbol method)
    {
        var parameters = method.TypeParameters;
        if (!method.IsExtensionMethod)
            return [[.. parameters]];

        var receiver = method.Parameters[0].Type;
        return [[.. parameters.Where(parameter => Mentions(receiver, parameter))], [.. parameters.Where(parameter => !Mentions(receiver, parameter))]];
    }

    private static bool Mentions(ITypeSymbol type, ITypeParameterSymbol parameter) =>
        SymbolEqualityComparer.Default.Equals(type, parameter) || type switch
        {
            IArrayTypeSymbol array => Mentions(array.ElementType, parameter),
            IPointerTypeSymbol pointer => Mentions(pointer.PointedAtType, parameter),
            INamedTypeSymbol named => named.TypeArguments.Any(argument => Mentions(argument, parameter)) ||
                                      (named.ContainingType is { } containingType && Mentions(containingType, parameter)),
            _ => false
        };

    [Theory]
    [MemberData(nameof(ExecutionContracts))]
    public void ItsExecution_OmitsTheBoundSearchSpaceAndProblem(string displayName)
    {
        var executionContract = RoleExecutions.Single(type => type.ToDisplayString() == displayName);
        TypeParameters(executionContract).Select(argument => argument.Name).ShouldHaveSingleItem()
            .ShouldBeOneOf("TCandidate", "TSearchState");
    }

    /// <summary>
    /// The algorithm keeps two forms, and the line between them is produced versus consumed: an algorithm returns
    /// its search state, so a form that does not name the state cannot declare the members that return it.
    /// </summary>
    [Fact]
    public void TheAlgorithmContract_NamesTheCandidateAndWhatItReturns()
    {
        var forms = PublicTypes
            .Where(type => type.TypeKind == TypeKind.Interface && type.MetadataName.StartsWith("IAlgorithm`", StringComparison.Ordinal))
            .Select(type => TypeParameters(type).Select(argument => argument.Name).ToArray())
            .OrderBy(named => named.Length)
            .ToArray();

        forms.ShouldBe([["TCandidate"], ["TCandidate", "TSearchState"]]);
    }

    private static INamedTypeSymbol[] RolesDerivedFrom(INamedTypeSymbol half) =>
        [.. PublicTypes
            .Where(type => type.TypeKind == TypeKind.Interface && type.IsGenericType &&
                           SymbolEqualityComparer.Default.Equals(type.ContainingNamespace, half.ContainingNamespace) &&
                           type.AllInterfaces.Contains(half, SymbolEqualityComparer.Default))
            .OrderBy(type => type.MetadataName, StringComparer.Ordinal)];

    private static IEnumerable<INamedTypeSymbol> PublicTypesIn(INamespaceOrTypeSymbol container)
    {
        foreach (var type in container.GetTypeMembers().Where(type => type.DeclaredAccessibility == Accessibility.Public))
        {
            yield return type;
            foreach (var nested in PublicTypesIn(type))
                yield return nested;
        }

        if (container is INamespaceSymbol ns)
        {
            foreach (var type in ns.GetNamespaceMembers().SelectMany(PublicTypesIn))
                yield return type;
        }
    }

    private static INamedTypeSymbol Contract(string metadataName) =>
        Main.GetTypeByMetadataName(metadataName).ShouldNotBeNull();
}
