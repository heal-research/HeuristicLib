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

    protected override IReadOnlyList<TCandidate> Mutate(IReadOnlyList<TCandidate> parents,
      IReadOnlyList<InnerMutate> innerMutators, IRandomNumberGenerator random, TSearchSpace searchSpace,
      TProblem problem)
    {
        var current = parents;
        foreach (var mutator in innerMutators)
        {
            current = mutator(current, random, searchSpace, problem);
        }
        return current;
    }
}
