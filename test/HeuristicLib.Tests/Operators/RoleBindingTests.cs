using System.Reflection;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Operators.Interceptors;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Operators.Refiners;
using HEAL.HeuristicLib.Operators.Replacers;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Operators.Terminators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Problems.TestFunctions;

namespace HEAL.HeuristicLib.Tests.Operators;

/// <summary>
/// Every role's binding check, over one bound operator per role.
/// </summary>
/// <remarks>
/// The nine roles share one migration shape, and each declares its own <c>TryResolve</c> next to itself. That the
/// shape is identical is what makes the roles interchangeable to reason about, so it is worth a test rather than an
/// assumption: an operator written for a search space and problem resolves over exactly those, is reported over
/// anything else, and never throws out of the check.
/// </remarks>
public class RoleBindingTests
{
    public static TheoryData<string> Roles =>
    [
        "Creator", "Crossover", "Evaluator", "Mutator", "Refiner", "Replacer", "Selector", "Terminator", "Interceptor"
    ];

    [Theory]
    [MemberData(nameof(Roles))]
    public void ABoundOperator_ResolvesOverItsOwnSearchSpaceAndProblem(string role)
    {
        var (instance, reason) = TryResolve(role, typeof(TestFunctionProblem));

        instance.ShouldNotBeNull();
        reason.ShouldBeNull();
    }

    [Theory]
    [MemberData(nameof(Roles))]
    public void ABoundOperator_IsReportedOverAWiderProblem(string role)
    {
        var (instance, reason) = TryResolve(role, typeof(IProblem<RealVector, BoundedRealVectorSearchSpace>));

        instance.ShouldBeNull();
        reason.ShouldNotBeNull();
        reason.ShouldContain(nameof(TestFunctionProblem));
    }

    /// <summary>
    /// The resolver form is the same check with the binding named once, so it must agree with the registry form.
    /// </summary>
    /// <remarks>
    /// All nine have one, including the two state-aware roles: their resolver carries the search state as a fourth
    /// type argument and derives from the triple resolver, so one object serves every role an algorithm resolves and
    /// no call site names anything.
    /// </remarks>
    [Theory]
    [MemberData(nameof(Roles))]
    public void TheResolverForm_AgreesWithTheRegistryForm(string role)
    {
        var throughRegistry = TryResolve(role, typeof(IProblem<RealVector, BoundedRealVectorSearchSpace>));
        var throughResolver = TryResolve(role, typeof(IProblem<RealVector, BoundedRealVectorSearchSpace>), viaResolver: true);

        throughResolver.Instance.ShouldBe(throughRegistry.Instance);
        throughResolver.Reason.ShouldBe(throughRegistry.Reason);

        TryResolve(role, typeof(TestFunctionProblem), viaResolver: true).Instance.ShouldNotBeNull();
    }

    /// <summary>Calls the role's own <c>TryResolve</c> reflectively, so the test is the same for all nine.</summary>
    private static (object? Instance, string? Reason) TryResolve(string role, Type problem, bool viaResolver = false)
    {
        var extensions = typeof(IMutator<>).Assembly
            .GetType($"HEAL.HeuristicLib.Operators.{role}ResolverExtensions")!;

        var stateAware = role is "Terminator" or "Interceptor";
        Type[] typeArguments = stateAware
            ? [typeof(RealVector), typeof(BoundedRealVectorSearchSpace), problem, typeof(SingleSolutionState<RealVector>)]
            : [typeof(RealVector), typeof(BoundedRealVectorSearchSpace), problem];

        var registry = new ExecutionInstanceRegistry();
        object receiver = registry;

        if (viaResolver)
        {
            // An extension block's own type parameters are merged into the emitted static method, so the resolver
            // form takes the same type arguments reflectively even though a C# call site names none of them. A
            // state-aware role's resolver carries the state too, which is the whole reason it takes four.
            var forMethod = typeof(ExecutionInstanceRegistryResolverExtensions)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Single(method => method.Name == nameof(ExecutionInstanceRegistryResolverExtensions.For)
                                  && method.GetGenericArguments().Length == typeArguments.Length)
                .MakeGenericMethod(typeArguments);
            receiver = forMethod.Invoke(null, [registry])!;
        }

        var tryResolve = extensions
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(method => method.Name == "TryResolve"
                              && method.GetParameters()[0].ParameterType.Name.StartsWith(viaResolver ? "ExecutionInstanceResolver" : nameof(ExecutionInstanceRegistry), StringComparison.Ordinal))
            .MakeGenericMethod(typeArguments);
        var arguments = new object?[] { receiver, BoundOperators[role], null, null };
        tryResolve.Invoke(null, arguments);
        return (arguments[2], (string?)arguments[3]);
    }

