using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Selectors;

public abstract class Selector<TGenotype, TSearchSpace, TProblem> : ISelector<TGenotype, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace> {
  public abstract ISolution<TGenotype> Select(IReadOnlyList<ISolution<TGenotype>> population, Objective objective, IRandomNumberGenerator random, TSearchSpace encoding, TProblem problem);

  public IReadOnlyList<ISolution<TGenotype>> Select(IReadOnlyList<ISolution<TGenotype>> population, Objective objective, int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) {
    return Enumerable.Range(0, count).ParallelSelect(random, (_, _, r) => Select(population, objective, r, searchSpace, problem));
  }
}

public abstract class Selector<TGenotype, TSearchSpace> : ISelector<TGenotype, TSearchSpace>
  where TSearchSpace : class, ISearchSpace<TGenotype> {
  public abstract ISolution<TGenotype> Select(IReadOnlyList<ISolution<TGenotype>> population, Objective objective, IRandomNumberGenerator random, TSearchSpace encoding);

  public IReadOnlyList<ISolution<TGenotype>> Select(IReadOnlyList<ISolution<TGenotype>> population, Objective objective, int count, IRandomNumberGenerator random, TSearchSpace searchSpace) {
    return Enumerable.Range(0, count).ParallelSelect(random, (_, _, r) => Select(population, objective, r, searchSpace));
  }

  IReadOnlyList<ISolution<TGenotype>> ISelector<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>>.Select(IReadOnlyList<ISolution<TGenotype>> population, Objective objective, int count, IRandomNumberGenerator random, TSearchSpace searchSpace, IProblem<TGenotype, TSearchSpace> problem) {
    return Select(population, objective, count, random, searchSpace);
  }
}

public abstract class Selector<TGenotype> : ISelector<TGenotype> {
  public abstract ISolution<TGenotype> Select(IReadOnlyList<ISolution<TGenotype>> population, Objective objective, IRandomNumberGenerator random);

  public IReadOnlyList<ISolution<TGenotype>> Select(IReadOnlyList<ISolution<TGenotype>> population, Objective objective, int count, IRandomNumberGenerator random) {
    return Enumerable.Range(0, count).ParallelSelect(random, (_, _, r) => Select(population, objective, r));
  }

  IReadOnlyList<ISolution<TGenotype>> ISelector<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>.Select(IReadOnlyList<ISolution<TGenotype>> population, Objective objective, int count, IRandomNumberGenerator random, ISearchSpace<TGenotype> searchSpace, IProblem<TGenotype, ISearchSpace<TGenotype>> problem) {
    return Select(population, objective, count, random);
  }

  IReadOnlyList<ISolution<TGenotype>> ISelector<TGenotype, ISearchSpace<TGenotype>>.Select(IReadOnlyList<ISolution<TGenotype>> population, Objective objective, int count, IRandomNumberGenerator random, ISearchSpace<TGenotype> searchSpace) {
    return Select(population, objective, count, random);
  }
}
