using Generator.Equals;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Creators;

/// <summary>
/// Emits predefined candidates across successive calls before delegating remaining requests to a fallback creator.
/// </summary>
[Equatable]
public partial record PredefinedCandidatesCreator<TCandidate, TSearchSpace, TProblem>
    : WrappingCreator<TCandidate, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public ICreator<TCandidate, TSearchSpace, TProblem> CreatorForRemainingCandidates => InnerCreator;

    [OrderedEquality] public ImmutableArray<TCandidate> PredefinedCandidates { get; init; }

    public PredefinedCandidatesCreator(ImmutableArray<TCandidate> predefinedCandidates, ICreator<TCandidate, TSearchSpace, TProblem> creatorForRemainingCandidates)
      : base(creatorForRemainingCandidates)
    {
        PredefinedCandidates = predefinedCandidates;
    }

    protected override WrappingCreatorInstance<TCandidate, TSearchSpace, TProblem> CreateCreatorInstance(ICreatorInstance<TCandidate, TSearchSpace, TProblem> innerCreator) =>
        new Instance(innerCreator, PredefinedCandidates);

    private sealed class Instance(ICreatorInstance<TCandidate, TSearchSpace, TProblem> innerCreator, ImmutableArray<TCandidate> predefinedCandidates)
        : WrappingCreatorInstance<TCandidate, TSearchSpace, TProblem>(innerCreator)
    {
        private int currentCandidateIndex;

        public override IReadOnlyList<TCandidate> Create(int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            var offspring = new TCandidate[count];
            var countPredefined = Math.Min(predefinedCandidates.Length - currentCandidateIndex, count);
            if (countPredefined > 0)
            {
                for (var i = 0; i < countPredefined; i++)
                {
                    offspring[i] = predefinedCandidates[currentCandidateIndex + i];
                }

                currentCandidateIndex += countPredefined;
            }

            var countRemaining = count - countPredefined;
            if (countRemaining <= 0)
            {
                return offspring;
            }

            var remaining = InnerCreator.Create(countRemaining, random, searchSpace, problem);
            for (var i = 0; i < remaining.Count; i++)
            {
                offspring[countPredefined + i] = remaining[i];
            }

            return offspring;
        }
    }
}
