using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

public sealed record PipelineMutator<TCandidate> : MultiMutator<TCandidate>
{
    public PipelineMutator(IReadOnlyList<IMutator<TCandidate>> childMutators)
        : base(childMutators)
    {
    }

    protected override IMutatorInstance<TCandidate, TRunSearchSpace, TRunProblem> CombineExecutionInstances<TRunSearchSpace, TRunProblem>(ImmutableArray<IMutatorInstance<TCandidate, TRunSearchSpace, TRunProblem>> childMutators) =>
        new Instance<TRunSearchSpace, TRunProblem>(childMutators);

    private sealed class Instance<TSearchSpace, TProblem>(ImmutableArray<IMutatorInstance<TCandidate, TSearchSpace, TProblem>> childMutators)
        : MultiMutatorInstance<TCandidate, TSearchSpace, TProblem>(childMutators)
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
    public static PipelineMutator<TCandidate> Create<TCandidate>(params IReadOnlyList<IMutator<TCandidate>> childMutators) => new(childMutators);
}

public static class PipelineMutatorExtensions
{
    extension<TCandidate>(IMutator<TCandidate> mutator)
    {
        public PipelineMutator<TCandidate> Then(params IReadOnlyList<IMutator<TCandidate>> followingMutators) =>
            PipelineMutator.Create([mutator, .. followingMutators]);
    }
}
