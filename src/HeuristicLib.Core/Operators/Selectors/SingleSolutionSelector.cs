using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Selectors;

public abstract class SingleSolutionSelector<TGenotype, TSearchSpace, TProblem> : ISelector<TGenotype, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  public abstract ISolution<TGenotype> Select(IReadOnlyList<ISolution<TGenotype>> population, Objective objective, IRandomNumberGenerator random, TSearchSpace encoding, TProblem problem);

  public IReadOnlyList<ISolution<TGenotype>> Select(IReadOnlyList<ISolution<TGenotype>> population, Objective objective, int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) => Enumerable.Range(0, count).ParallelSelect(random, (_, _, r) => Select(population, objective, r, searchSpace, problem));
}

public abstract class SingleSolutionSelector<TGenotype, TSearchSpace> : ISelector<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>>
  where TSearchSpace : class, ISearchSpace<TGenotype>
{
  public abstract ISolution<TGenotype> Select(IReadOnlyList<ISolution<TGenotype>> population, Objective objective, IRandomNumberGenerator random, TSearchSpace encoding);

  public IReadOnlyList<ISolution<TGenotype>> Select(IReadOnlyList<ISolution<TGenotype>> population, Objective objective, int count, IRandomNumberGenerator random, TSearchSpace searchSpace) => Enumerable.Range(0, count).ParallelSelect(random, (_, _, r) => Select(population, objective, r, searchSpace));

  IReadOnlyList<ISolution<TGenotype>> ISelector<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>>.Select(IReadOnlyList<ISolution<TGenotype>> population, Objective objective, int count, IRandomNumberGenerator random, TSearchSpace searchSpace, IProblem<TGenotype, TSearchSpace> problem) => Select(population, objective, count, random, searchSpace);
}

public abstract class SingleSolutionSelector<TGenotype> : ISelector<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>
{
  public abstract ISolution<TGenotype> Select(IReadOnlyList<ISolution<TGenotype>> population, Objective objective, IRandomNumberGenerator random);

  public IReadOnlyList<ISolution<TGenotype>> Select(IReadOnlyList<ISolution<TGenotype>> population, Objective objective, int count, IRandomNumberGenerator random) => Enumerable.Range(0, count).ParallelSelect(random, (_, _, r) => Select(population, objective, r));

  IReadOnlyList<ISolution<TGenotype>> ISelector<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>.Select(IReadOnlyList<ISolution<TGenotype>> population, Objective objective, int count, IRandomNumberGenerator random, ISearchSpace<TGenotype> searchSpace, IProblem<TGenotype, ISearchSpace<TGenotype>> problem) => Select(population, objective, count, random);
}
