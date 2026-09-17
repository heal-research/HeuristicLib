using System.Reflection;

namespace HEAL.HeuristicLib.Tests.Architecture;

/// <summary>
/// The configuration layer names only the candidate, and the execution layer names everything.
/// </summary>
/// <remarks>
/// This split is what the arity reduction bought, and nothing else in the suite fails if it is lost: a role that
/// reintroduced <c>TSearchSpace</c> on its configuration contract would compile, pass every existing test, and
/// surface only as arity in a user's field and parameter declarations. The roles are discovered from the assembly
/// rather than listed, so a new role added at the wrong arity is caught on the same terms as a regression in an
/// existing one.
/// </remarks>
public sealed class RoleContractArityTests
{
    private static readonly Assembly Contracts = typeof(IOperator).Assembly;

    private static readonly IReadOnlyList<Type> Roles = RolesDerivedFrom(typeof(IOperator));
    private static readonly IReadOnlyList<Type> RoleInstances = RolesDerivedFrom(typeof(IOperatorInstance));

    public static TheoryData<Type> ConfigurationContracts => [.. Roles];
    public static TheoryData<Type> ExecutionContracts => [.. RoleInstances];

    [Fact]
    public void EveryRole_DeclaresBothHalves()
    {
        Roles.Count.ShouldBe(9);
        RoleInstances.Count.ShouldBe(Roles.Count);
    }

    [Theory]
    [MemberData(nameof(ConfigurationContracts))]
    public void AConfiguredRole_NamesOnlyTheCandidate(Type role) =>
        role.GetGenericArguments().Select(argument => argument.Name).ShouldBe(["TCandidate"]);

    [Theory]
    [MemberData(nameof(ExecutionContracts))]
    public void ItsExecutionInstance_StillNamesTheSearchSpaceAndProblem(Type instance)
    {
        var named = instance.GetGenericArguments().Select(argument => argument.Name).ToArray();

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
        var forms = Contracts.GetExportedTypes()
            .Where(type => type.IsInterface && type.Name.StartsWith("IAlgorithm`", StringComparison.Ordinal))
            .Select(type => type.GetGenericArguments().Select(argument => argument.Name).ToArray())
            .OrderBy(named => named.Length)
            .ToArray();

        forms.ShouldBe([["TCandidate"], ["TCandidate", "TSearchState"]]);
    }

    private static IReadOnlyList<Type> RolesDerivedFrom(Type half) =>
        [.. Contracts.GetExportedTypes()
            .Where(type => type.IsInterface && type.IsGenericTypeDefinition && type.GetInterfaces().Contains(half))
            .OrderBy(type => type.Name, StringComparer.Ordinal)];
}
