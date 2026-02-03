using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Creators;

public abstract class BatchCreator<TGenotype, TSearchSpace, TProblem> : ICreator<TGenotype, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  public abstract IReadOnlyList<TGenotype> Create(int count, IRandomNumberGenerator random, TSearchSpace encoding, TProblem problem);
}

public abstract class BatchCreator<TGenotype> : ICreator<TGenotype>
{
  public abstract IReadOnlyList<TGenotype> Create(int count, IRandomNumberGenerator random);

  IReadOnlyList<TGenotype> ICreator<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>.Create(int count, IRandomNumberGenerator random, ISearchSpace<TGenotype> searchSpace, IProblem<TGenotype, ISearchSpace<TGenotype>> problem) => Create(count, random);

  IReadOnlyList<TGenotype> ICreator<TGenotype, ISearchSpace<TGenotype>>.Create(int count, IRandomNumberGenerator random, ISearchSpace<TGenotype> searchSpace) => Create(count, random);
}

public abstract class BatchCreator<TGenotype, TSearchSpace> : ICreator<TGenotype, TSearchSpace>
  where TSearchSpace : class, ISearchSpace<TGenotype>
{
  public abstract IReadOnlyList<TGenotype> Create(int count, IRandomNumberGenerator random, TSearchSpace encoding);

  IReadOnlyList<TGenotype> ICreator<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>>.Create(int count, IRandomNumberGenerator random, TSearchSpace encoding, IProblem<TGenotype, TSearchSpace> problem) => Create(count, random, encoding);
}
