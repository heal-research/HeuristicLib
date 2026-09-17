using System.Reflection;
using HEAL.HeuristicLib.Operators.Mutators;

namespace HEAL.HeuristicLib.Tests.Operators;

public class OperatorTopologyTests
{
    /// <summary>
    /// Every operator role namespace. Adding a role means adding it here, which is what keeps the conformance
    /// checks below a complete matrix rather than a sample.
    /// </summary>
    public static TheoryData<string> RoleNamespaces =>
    [
        "HEAL.HeuristicLib.Operators.Creators",
        "HEAL.HeuristicLib.Operators.Crossovers",
        "HEAL.HeuristicLib.Operators.Evaluators",
        "HEAL.HeuristicLib.Operators.Interceptors",
        "HEAL.HeuristicLib.Operators.Mutators",
        "HEAL.HeuristicLib.Operators.Replacers",
        "HEAL.HeuristicLib.Operators.Selectors",
        "HEAL.HeuristicLib.Operators.Terminators"
    ];

    /// <summary>
    /// A migrated configuration contract names the candidate alone. The search space and problem the run supplies
    /// arrive as method type arguments when the execution instance is created, so they are not part of the type an
    /// author stores in a field, passes as a parameter or reuses across problems.
    /// </summary>
    /// <remarks>
    /// All nine roles are listed, terminators and interceptors included. They were the last to keep a second type
    /// argument, and they lost it for the same reason as the other seven: an operator does not originate the search
    /// state either, so the algorithm that resolves it supplies one.
    /// </remarks>
    [Theory]
    [InlineData(typeof(ICreator<>))]
    [InlineData(typeof(ICrossover<>))]
    [InlineData(typeof(IEvaluator<>))]
    [InlineData(typeof(IMutator<>))]
    [InlineData(typeof(IRefiner<>))]
    [InlineData(typeof(IReplacer<>))]
    [InlineData(typeof(ISelector<>))]
    [InlineData(typeof(ITerminator<>))]
    [InlineData(typeof(IInterceptor<>))]
    public void MigratedOperatorConfigurationContracts_NameOnlyTheCandidate(Type roleContract)
    {
        var typeParameters = roleContract.GetGenericArguments();

        typeParameters.Length.ShouldBe(1);
        Variance(typeParameters[0]).ShouldBe(GenericParameterAttributes.None);
    }

    /// <summary>
    /// An execution instance is created for one run and runs over that run's search space and problem, so it names
    /// both and stays contravariant in them. Every configuration contract has migrated, so only instance contracts
    /// remain here.
    /// </summary>
    [Theory]
    [InlineData(typeof(ICreatorInstance<,,>))]
    [InlineData(typeof(ICrossoverInstance<,,>))]
    [InlineData(typeof(IEvaluatorInstance<,,>))]
    [InlineData(typeof(IMutatorInstance<,,>))]
    [InlineData(typeof(IReplacerInstance<,,>))]
    [InlineData(typeof(ISelectorInstance<,,>))]
    [InlineData(typeof(IInterceptorInstance<,,,>))]
    [InlineData(typeof(ITerminatorInstance<,,,>))]
    public void OperatorRoleContracts_PreserveCandidateAndUseContravariantContext(Type roleContract)
    {
        var typeParameters = roleContract.GetGenericArguments();

        Variance(typeParameters[0]).ShouldBe(GenericParameterAttributes.None);
        Variance(typeParameters[1]).ShouldBe(GenericParameterAttributes.Contravariant);
        Variance(typeParameters[2]).ShouldBe(GenericParameterAttributes.Contravariant);
    }

    /// <summary>
    /// <c>TSearchState</c> variance is structural rather than stylistic: a terminator only consumes the state, while
    /// an interceptor returns it and therefore cannot be contravariant in it.
    /// </summary>
    [Theory]
    [InlineData(typeof(IInterceptorInstance<,,,>), GenericParameterAttributes.None)]
    [InlineData(typeof(ITerminatorInstance<,,,>), GenericParameterAttributes.Contravariant)]
    public void SearchStateAwareRoleContracts_DeclareExpectedSearchStateVariance(Type roleContract, GenericParameterAttributes expectedVariance)
    {
        var searchState = roleContract.GetGenericArguments()[3];

        Variance(searchState).ShouldBe(expectedVariance);
    }

    [Theory]
    [MemberData(nameof(RoleNamespaces))]
    public void OperatorConfigurations_RetainedChildOperatorsArePubliclyInspectable(string roleNamespace)
    {
        var hiddenChildProperties = OperatorTypesIn(roleNamespace)
            .Where(type => typeof(IOperator).IsAssignableFrom(type))
            .SelectMany(type => type.GetProperties(
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.DeclaredOnly))
            .Where(property => property.Name.StartsWith("Child", StringComparison.Ordinal))
            .Where(property => property.GetMethod is not { IsPublic: true })
            .Select(property => $"{property.DeclaringType}.{property.Name}")
            .ToArray();

        hiddenChildProperties.ShouldBeEmpty();
    }

    [Theory]
    [MemberData(nameof(RoleNamespaces))]
    public void OperatorExecutionInstances_DoNotPubliclyExposeChildMachinery(string roleNamespace)
    {
        var publicChildProperties = OperatorTypesIn(roleNamespace)
            .Where(type => typeof(IOperatorInstance).IsAssignableFrom(type))
            .SelectMany(type => type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            .Where(property => property.Name.StartsWith("Child", StringComparison.Ordinal))
            .Select(property => $"{property.DeclaringType}.{property.Name}")
            .ToArray();

        publicChildProperties.ShouldBeEmpty();
    }

    /// <summary>
    /// Configuration collections carry structural equality through their member type, so an operator configuration
    /// must not retain an ordered collection as <see cref="ImmutableArray{T}"/>, which compares by array reference.
    /// </summary>
    [Theory]
    [MemberData(nameof(RoleNamespaces))]
    public void OperatorConfigurations_RetainCollectionsAsValueArray(string roleNamespace)
    {
        var referenceEqualCollections = OperatorTypesIn(roleNamespace)
            .Where(type => typeof(IOperator).IsAssignableFrom(type))
            .SelectMany(type => type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            .Where(property => property.PropertyType.IsGenericType &&
                               property.PropertyType.GetGenericTypeDefinition() == typeof(ImmutableArray<>))
            .Select(property => $"{property.DeclaringType}.{property.Name}")
            .ToArray();

        referenceEqualCollections.ShouldBeEmpty();
    }

    private static GenericParameterAttributes Variance(Type typeParameter) =>
        typeParameter.GenericParameterAttributes & GenericParameterAttributes.VarianceMask;

    private static IEnumerable<Type> OperatorTypesIn(string roleNamespace) =>
        typeof(Mutator<,,>).Assembly
            .GetTypes()
            .Where(type => type.Namespace?.StartsWith(roleNamespace, StringComparison.Ordinal) == true);
}
