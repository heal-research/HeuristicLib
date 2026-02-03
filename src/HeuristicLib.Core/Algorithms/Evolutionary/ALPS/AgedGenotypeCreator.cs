using HEAL.HeuristicLib.Operators.Creator;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Algorithms.ALPS;

public class AgedGenotypeCreator<TGenotype, TEncoding, TProblem>(ICreator<TGenotype, TEncoding, TProblem> internalCreator) : ICreator<AgedGenotype<TGenotype>, AgedSearchSpace<TGenotype, TEncoding>, AgedProblem<TGenotype, TEncoding, TProblem>>
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : class, IProblem<TGenotype, TEncoding> {
  public IReadOnlyList<AgedGenotype<TGenotype>> Create(int count, IRandomNumberGenerator random, AgedSearchSpace<TGenotype, TEncoding> searchSpace, AgedProblem<TGenotype, TEncoding, TProblem> problem) {
    var offspring = new AgedGenotype<TGenotype>[count];
    var genotypes = internalCreator.Create(count, random, searchSpace.InnerEncoding, problem.InnerProblem);
    for (var i = 0; i < count; i++) {
      offspring[i] = new AgedGenotype<TGenotype>(genotypes[i], 0);
    }

    return offspring;
  }
}
