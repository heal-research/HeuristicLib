using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.Creator;

public class PredefinedISolutionsCreator<TGenotype, TEncoding, TProblem>(IReadOnlyList<TGenotype> predefinedISolutions, ICreator<TGenotype, TEncoding, TProblem> creatorForRemainingISolutions)
  : BatchCreator<TGenotype, TEncoding, TProblem>
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : class, IProblem<TGenotype, TEncoding> {
  private int currentISolutionIndex;

  public override IReadOnlyList<TGenotype> Create(int count, IRandomNumberGenerator random, TEncoding encoding, TProblem problem) {
    var offspring = new TGenotype[count];

    var countPredefined = Math.Min(predefinedISolutions.Count - currentISolutionIndex, count);
    if (countPredefined > 0) {
      for (var i = 0; i < countPredefined; i++) {
        offspring[i] = predefinedISolutions[currentISolutionIndex + i];
      }

      currentISolutionIndex += countPredefined;
    }

    var countRemaining = count - countPredefined;
    if (countRemaining <= 0)
      return offspring;
    var remainingRandom = random.Spawn(); //random.Fork("remaining");
    var remaining = creatorForRemainingISolutions.Create(countRemaining, remainingRandom, encoding, problem);
    for (var i = 0; i < remaining.Count; i++) {
      offspring[countPredefined + i] = remaining[i];
    }

    return offspring;
  }
}
