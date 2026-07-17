using Generator.Equals;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Mutators;

[Equatable]
public partial record PipelineMutator<TCandidate, TSearchSpace, TProblem>
  : MultiMutator<TCandidate, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    [IgnoreEquality] public ImmutableArray<IMutator<TCandidate, TSearchSpace, TProblem>> Mutators => InnerMutators;

    public PipelineMutator(ImmutableArray<IMutator<TCandidate, TSearchSpace, TProblem>> mutators)
      : base(mutators)
    {
        // ToDo: think if we want to allow empty pipelines.
        if (mutators.Length == 0)
        {
            throw new ArgumentException("At least one mutator must be provided.", nameof(mutators));
        }
    }

    protected override MultiMutatorInstance<TCandidate, TSearchSpace, TProblem> CreateMutatorInstance(
        ImmutableArray<IMutatorInstance<TCandidate, TSearchSpace, TProblem>> innerMutators) => new Instance(innerMutators);

    private sealed class Instance(ImmutableArray<IMutatorInstance<TCandidate, TSearchSpace, TProblem>> innerMutators)
        : MultiMutatorInstance<TCandidate, TSearchSpace, TProblem>(innerMutators)
    {
        public override IReadOnlyList<TCandidate> Mutate(IReadOnlyList<TCandidate> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            var current = parents;
            foreach (var mutator in InnerMutators)
            {
                current = mutator.Mutate(current, random, searchSpace, problem);
            }

            return current;
        }
    }
}
