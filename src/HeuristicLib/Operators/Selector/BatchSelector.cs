using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.Selector;

public abstract class BatchSelector<TGenotype, TEncoding, TProblem> : ISelector<TGenotype, TEncoding, TProblem>
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : class, IProblem<TGenotype, TEncoding> {
  public abstract IReadOnlyList<Solution<TGenotype>> Select(IReadOnlyList<Solution<TGenotype>> population, Objective objective, int count, IRandomNumberGenerator random, TEncoding encoding, TProblem problem);
}

public abstract class BatchSelector<TGenotype> : ISelector<TGenotype> {
  public abstract IReadOnlyList<Solution<TGenotype>> Select(IReadOnlyList<Solution<TGenotype>> population, Objective objective, int count, IRandomNumberGenerator random);

  IReadOnlyList<Solution<TGenotype>> ISelector<TGenotype, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>>.Select(IReadOnlyList<Solution<TGenotype>> population, Objective objective, int count, IRandomNumberGenerator random, IEncoding<TGenotype> encoding, IProblem<TGenotype, IEncoding<TGenotype>> problem) {
    return Select(population, objective, count, random);
  }

  IReadOnlyList<Solution<TGenotype>> ISelector<TGenotype, IEncoding<TGenotype>>.Select(IReadOnlyList<Solution<TGenotype>> population, Objective objective, int count, IRandomNumberGenerator random, IEncoding<TGenotype> encoding) {
    return Select(population, objective, count, random);
  }
}

public abstract class BatchSelector<TGenotype, TEncoding> : ISelector<TGenotype, TEncoding>
  where TEncoding : class, IEncoding<TGenotype> {
  public abstract IReadOnlyList<Solution<TGenotype>> Select(IReadOnlyList<Solution<TGenotype>> population, Objective objective, int count, IRandomNumberGenerator random, TEncoding encoding);

  IReadOnlyList<Solution<TGenotype>> ISelector<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>>.Select(IReadOnlyList<Solution<TGenotype>> population, Objective objective, int count, IRandomNumberGenerator random, TEncoding encoding, IProblem<TGenotype, TEncoding> problem) {
    return Select(population, objective, count, random, encoding);
  }
}
