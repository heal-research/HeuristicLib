using Generator.Equals;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Creators;

[Equatable]
public partial record PredefinedSolutionsCreator<TGenotype, TSearchSpace, TProblem>
  : DecoratorCreator<TGenotype, TSearchSpace, TProblem, PredefinedSolutionsCreator<TGenotype, TSearchSpace, TProblem>.State>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  public ICreator<TGenotype, TSearchSpace, TProblem> CreatorForRemainingSolutions => InnerCreator;
  
  [OrderedEquality] public ImmutableArray<TGenotype> PredefinedSolutions { get; init; }

  public PredefinedSolutionsCreator(ImmutableArray<TGenotype> predefinedSolutions, ICreator<TGenotype, TSearchSpace, TProblem> creatorForRemainingSolutions)
    : base(creatorForRemainingSolutions)
  { 
    PredefinedSolutions = predefinedSolutions;
  }

  protected override State CreateInitialState() => new ();
  
  protected override IReadOnlyList<TGenotype> Create(int count, State state, ICreatorInstance<TGenotype, TSearchSpace, TProblem> innerCreator, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
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
    var remaining = innerCreator.Create(countRemaining, remainingRandom, searchSpace, problem);
    for (var i = 0; i < remaining.Count; i++) {
      offspring[countPredefined + i] = remaining[i];
    }

    return offspring;
  }

  public new sealed class State {
    public int CurrentSolutionIndex { get; set; } = 0;
  }
}
