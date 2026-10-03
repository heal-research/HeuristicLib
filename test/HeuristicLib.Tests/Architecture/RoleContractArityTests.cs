using System.Reflection;

namespace HEAL.HeuristicLib.Tests.Architecture;

/// <summary>
/// The configuration layer names only the candidate, and the execution layer names everything.
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
    private static readonly Assembly Main = typeof(IOperator).Assembly;

    private static readonly IReadOnlyList<Type> Roles = RolesDerivedFrom(typeof(IOperator));
    private static readonly IReadOnlyList<Type> RoleExecutions = RolesDerivedFrom(typeof(IOperatorExecution));

    public static TheoryData<Type> ConfigurationContracts => [.. Roles];
    public static TheoryData<Type> ExecutionContracts => [.. RoleExecutions];

    public static TheoryData<Type> TopologyBases => [.. Roles.SelectMany(role => Main.GetExportedTypes()
        .Where(type => type.Name == "Wrapping" + role.Name[1..] || type.Name == "Multi" + role.Name[1..]))];

    [Fact]
    public void EveryRole_DeclaresBothHalves()
    {
        Roles.Count.ShouldBe(9);
        RoleExecutions.Count.ShouldBe(Roles.Count);
    }

    [Theory]
    [MemberData(nameof(ConfigurationContracts))]
    public void AConfiguredRole_NamesOnlyTheCandidate(Type role) =>
        role.GetGenericArguments().Select(argument => argument.Name).ShouldBe(["TCandidate"]);

    [Theory]
    [MemberData(nameof(ConfigurationContracts))]
    public void AConfiguredRole_PreparesItsTypedFactoryWithoutAScope(Type role)
    {
        var factory = role.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Single(method => method.Name == "CreateExecutionFactory");

        factory.GetParameters().ShouldBeEmpty();
        factory.ReturnType.GetGenericTypeDefinition().ShouldBe(typeof(ExecutionFactory<>));
        factory.ReturnType.GetGenericArguments().Single().GetInterfaces().ShouldContain(typeof(IOperatorExecution));
        role.GetMethods().ShouldNotContain(method => method.Name == "CreateExecutionInstance");
    }

    [Theory]
    [MemberData(nameof(TopologyBases))]
    public void ATopologyBase_PreparesBeforeReceivingItsResolvedChildren(Type topology)
    {
        var factory = topology.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Single(method => method.Name == "CreateExecutionFactory");
        var hook = topology.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Single(method => method.IsAbstract);

        factory.GetParameters().ShouldBeEmpty();
        factory.ReturnType.GetGenericTypeDefinition().ShouldBe(typeof(ExecutionFactory<>));
        hook.GetParameters().ShouldBeEmpty();
        var wrapper = topology.Name.StartsWith("Wrapping", StringComparison.Ordinal);
        hook.ReturnType.GetGenericTypeDefinition().ShouldBe(wrapper ? typeof(WrapperExecutionFactory<>) : typeof(CompositeExecutionFactory<>));
        var execution = hook.ReturnType.GetGenericArguments().Single();
        execution.GetGenericTypeDefinition().ShouldBe(factory.ReturnType.GetGenericArguments().Single().GetGenericTypeDefinition());
        var constructor = hook.ReturnType.GetMethod("Invoke")!;
        constructor.ReturnType.ShouldBe(execution);
        constructor.GetParameters().Single().ParameterType.ShouldBe(wrapper ? execution : typeof(ImmutableArray<>).MakeGenericType(execution));
        topology.GetMethods().ShouldNotContain(method => method.Name == "CreateExecutionInstance");
    }

    [Theory]
    [InlineData(typeof(IterativeAlgorithm<,,>))]
    [InlineData(typeof(IterativeAlgorithm<,,,,>))]
    public void AnIterativeBase_UsesTheCommonFactoryAndRequiresAnIterativeExecution(Type algorithm)
    {
        var factories = algorithm.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(method => method.Name == "CreateExecutionFactory").ToArray();
        factories.ShouldNotBeEmpty();
        foreach (var factory in factories)
        {
            factory.GetParameters().ShouldBeEmpty();
            factory.ReturnType.GetGenericTypeDefinition().ShouldBe(typeof(ExecutionFactory<>));
        }
        var hook = algorithm.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Single(method => method.IsAbstract);
        hook.Name.ShouldBe("CreateIterationFactory");
        hook.GetParameters().ShouldBeEmpty();
        hook.ReturnType.GetGenericTypeDefinition().ShouldBe(typeof(ExecutionFactory<>));
        var constructor = hook.ReturnType.GetMethod("Invoke")!;
        constructor.ReturnType.GetGenericTypeDefinition().ShouldBe(typeof(IterativeAlgorithmExecution<,,,>));
        constructor.GetParameters().Select(parameter => parameter.ParameterType).ShouldBe([typeof(ResolutionScope)]);
        algorithm.GetMethods().ShouldNotContain(method => method.Name == "CreateExecutionInstance");
    }

    [Theory]
    [MemberData(nameof(ExecutionContracts))]
    public void ItsExecution_StillNamesTheSearchSpaceAndProblem(Type executionContract)
    {
        var named = executionContract.GetGenericArguments().Select(argument => argument.Name).ToArray();

        named.Take(3).ShouldBe(["TCandidate", "TSearchSpace", "TProblem"]);
        named.Skip(3).ShouldBeSubsetOf(["TSearchState"]);
    }

    /// <summary>
    /// The algorithm keeps two forms, and the line between them is produced versus consumed: an algorithm returns
    /// its search state, so a form that does not name the state cannot declare the members that return it.
    /// </summary>
    [Fact]
    public void TheAlgorithmContract_NamesTheCandidateAndWhatItReturns()
    {
        var forms = Main.GetExportedTypes()
            .Where(type => type.IsInterface && type.Name.StartsWith("IAlgorithm`", StringComparison.Ordinal))
            .Select(type => type.GetGenericArguments().Select(argument => argument.Name).ToArray())
            .OrderBy(named => named.Length)
            .ToArray();

        forms.ShouldBe([["TCandidate"], ["TCandidate", "TSearchState"]]);
    }

    private static IReadOnlyList<Type> RolesDerivedFrom(Type half) =>
        [.. Main.GetExportedTypes()
            .Where(type => type.IsInterface && type.IsGenericTypeDefinition &&
                           type.Namespace == typeof(IOperator).Namespace && type.GetInterfaces().Contains(half))
            .OrderBy(type => type.Name, StringComparer.Ordinal)];
}
