using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.Selector;

public abstract class Selector<TGenotype, TEncoding, TProblem> : ISelector<TGenotype, TEncoding, TProblem>
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : class, IProblem<TGenotype, TEncoding> {
  public abstract ISolution<TGenotype> Select(IReadOnlyList<ISolution<TGenotype>> population, Objective objective, IRandomNumberGenerator random, TEncoding encoding, TProblem problem);

  public IReadOnlyList<ISolution<TGenotype>> Select(IReadOnlyList<ISolution<TGenotype>> population, Objective objective, int count, IRandomNumberGenerator random, TEncoding encoding, TProblem problem) {
    return Enumerable.Range(0, count).ParallelSelect(random, (_, _, r) => Select(population, objective, r, encoding, problem));
  }
}

public abstract class Selector<TGenotype, TEncoding> : ISelector<TGenotype, TEncoding>
  where TEncoding : class, IEncoding<TGenotype> {
  public abstract ISolution<TGenotype> Select(IReadOnlyList<ISolution<TGenotype>> population, Objective objective, IRandomNumberGenerator random, TEncoding encoding);

  public IReadOnlyList<ISolution<TGenotype>> Select(IReadOnlyList<ISolution<TGenotype>> population, Objective objective, int count, IRandomNumberGenerator random, TEncoding encoding) {
    return Enumerable.Range(0, count).ParallelSelect(random, (_, _, r) => Select(population, objective, r, encoding));
  }

  IReadOnlyList<ISolution<TGenotype>> ISelector<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>>.Select(IReadOnlyList<ISolution<TGenotype>> population, Objective objective, int count, IRandomNumberGenerator random, TEncoding encoding, IProblem<TGenotype, TEncoding> problem) {
    return Select(population, objective, count, random, encoding);
  }
}

public abstract class Selector<TGenotype> : ISelector<TGenotype> {
  public abstract ISolution<TGenotype> Select(IReadOnlyList<ISolution<TGenotype>> population, Objective objective, IRandomNumberGenerator random);

  public IReadOnlyList<ISolution<TGenotype>> Select(IReadOnlyList<ISolution<TGenotype>> population, Objective objective, int count, IRandomNumberGenerator random) {
    return Enumerable.Range(0, count).ParallelSelect(random, (_, _, r) => Select(population, objective, r));
  }

  IReadOnlyList<ISolution<TGenotype>> ISelector<TGenotype, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>>.Select(IReadOnlyList<ISolution<TGenotype>> population, Objective objective, int count, IRandomNumberGenerator random, IEncoding<TGenotype> encoding, IProblem<TGenotype, IEncoding<TGenotype>> problem) {
    return Select(population, objective, count, random);
  }

  IReadOnlyList<ISolution<TGenotype>> ISelector<TGenotype, IEncoding<TGenotype>>.Select(IReadOnlyList<ISolution<TGenotype>> population, Objective objective, int count, IRandomNumberGenerator random, IEncoding<TGenotype> encoding) {
    return Select(population, objective, count, random);
  }
}
