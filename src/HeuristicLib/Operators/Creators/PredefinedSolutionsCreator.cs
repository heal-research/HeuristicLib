using Generator.Equals;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Creators;

[Equatable]
public partial record PredefinedSolutionsCreator<TCandidate, TSearchSpace, TProblem>
  : WrappingCreator<TCandidate, TSearchSpace, TProblem, PredefinedSolutionsCreator<TCandidate, TSearchSpace, TProblem>.ExecutionState>
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public ICreator<TCandidate, TSearchSpace, TProblem> CreatorForRemainingSolutions => InnerCreator;

    [OrderedEquality] public ImmutableArray<TCandidate> PredefinedSolutions { get; init; }

    public PredefinedSolutionsCreator(ImmutableArray<TCandidate> predefinedSolutions, ICreator<TCandidate, TSearchSpace, TProblem> creatorForRemainingSolutions)
      : base(creatorForRemainingSolutions)
    {
        PredefinedSolutions = predefinedSolutions;
    }

    protected override ExecutionState CreateInitialState() => new();

    protected override IReadOnlyList<TCandidate> Create(int count, ExecutionState executionState, InnerCreate innerCreate, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
    {
        var offspring = new TCandidate[count];

        var countPredefined = Math.Min(PredefinedSolutions.Length - executionState.CurrentSolutionIndex, count);
        if (countPredefined > 0)
        {
            for (var i = 0; i < countPredefined; i++)
            {
                offspring[i] = PredefinedSolutions[executionState.CurrentSolutionIndex + i];
            }

            executionState.CurrentSolutionIndex += countPredefined;
        }

        var countRemaining = count - countPredefined;
        if (countRemaining <= 0)
        {
            return offspring;
        }

        var remainingRandom = random.Fork(1);
        var remaining = innerCreate(countRemaining, remainingRandom, searchSpace, problem);
        for (var i = 0; i < remaining.Count; i++)
        {
            offspring[countPredefined + i] = remaining[i];
        }

        return offspring;
    }

    public sealed class ExecutionState
    {
        public int CurrentSolutionIndex { get; set; } = 0;
    }
}
