using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

public sealed record PipelineMutator<TCandidate> : MultiOperator<Mutation, TCandidate>
{
    public PipelineMutator(IReadOnlyList<IOperator<Mutation, TCandidate>> childMutators)
        : base(childMutators)
    {
    }

    protected override OperatorExecutorFactory CreateOperatorFactory() => new Factory(this);

    private sealed class Factory(PipelineMutator<TCandidate> source) : OperatorExecutorFactory<Mutation, TCandidate>
    {
        public override ExecutorFactory GetExecutorFactory<TRunSearchSpace, TRunProblem, TRunSearchState>(
            OperationExecutorAdapter<Mutation, TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState> adapter) =>
            ExecutorFactory.FromFactory<IMutationExecutor<TCandidate, TRunSearchSpace, TRunProblem>>(scope =>
            {
                var typed = scope.For<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState>();
                ImmutableArray<IMutationExecutor<TCandidate, TRunSearchSpace, TRunProblem>> childMutators = [.. source.ChildOperators.Select(child => typed.Resolve(child))];
                return new Executor<TRunSearchSpace, TRunProblem>(childMutators);
            });
    }

    private sealed class Executor<TSearchSpace, TProblem>(ImmutableArray<IMutationExecutor<TCandidate, TSearchSpace, TProblem>> childMutators)
        : MultiMutationExecutor<TCandidate, TSearchSpace, TProblem>(childMutators)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public override IReadOnlyList<TCandidate> Mutate(IReadOnlyList<TCandidate> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            var current = parents;
            foreach (var mutator in ChildMutators)
            {
                current = mutator.Mutate(current, random, searchSpace, problem);
            }

            return current;
        }
    }
}

public static class PipelineMutator
{
    public static PipelineMutator<TCandidate> Create<TCandidate>(params IReadOnlyList<IOperator<Mutation, TCandidate>> childMutators) => new(childMutators);
}

public static class PipelineMutatorExtensions
{
    extension<TCandidate>(IOperator<Mutation, TCandidate> mutator)
    {
        public PipelineMutator<TCandidate> Then(params IReadOnlyList<IOperator<Mutation, TCandidate>> followingMutators) =>
            PipelineMutator.Create([mutator, .. followingMutators]);
    }
}
