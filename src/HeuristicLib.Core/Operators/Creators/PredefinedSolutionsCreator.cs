using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Creators;

public class PredefinedSolutionsCreator<TGenotype, TSearchSpace, TProblem>
  : Creator<TGenotype, TSearchSpace, TProblem>
  where TGenotype : class
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  private readonly IReadOnlyList<TGenotype> predefinedSolutions;
  private readonly ICreator<TGenotype, TSearchSpace, TProblem> creatorForRemainingSolutions;
  
  public PredefinedSolutionsCreator(IReadOnlyList<TGenotype> predefinedSolutions, ICreator<TGenotype, TSearchSpace, TProblem> creatorForRemainingSolutions)
  {
    this.predefinedSolutions = predefinedSolutions;
    this.creatorForRemainingSolutions = creatorForRemainingSolutions;
  }

  public override PredefinedSolutionsCreatorInstance<TGenotype, TSearchSpace, TProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry)
  {
    var creatorForRemainingSolutionsInstance = instanceRegistry.GetOrAdd(creatorForRemainingSolutions, () => this.creatorForRemainingSolutions.CreateExecutionInstance(instanceRegistry));
    return new PredefinedSolutionsCreatorInstance<TGenotype, TSearchSpace, TProblem>(this.predefinedSolutions, creatorForRemainingSolutionsInstance);
  }
}

public class PredefinedSolutionsCreatorInstance<TGenotype, TSearchSpace, TProblem>
  : CreatorInstance<TGenotype, TSearchSpace, TProblem>
  where TGenotype : class
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  private int currentSolutionIndex;
  
  private readonly IReadOnlyList<TGenotype> predefinedSolutions;
  private readonly ICreatorInstance<TGenotype, TSearchSpace, TProblem> creatorForRemainingSolutionsInstance;
    
  public PredefinedSolutionsCreatorInstance(IReadOnlyList<TGenotype> predefinedSolutions, ICreatorInstance<TGenotype, TSearchSpace, TProblem> creatorForRemainingSolutionsInstance)
  {
    this.predefinedSolutions = predefinedSolutions;
    this.creatorForRemainingSolutionsInstance = creatorForRemainingSolutionsInstance;
  }
  
  public override IReadOnlyList<TGenotype> Create(int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
  {
    var offspring = new TGenotype[count];

    var countPredefined = Math.Min(predefinedSolutions.Count - currentSolutionIndex, count);
    if (countPredefined > 0) {
      for (var i = 0; i < countPredefined; i++) {
        offspring[i] = predefinedSolutions[currentSolutionIndex + i];
      }

      currentSolutionIndex += countPredefined;
    }

    var countRemaining = count - countPredefined;
    if (countRemaining <= 0) {
      return offspring;
    }

    var remainingRandom = random.Fork(1); 
    var remaining = creatorForRemainingSolutionsInstance.Create(countRemaining, remainingRandom, searchSpace, problem);
    for (var i = 0; i < remaining.Count; i++) {
      offspring[countPredefined + i] = remaining[i];
    }

    return offspring;
  }
}


