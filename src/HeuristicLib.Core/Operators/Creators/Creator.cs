using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Creators;

public abstract class Creator<TGenotype, TSearchSpace, TProblem> : ICreator<TGenotype, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  public abstract TGenotype Create(IRandomNumberGenerator random, TSearchSpace encoding, TProblem problem);

  public IReadOnlyList<TGenotype> Create(int count, IRandomNumberGenerator random, TSearchSpace encoding, TProblem problem) => Enumerable.Range(0, count).ParallelSelect(random, action: (_, _, r) => Create(r, encoding, problem)).ToArray();
}

public abstract class Creator<TGenotype, TSearchSpace> : ICreator<TGenotype, TSearchSpace>
  where TSearchSpace : class, ISearchSpace<TGenotype>
{

  public IReadOnlyList<TGenotype> Create(int count, IRandomNumberGenerator random, TSearchSpace encoding) => Enumerable.Range(0, count).ParallelSelect(random, action: (_, _, r) => Create(r, encoding)).ToArray();

  IReadOnlyList<TGenotype> ICreator<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>>.Create(int count, IRandomNumberGenerator random, TSearchSpace encoding, IProblem<TGenotype, TSearchSpace> problem) => Create(count, random, encoding);
  public abstract TGenotype Create(IRandomNumberGenerator random, TSearchSpace encoding);
}

public abstract class Creator<TGenotype> : ICreator<TGenotype>
{

  public IReadOnlyList<TGenotype> Create(int count, IRandomNumberGenerator random) => Enumerable.Range(0, count).ParallelSelect(random, action: (_, _, r) => Create(r)).ToArray();

  IReadOnlyList<TGenotype> ICreator<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>.Create(int count, IRandomNumberGenerator random, ISearchSpace<TGenotype> searchSpace, IProblem<TGenotype, ISearchSpace<TGenotype>> problem) => Create(count, random);

  IReadOnlyList<TGenotype> ICreator<TGenotype, ISearchSpace<TGenotype>>.Create(int count, IRandomNumberGenerator random, ISearchSpace<TGenotype> searchSpace) => Create(count, random);
  public abstract TGenotype Create(IRandomNumberGenerator random);
}
