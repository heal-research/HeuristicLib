using Generator.Equals;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Creators;

[Equatable]
public partial record PredefinedSolutionsCreator<TGenotype, TSearchSpace, TProblem>
  : StatefulCreator<TGenotype, TSearchSpace, TProblem, PredefinedSolutionsCreator<TGenotype, TSearchSpace, TProblem>.State>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  [OrderedEquality] public ImmutableArray<TGenotype> PredefinedSolutions { get; init; }

  public ICreator<TGenotype, TSearchSpace, TProblem> CreatorForRemainingSolutions { get; init; }

  public PredefinedSolutionsCreator(ImmutableArray<TGenotype> predefinedSolutions, ICreator<TGenotype, TSearchSpace, TProblem> creatorForRemainingSolutions)
  {
    PredefinedSolutions = predefinedSolutions;
    CreatorForRemainingSolutions = creatorForRemainingSolutions;
  }

  protected override State CreateInitialState(ExecutionInstanceRegistry instanceRegistry) 
    => new (instanceRegistry.Resolve(CreatorForRemainingSolutions));
  
  protected override IReadOnlyList<TGenotype> Create(int count, State state, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
  {
    var offspring = new TGenotype[count];

    var countPredefined = Math.Min(PredefinedSolutions.Length - state.CurrentSolutionIndex, count);
    if (countPredefined > 0) {
      for (var i = 0; i < countPredefined; i++) {
        offspring[i] = PredefinedSolutions[state.CurrentSolutionIndex + i];
      }

      state.CurrentSolutionIndex += countPredefined;
    }

    var countRemaining = count - countPredefined;
    if (countRemaining <= 0) {
      return offspring;
    }

    var remainingRandom = random.Fork(1);
    var remaining = state.CreatorForRemainingSolutionsInstance.Create(countRemaining, remainingRandom, searchSpace, problem);
    for (var i = 0; i < remaining.Count; i++) {
      offspring[countPredefined + i] = remaining[i];
    }

    return offspring;
  }

  public sealed class State(ICreatorInstance<TGenotype, TSearchSpace, TProblem> creatorForRemainingSolutionsInstance)
  {
    public int CurrentSolutionIndex { get; set; } = 0;
    public ICreatorInstance<TGenotype, TSearchSpace, TProblem> CreatorForRemainingSolutionsInstance { get; } = creatorForRemainingSolutionsInstance;
  }
}
