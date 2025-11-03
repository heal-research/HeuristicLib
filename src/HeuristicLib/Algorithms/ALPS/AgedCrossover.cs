using HEAL.HeuristicLib.Operators.Crossover;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Algorithms.ALPS;

public class AgedCrossover<TGenotype, TEncoding, TProblem>(ICrossover<TGenotype, TEncoding, TProblem> internalCrossover)
  : ICrossover<AgedGenotype<TGenotype>, AgedEncoding<TGenotype, TEncoding>, AgedProblem<TGenotype, TEncoding, TProblem>>
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : class, IProblem<TGenotype, TEncoding> {
  public IReadOnlyList<AgedGenotype<TGenotype>> Cross(IReadOnlyList<(AgedGenotype<TGenotype>, AgedGenotype<TGenotype>)> parents, IRandomNumberGenerator random, AgedEncoding<TGenotype, TEncoding> encoding, AgedProblem<TGenotype, TEncoding, TProblem> problem) {
    var innerParents = new (TGenotype, TGenotype)[parents.Count];
    for (int i = 0; i < parents.Count; i++) {
      innerParents[i] = (parents[i].Item1.InnerGenotype, parents[i].Item2.InnerGenotype);
    }

    var offspring = internalCrossover.Cross(innerParents, random, encoding.InnerEncoding, problem.InnerProblem);
    var result = new AgedGenotype<TGenotype>[offspring.Count];
    for (int i = 0; i < offspring.Count; i++) {
      int newAge = Math.Max(parents[i].Item1.Age, parents[i].Item2.Age) + 1;
      result[i] = new AgedGenotype<TGenotype>(offspring[i], newAge);
    }

    return result;
  }
}
