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

    protected override OperatorExecutorFactory CreateOperatorFactory()
    {
        if (ChildOperators.Count == 0)
            throw new InvalidOperationException("At least one mutator is required.");
        return new Factory(this, new ExecutionState());
    }

    private sealed class Factory(RoundRobinMutator<TCandidate> source, ExecutionState state)
        : OperatorExecutorFactory<Mutation, TCandidate>
    {
        public override ExecutorFactory GetExecutorFactory<TSearchSpace, TProblem, TSearchState>(
            OperationExecutorAdapter<Mutation, TCandidate, TSearchSpace, TProblem, TSearchState> adapter) =>
            ExecutorFactory.FromFactory<IMutationExecutor<TCandidate, TSearchSpace, TProblem>>(scope =>
            {
                var typed = scope.For<TCandidate, TSearchSpace, TProblem, TSearchState>();
                ImmutableArray<IMutationExecutor<TCandidate, TSearchSpace, TProblem>> children =
                    [.. source.ChildOperators.Select(child => typed.Resolve(child))];
                return new Executor<TSearchSpace, TProblem>(children, state);
            });
    }

    private sealed class ExecutionState
    {
        public int NextChild { get; set; }
    }

    private sealed class Executor<TSearchSpace, TProblem>(
        ImmutableArray<IMutationExecutor<TCandidate, TSearchSpace, TProblem>> children,
        ExecutionState state)
        : MultiMutationExecutor<TCandidate, TSearchSpace, TProblem>(children)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public override IReadOnlyList<TCandidate> Mutate(
            IReadOnlyList<TCandidate> parents,
            IRandomNumberGenerator random,
            TSearchSpace searchSpace,
            TProblem problem)
        {
            var offspring = ChildMutators[state.NextChild].Mutate(parents, random, searchSpace, problem);
            state.NextChild = (state.NextChild + 1) % ChildMutators.Length;
            return offspring;
        }
    }
}
#endregion stateful-composition-type

public sealed class StatefulCompositionDocumentationSpecs
{
    [Fact]
    public void AddingInspectionKeepsTheNextChildAndExistingBindings()
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

        var firstCalls = new CountAccumulator();
        var secondCalls = new CountAccumulator();
        var observedScope = scope.With(
            Aspect.CountCalls(first, firstCalls),
            Aspect.CountCalls(second, secondCalls));
        var observed = observedScope.For<
            RealVector,
            BoundedRealVectorSearchSpace,
            IProblem<RealVector, BoundedRealVectorSearchSpace>>()
            .Resolve(composition);

        observed.Mutate(parents, random, searchSpace, problem); // Second child; the cursor was retained.
        original.Mutate(parents, random, searchSpace, problem); // First child; unobserved.
        #endregion stateful-composition-usage

        firstCalls.CurrentCount.ShouldBe(0);
        secondCalls.CurrentCount.ShouldBe(1);

        #region stateful-composition-independent
        var independentFirstCalls = new CountAccumulator();
        var independentScope = ResolutionScope.Create(Aspect.CountCalls(first, independentFirstCalls));
        var independent = independentScope.For<
            RealVector,
            BoundedRealVectorSearchSpace,
            IProblem<RealVector, BoundedRealVectorSearchSpace>>()
            .Resolve(composition);
        independent.Mutate(parents, random, searchSpace, problem); // First child; fresh cursor.
        #endregion stateful-composition-independent

        independentFirstCalls.CurrentCount.ShouldBe(1);
        secondCalls.CurrentCount.ShouldBe(1);
    }
}
