using System.Reflection;
using System.Runtime.CompilerServices;

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

    private static readonly IReadOnlyList<Type> RoleExecutions = RolesDerivedFrom(typeof(IOperatorExecutor));

    public static TheoryData<Type> ExecutionContracts => [.. RoleExecutions];

    [Fact]
    public void EveryRole_SharesTheConfigurationContractAndDeclaresItsExecution()
    {
        RolesDerivedFrom(typeof(IOperator)).ShouldBe([typeof(IOperator<,>)]);
        RoleExecutions.Count.ShouldBe(9);
    }

    [Fact]
    public void TheSharedConsumingContractNamesOperationThenCandidateAndBuildsInOneHook()
    {
        var contract = typeof(IOperator<,>);
        contract.GetGenericArguments().Select(argument => argument.Name).ShouldBe(["TOperation", "TCandidate"]);
        contract.GetInterfaces().ShouldContain(typeof(IOperator));
        var hook = contract.GetMethod("CreateExecutor")!;
        hook.GetGenericArguments().Select(argument => argument.Name).ShouldBe(["TSearchSpace", "TProblem", "TSearchState"]);
        hook.GetParameters().ShouldHaveSingleItem().ParameterType.GetGenericTypeDefinition().ShouldBe(typeof(ResolutionScope<,,,>));
        hook.ReturnType.ShouldBe(typeof(IExecutor));
        contract.GetMethods().ShouldNotContain(method => method.Name == "Prepare");
    }

    [Fact]
    public void PublicOperationParametersComeFirstInTypesAndMethods()
    {
        var types = Main.GetExportedTypes();
        var signatures = types.Select(type => (Name: type.ToString(), Parameters: type.GetGenericArguments()))
            .Concat(types.SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
                .SelectMany(method => DeclaredParameterLists(method).Select(parameters => (Name: $"{method.DeclaringType}.{method.Name}", Parameters: parameters))));
        foreach (var (name, parameters) in signatures)
        {
            var operation = Array.FindIndex(parameters, parameter => parameter.IsGenericParameter &&
                parameter.GetGenericParameterConstraints().Any(constraint => constraint.IsGenericType &&
                    constraint.GetGenericTypeDefinition() == typeof(IOperation<>)));
            if (operation >= 0)
                operation.ShouldBe(0, name);
        }
    }

    /// <summary>
    /// An extension method lists the type parameters of its receiver before its own, as an extension block declares
    /// them, so each of the two lists must lead with its operation.
    /// </summary>
    private static IEnumerable<Type[]> DeclaredParameterLists(MethodInfo method)
    {
        var parameters = method.GetGenericArguments();
        if (!method.IsDefined(typeof(ExtensionAttribute)))
            return [parameters];

        var receiver = method.GetParameters()[0].ParameterType;
        return [[.. parameters.Where(parameter => Mentions(receiver, parameter))], [.. parameters.Where(parameter => !Mentions(receiver, parameter))]];
    }

    private static bool Mentions(Type type, Type parameter) =>
        type == parameter
        || (type.HasElementType && Mentions(type.GetElementType()!, parameter))
        || (type.IsGenericType && type.GetGenericArguments().Any(argument => Mentions(argument, parameter)));

    [Fact]
    public void TheBoundIterativeBase_BuildsInOneHookAndRequiresAnIterativeExecution()
    {
        var hook = typeof(IterativeAlgorithm<,,,,>).GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Single(method => method.IsAbstract);
        hook.Name.ShouldBe("CreateExecutor");
        hook.IsFamily.ShouldBeTrue();
        hook.GetParameters().ShouldHaveSingleItem().ParameterType.GetGenericTypeDefinition().ShouldBe(typeof(ResolutionScope<,,,>));
        hook.ReturnType.GetGenericTypeDefinition().ShouldBe(typeof(IterativeAlgorithmExecutor<,,,>));
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
