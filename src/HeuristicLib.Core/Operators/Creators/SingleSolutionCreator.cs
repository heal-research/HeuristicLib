using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Creators;

public abstract class SingleSolutionCreator<TGenotype, TSearchSpace, TProblem> : ICreator<TGenotype, TSearchSpace, TProblem>
  where TGenotype : class
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  public abstract TGenotype Create(IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

  public IReadOnlyList<TGenotype> Create(int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) => Enumerable.Range(0, count).ParallelSelect(random, (_, _, r) => Create(r, searchSpace, problem)).ToArray();
}

public abstract class SingleSolutionCreator<TGenotype, TSearchSpace> : ICreator<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>>
  where TGenotype : class
  where TSearchSpace : class, ISearchSpace<TGenotype>
{
  public abstract TGenotype Create(IRandomNumberGenerator random, TSearchSpace encoding);

  public IReadOnlyList<TGenotype> Create(int count, IRandomNumberGenerator random, TSearchSpace searchSpace) => Enumerable.Range(0, count).ParallelSelect(random, (_, _, r) => Create(r, searchSpace)).ToArray();

  IReadOnlyList<TGenotype> ICreator<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>>.Create(int count, IRandomNumberGenerator random, TSearchSpace searchSpace, IProblem<TGenotype, TSearchSpace> problem) => Create(count, random, searchSpace);
}

public abstract class SingleSolutionCreator<TGenotype> : ICreator<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>
  where TGenotype : class
{
  public abstract TGenotype Create(IRandomNumberGenerator random);

  public IReadOnlyList<TGenotype> Create(int count, IRandomNumberGenerator random) => Enumerable.Range(0, count).ParallelSelect(random, (_, _, r) => Create(r)).ToArray();

  IReadOnlyList<TGenotype> ICreator<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>.Create(int count, IRandomNumberGenerator random, ISearchSpace<TGenotype> searchSpace, IProblem<TGenotype, ISearchSpace<TGenotype>> problem) => Create(count, random);
}
