using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Algorithms.Evolutionary.ALPS;

public class AgedGenotypeCreator<TGenotype, TSearchSpace, TProblem>(ICreator<TGenotype, TSearchSpace, TProblem> internalCreator) : ICreator<AgedGenotype<TGenotype>, AgedSearchSpace<TGenotype, TSearchSpace>, AgedProblem<TGenotype, TSearchSpace, TProblem>>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  public IReadOnlyList<AgedGenotype<TGenotype>> Create(int count, IRandomNumberGenerator random, AgedSearchSpace<TGenotype, TSearchSpace> searchSpace, AgedProblem<TGenotype, TSearchSpace, TProblem> problem)
  {
    var offspring = new AgedGenotype<TGenotype>[count];
    var genotypes = internalCreator.Create(count, random, searchSpace.InnerEncoding, problem.InnerProblem);
    for (var i = 0; i < count; i++) {
      offspring[i] = new AgedGenotype<TGenotype>(genotypes[i], 0);
    }

    return offspring;
  }
}
