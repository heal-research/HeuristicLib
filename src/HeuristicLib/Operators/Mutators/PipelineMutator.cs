using Generator.Equals;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Mutators;

[Equatable]
public sealed partial record PipelineMutator<TCandidate, TSearchSpace, TProblem>
    : MultiMutator<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public PipelineMutator(IReadOnlyList<IMutator<TCandidate, TSearchSpace, TProblem>> childMutators)
        : base(childMutators)
    {
    }

    protected override MultiMutatorInstance<TCandidate, TSearchSpace, TProblem> CreateExecutionInstance(ImmutableArray<IMutatorInstance<TCandidate, TSearchSpace, TProblem>> childMutators) =>
        new Instance(childMutators);

    private sealed class Instance(ImmutableArray<IMutatorInstance<TCandidate, TSearchSpace, TProblem>> childMutators)
        : MultiMutatorInstance<TCandidate, TSearchSpace, TProblem>(childMutators)
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
    public static PipelineMutator<TCandidate, TSearchSpace, TProblem> Create<TCandidate, TSearchSpace, TProblem>(params IReadOnlyList<IMutator<TCandidate, TSearchSpace, TProblem>> childMutators)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> => new(childMutators);
}

public static class PipelineMutatorExtensions
{
    extension<TCandidate, TSearchSpace, TProblem>(IMutator<TCandidate, TSearchSpace, TProblem> mutator)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public PipelineMutator<TCandidate, TSearchSpace, TProblem> Then(params IReadOnlyList<IMutator<TCandidate, TSearchSpace, TProblem>> followingMutators) =>
            PipelineMutator.Create([mutator, .. followingMutators]);
    }
}
