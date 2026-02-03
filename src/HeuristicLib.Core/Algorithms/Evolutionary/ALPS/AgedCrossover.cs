using HEAL.HeuristicLib.Operators.Crossovers;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Algorithms.Evolutionary.ALPS;

public class AgedCrossover<TGenotype, TSearchSpace, TProblem>(ICrossover<TGenotype, TSearchSpace, TProblem> internalCrossover)
  : ICrossover<AgedGenotype<TGenotype>, AgedSearchSpace<TGenotype, TSearchSpace>, AgedProblem<TGenotype, TSearchSpace, TProblem>>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  public IReadOnlyList<AgedGenotype<TGenotype>> Cross(IReadOnlyList<IParents<AgedGenotype<TGenotype>>> parents, IRandomNumberGenerator random, AgedSearchSpace<TGenotype, TSearchSpace> searchSpace, AgedProblem<TGenotype, TSearchSpace, TProblem> problem)
  {
    var innerParents = new IParents<TGenotype>[parents.Count];
    for (var i = 0; i < parents.Count; i++) {
      innerParents[i] = new Parents<TGenotype>(parents[i].Item1.InnerGenotype, parents[i].Item2.InnerGenotype);
    }

    var offspring = internalCrossover.Cross(innerParents, random, searchSpace.InnerEncoding, problem.InnerProblem);
    var result = new AgedGenotype<TGenotype>[offspring.Count];
    for (var i = 0; i < offspring.Count; i++) {
      var newAge = Math.Max(parents[i].Item1.Age, parents[i].Item2.Age) + 1;
      result[i] = new AgedGenotype<TGenotype>(offspring[i], newAge);
    }

    return result;
  }
}
