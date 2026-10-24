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

    protected override IExecutor CreateExecutor<TRunSearchSpace, TRunProblem, TRunSearchState>(
        ResolutionScope<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState> scope)
    {
        ImmutableArray<IMutationExecutor<TCandidate, TRunSearchSpace, TRunProblem>> childMutators = [.. ChildOperators.Select(child => scope.Resolve(child))];
        return new Executor<TRunSearchSpace, TRunProblem>(childMutators);
    }

    private sealed class Executor<TSearchSpace, TProblem>(ImmutableArray<IMutationExecutor<TCandidate, TSearchSpace, TProblem>> childMutators)
        : IMutationExecutor<TCandidate, TSearchSpace, TProblem>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public IReadOnlyList<TCandidate> Mutate(IReadOnlyList<TCandidate> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            var current = parents;
            foreach (var mutator in childMutators)
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
