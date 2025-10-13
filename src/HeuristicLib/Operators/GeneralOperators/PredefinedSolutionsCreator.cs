using HEAL.HeuristicLib.Operators.BatchOperators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators;

public class PredefinedSolutionsCreator<TGenotype, TEncoding, TProblem>(IReadOnlyList<TGenotype> predefinedSolutions, ICreator<TGenotype, TEncoding, TProblem> creatorForRemainingSolutions)
  : BatchCreator<TGenotype, TEncoding, TProblem>
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : class, IProblem<TGenotype, TEncoding> {
  private int currentSolutionIndex;

  public override IReadOnlyList<TGenotype> Create(int count, IRandomNumberGenerator random, TEncoding encoding, TProblem problem) {
    var offspring = new TGenotype[count];

    var countPredefined = Math.Min(predefinedSolutions.Count - currentSolutionIndex, count);
    if (countPredefined > 0) {
      for (var i = 0; i < countPredefined; i++) {
        offspring[i] = predefinedSolutions[currentSolutionIndex + i];
      }

      currentSolutionIndex += countPredefined;
    }

    var countRemaining = count - countPredefined;
    if (countRemaining <= 0)
      return offspring;
    var remainingRandom = random.Fork(1); //random.Fork("remaining");
    var remaining = creatorForRemainingSolutions.Create(countRemaining, remainingRandom, encoding, problem);
    for (var i = 0; i < remaining.Count; i++) {
      offspring[countPredefined + i] = remaining[i];
    }

    return offspring;
  }
}
