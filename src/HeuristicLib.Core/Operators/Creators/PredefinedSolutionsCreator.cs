using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Creators;

public class PredefinedISolutionsCreator<TGenotype, TSearchSpace, TProblem>(IReadOnlyList<TGenotype> predefinedISolutions, ICreator<TGenotype, TSearchSpace, TProblem> creatorForRemainingISolutions)
  : Creator<TGenotype, TSearchSpace, TProblem>
  where TGenotype : class
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace> {
  private int currentSolutionIndex;

  public override IReadOnlyList<TGenotype> Create(int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) {
    var offspring = new TGenotype[count];

    var countPredefined = Math.Min(predefinedISolutions.Count - currentSolutionIndex, count);
    if (countPredefined > 0) {
      for (var i = 0; i < countPredefined; i++) {
        offspring[i] = predefinedISolutions[currentSolutionIndex + i];
      }

      currentSolutionIndex += countPredefined;
    }

    var countRemaining = count - countPredefined;
    if (countRemaining <= 0)
      return offspring;
    var remainingRandom = random;//random.Fork(); //random.Fork("remaining");
    var remaining = creatorForRemainingISolutions.Create(countRemaining, remainingRandom, searchSpace, problem);
    for (var i = 0; i < remaining.Count; i++) {
      offspring[countPredefined + i] = remaining[i];
    }

    return offspring;
  }
}
