using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.Replacer;

public abstract class Replacer<TGenotype, TEncoding, TProblem> : IReplacer<TGenotype, TEncoding, TProblem>
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : class, IProblem<TGenotype, TEncoding> {
  public abstract IReadOnlyList<ISolution<TGenotype>> Replace(IReadOnlyList<ISolution<TGenotype>> previousPopulation, IReadOnlyList<ISolution<TGenotype>> offspringPopulation, Objective objective, IRandomNumberGenerator random, TEncoding encoding, TProblem problem);

  public abstract int GetOffspringCount(int populationSize);
}

public abstract class Replacer<TGenotype, TEncoding> : IReplacer<TGenotype, TEncoding>
  where TEncoding : class, IEncoding<TGenotype> {
  public abstract IReadOnlyList<ISolution<TGenotype>> Replace(IReadOnlyList<ISolution<TGenotype>> previousPopulation, IReadOnlyList<ISolution<TGenotype>> offspringPopulation, Objective objective, IRandomNumberGenerator random, TEncoding encoding);

  public abstract int GetOffspringCount(int populationSize);

  IReadOnlyList<ISolution<TGenotype>> IReplacer<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>>.Replace(IReadOnlyList<ISolution<TGenotype>> previousPopulation, IReadOnlyList<ISolution<TGenotype>> offspringPopulation, Objective objective, IRandomNumberGenerator random, TEncoding encoding, IProblem<TGenotype, TEncoding> problem) {
    return Replace(previousPopulation, offspringPopulation, objective, random, encoding);
  }
}

public abstract class Replacer<TGenotype> : IReplacer<TGenotype> {
  public abstract IReadOnlyList<ISolution<TGenotype>> Replace(IReadOnlyList<ISolution<TGenotype>> previousPopulation, IReadOnlyList<ISolution<TGenotype>> offspringPopulation, Objective objective, IRandomNumberGenerator random);

  public abstract int GetOffspringCount(int populationSize);

  IReadOnlyList<ISolution<TGenotype>> IReplacer<TGenotype, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>>.Replace(IReadOnlyList<ISolution<TGenotype>> previousPopulation, IReadOnlyList<ISolution<TGenotype>> offspringPopulation, Objective objective, IRandomNumberGenerator random, IEncoding<TGenotype> encoding, IProblem<TGenotype, IEncoding<TGenotype>> problem) {
    return Replace(previousPopulation, offspringPopulation, objective, random);
  }

  IReadOnlyList<ISolution<TGenotype>> IReplacer<TGenotype, IEncoding<TGenotype>>.Replace(IReadOnlyList<ISolution<TGenotype>> previousPopulation, IReadOnlyList<ISolution<TGenotype>> offspringPopulation, Objective objective, IRandomNumberGenerator random, IEncoding<TGenotype> encoding) {
    return Replace(previousPopulation, offspringPopulation, objective, random);
  }
}
