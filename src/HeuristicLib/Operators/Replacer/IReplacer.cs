using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.Replacer;

public interface IReplacer<TGenotype> : IReplacer<TGenotype, IEncoding<TGenotype>> {
  IReadOnlyList<Solution<TGenotype>> Replace(IReadOnlyList<Solution<TGenotype>> previousPopulation, IReadOnlyList<Solution<TGenotype>> offspringPopulation, Objective objective, IRandomNumberGenerator random);
}

public interface IReplacer<TGenotype, in TEncoding> : IReplacer<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>>
  where TEncoding : class, IEncoding<TGenotype> {
  IReadOnlyList<Solution<TGenotype>> Replace(IReadOnlyList<Solution<TGenotype>> previousPopulation, IReadOnlyList<Solution<TGenotype>> offspringPopulation, Objective objective, IRandomNumberGenerator random, TEncoding encoding);
}

public interface IReplacer<TGenotype, in TEncoding, in TProblem>
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : class, IProblem<TGenotype, TEncoding> {
  IReadOnlyList<Solution<TGenotype>> Replace(IReadOnlyList<Solution<TGenotype>> previousPopulation, IReadOnlyList<Solution<TGenotype>> offspringPopulation, Objective objective, IRandomNumberGenerator random, TEncoding encoding, TProblem problem);
  int GetOffspringCount(int populationSize);
}
