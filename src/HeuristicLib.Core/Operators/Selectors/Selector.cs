using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Selectors;

public abstract class Selector<TGenotype, TSearchSpace, TProblem> : ISelector<TGenotype, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{

  public IReadOnlyList<ISolution<TGenotype>> Select(IReadOnlyList<ISolution<TGenotype>> population, Objective objective, int count, IRandomNumberGenerator random, TSearchSpace encoding, TProblem problem) => Enumerable.Range(0, count).ParallelSelect(random, action: (_, _, r) => Select(population, objective, r, encoding, problem));
  public abstract ISolution<TGenotype> Select(IReadOnlyList<ISolution<TGenotype>> population, Objective objective, IRandomNumberGenerator random, TSearchSpace encoding, TProblem problem);
}

public abstract class Selector<TGenotype, TSearchSpace> : ISelector<TGenotype, TSearchSpace>
  where TSearchSpace : class, ISearchSpace<TGenotype>
{

  public IReadOnlyList<ISolution<TGenotype>> Select(IReadOnlyList<ISolution<TGenotype>> population, Objective objective, int count, IRandomNumberGenerator random, TSearchSpace encoding) => Enumerable.Range(0, count).ParallelSelect(random, action: (_, _, r) => Select(population, objective, r, encoding));

  IReadOnlyList<ISolution<TGenotype>> ISelector<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>>.Select(IReadOnlyList<ISolution<TGenotype>> population, Objective objective, int count, IRandomNumberGenerator random, TSearchSpace encoding, IProblem<TGenotype, TSearchSpace> problem) => Select(population, objective, count, random, encoding);
  public abstract ISolution<TGenotype> Select(IReadOnlyList<ISolution<TGenotype>> population, Objective objective, IRandomNumberGenerator random, TSearchSpace encoding);
}

public abstract class Selector<TGenotype> : ISelector<TGenotype>
{

  public IReadOnlyList<ISolution<TGenotype>> Select(IReadOnlyList<ISolution<TGenotype>> population, Objective objective, int count, IRandomNumberGenerator random) => Enumerable.Range(0, count).ParallelSelect(random, action: (_, _, r) => Select(population, objective, r));

  IReadOnlyList<ISolution<TGenotype>> ISelector<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>.Select(IReadOnlyList<ISolution<TGenotype>> population, Objective objective, int count, IRandomNumberGenerator random, ISearchSpace<TGenotype> searchSpace, IProblem<TGenotype, ISearchSpace<TGenotype>> problem) => Select(population, objective, count, random);

  IReadOnlyList<ISolution<TGenotype>> ISelector<TGenotype, ISearchSpace<TGenotype>>.Select(IReadOnlyList<ISolution<TGenotype>> population, Objective objective, int count, IRandomNumberGenerator random, ISearchSpace<TGenotype> searchSpace) => Select(population, objective, count, random);
  public abstract ISolution<TGenotype> Select(IReadOnlyList<ISolution<TGenotype>> population, Objective objective, IRandomNumberGenerator random);
}
