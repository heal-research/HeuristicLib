using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Operators.Creator;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Algorithms.ALPS;

public class AgedGenotypeCreator<TGenotype, TEncoding, TProblem>(ICreator<TGenotype, TEncoding, TProblem> internalCreator) : ICreator<AgedGenotype<TGenotype>, AgedEncoding<TGenotype, TEncoding>, AgedProblem<TGenotype, TEncoding, TProblem>>
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : class, IProblem<TGenotype, TEncoding> {
  public IReadOnlyList<AgedGenotype<TGenotype>> Create(int count, IRandomNumberGenerator random, AgedEncoding<TGenotype, TEncoding> encoding, AgedProblem<TGenotype, TEncoding, TProblem> problem) {
    var offspring = new AgedGenotype<TGenotype>[count];
    var genotypes = internalCreator.Create(count, random, encoding.InnerEncoding, problem.InnerProblem);
    for (int i = 0; i < count; i++) {
      offspring[i] = new AgedGenotype<TGenotype>(genotypes[i], 0);
    }

    return offspring;
  }
}