    private static readonly Dictionary<string, object> BoundOperators = new()
    {
        ["Creator"] = new BoundCreator(),
        ["Crossover"] = new BoundCrossover(),
        ["Evaluator"] = new BoundEvaluator(),
        ["Mutator"] = new BoundMutator(),
        ["Refiner"] = new BoundRefiner(),
        ["Replacer"] = new BoundReplacer(),
        ["Selector"] = new BoundSelector(),
        ["Terminator"] = new BoundTerminator(),
        ["Interceptor"] = new BoundInterceptor(),
    };

    private sealed record BoundCreator : StatelessCreator<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>
    {
        public override IReadOnlyList<RealVector> Create(int count, IRandomNumberGenerator random, BoundedRealVectorSearchSpace searchSpace, TestFunctionProblem problem) => [];
    }

    private sealed record BoundCrossover : StatelessCrossover<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>
    {
        public override IReadOnlyList<RealVector> Cross(IReadOnlyList<Parents<RealVector>> parents, IRandomNumberGenerator random, BoundedRealVectorSearchSpace searchSpace, TestFunctionProblem problem) => [];
    }

    private sealed record BoundEvaluator : StatelessEvaluator<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>
    {
        public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<RealVector> candidates, IRandomNumberGenerator random, BoundedRealVectorSearchSpace searchSpace, TestFunctionProblem problem) => [];
    }

    private sealed record BoundMutator : StatelessMutator<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>
    {
        public override IReadOnlyList<RealVector> Mutate(IReadOnlyList<RealVector> parents, IRandomNumberGenerator random, BoundedRealVectorSearchSpace searchSpace, TestFunctionProblem problem) => [];
    }

    private sealed record BoundRefiner : StatelessRefiner<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>
    {
        public override IReadOnlyList<RealVector> Refine(IReadOnlyList<RealVector> candidates, IRandomNumberGenerator random, BoundedRealVectorSearchSpace searchSpace, TestFunctionProblem problem) => [];
    }

    private sealed record BoundReplacer : StatelessReplacer<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>
    {
        public override IReadOnlyList<EvaluatedCandidate<RealVector>> Replace(IReadOnlyList<EvaluatedCandidate<RealVector>> previousPopulation, IReadOnlyList<EvaluatedCandidate<RealVector>> offspringPopulation, ObjectiveDirections objective, int count, IRandomNumberGenerator random, BoundedRealVectorSearchSpace searchSpace, TestFunctionProblem problem) => [];
    }

    private sealed record BoundSelector : StatelessSelector<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>
    {
        public override IReadOnlyList<EvaluatedCandidate<RealVector>> Select(IReadOnlyList<EvaluatedCandidate<RealVector>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random, BoundedRealVectorSearchSpace searchSpace, TestFunctionProblem problem) => [];
    }

    private sealed record BoundTerminator : StatelessTerminator<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, SingleSolutionState<RealVector>>
    {
        public override bool IsTerminalState(SingleSolutionState<RealVector> state, BoundedRealVectorSearchSpace searchSpace, TestFunctionProblem problem) => false;
    }

    private sealed record BoundInterceptor : StatelessInterceptor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, SingleSolutionState<RealVector>>
    {
        public override SingleSolutionState<RealVector> Transform(SingleSolutionState<RealVector> currentState, SingleSolutionState<RealVector>? previousState, IRandomNumberGenerator random, BoundedRealVectorSearchSpace searchSpace, TestFunctionProblem problem) => currentState;
    }
}
