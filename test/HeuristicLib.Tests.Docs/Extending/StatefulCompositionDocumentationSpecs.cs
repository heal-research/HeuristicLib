#region stateful-composition-imports
using System.Collections.Immutable;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
#endregion stateful-composition-imports

namespace HEAL.HeuristicLib.Tests.Docs.Extending;

#region stateful-composition-type
public sealed record RoundRobinMutator<TCandidate> : MultiOperator<Mutation, TCandidate>
{
    public RoundRobinMutator(IReadOnlyList<IOperator<Mutation, TCandidate>> childMutators)
        : base(childMutators)
    {
    }

    protected override IExecutor CreateExecutor<TSearchSpace, TProblem, TSearchState>(
        ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState> scope)
    {
        if (ChildOperators.Count == 0)
            throw new InvalidOperationException("At least one mutator is required.");
        ImmutableArray<IMutationExecutor<TCandidate, TSearchSpace, TProblem>> children =
            [.. ChildOperators.Select(child => scope.Resolve(child))];
        return new Executor<TSearchSpace, TProblem>(children);
    }

    private sealed class Executor<TSearchSpace, TProblem>(
        ImmutableArray<IMutationExecutor<TCandidate, TSearchSpace, TProblem>> children)
        : IMutationExecutor<TCandidate, TSearchSpace, TProblem>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        private int nextChild;

        public IReadOnlyList<TCandidate> Mutate(
            IReadOnlyList<TCandidate> parents,
            IRandomNumberGenerator random,
            TSearchSpace searchSpace,
            TProblem problem)
        {
            var offspring = children[nextChild].Mutate(parents, random, searchSpace, problem);
            nextChild = (nextChild + 1) % children.Length;
            return offspring;
        }
    }
}
#endregion stateful-composition-type

public sealed class StatefulCompositionDocumentationSpecs
{
    [Fact]
    public void AddingInspectionConstructsAnIndependentCursorAndKeepsExistingBindings()
    {
        #region stateful-composition-usage
        var first = new NoChangeMutator<RealVector>();
        var second = new NoChangeMutator<RealVector>();
        var composition = new RoundRobinMutator<RealVector>([first, second]);
        var searchSpace = new BoundedRealVectorSearchSpace(1, -10.0, 10.0);
        var problem = FuncProblem.Create(
            (RealVector candidate) => candidate[0] * candidate[0],
            searchSpace,
            SingleObjective.Minimize);
        var random = RandomNumberGenerator.Create(seed: 42);
        IReadOnlyList<RealVector> parents = [new RealVector(0.0)];

        var scope = ResolutionScope.Create();
        var original = scope.For<
            RealVector,
            BoundedRealVectorSearchSpace,
            IProblem<RealVector, BoundedRealVectorSearchSpace>>()
            .Resolve(composition);
        original.Mutate(parents, random, searchSpace, problem); // First child.

        var firstCalls = first.Invocations.Count();
        var secondCalls = second.Invocations.Count();
        var observedScope = scope.CreateChildScope();
        observedScope.AddAspect(observedScope.Results.Aspect(firstCalls));
        observedScope.AddAspect(observedScope.Results.Aspect(secondCalls));
        var observed = observedScope.For<
            RealVector,
            BoundedRealVectorSearchSpace,
            IProblem<RealVector, BoundedRealVectorSearchSpace>>()
            .Resolve(composition);

        observed.Mutate(parents, random, searchSpace, problem); // First child with its own cursor.
        original.Mutate(parents, random, searchSpace, problem); // Second child with the original cursor.
        #endregion stateful-composition-usage

        observedScope.Results.Result(firstCalls).CurrentCount.ShouldBe(1);
        observedScope.Results.Result(secondCalls).CurrentCount.ShouldBe(0);

        #region stateful-composition-independent
        var independentFirstCalls = first.Invocations.Count();
        var independentScope = ResolutionScope.Create();
        independentScope.AddAspect(independentScope.Results.Aspect(independentFirstCalls));
        var independent = independentScope.For<
            RealVector,
            BoundedRealVectorSearchSpace,
            IProblem<RealVector, BoundedRealVectorSearchSpace>>()
            .Resolve(composition);
        independent.Mutate(parents, random, searchSpace, problem); // First child with a fresh cursor.
        #endregion stateful-composition-independent

        independentScope.Results.Result(independentFirstCalls).CurrentCount.ShouldBe(1);
        observedScope.Results.Result(secondCalls).CurrentCount.ShouldBe(0);
    }
}
