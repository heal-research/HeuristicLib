using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Creators;

public abstract class Creator<TGenotype, TSearchSpace, TProblem> : ICreator<TGenotype, TSearchSpace, TProblem>
  where TGenotype : class
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  public abstract IReadOnlyList<TGenotype> Create(int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
}

public abstract class Creator<TGenotype, TSearchSpace> : ICreator<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>>
  where TGenotype : class
  where TSearchSpace : class, ISearchSpace<TGenotype>
{
  public abstract IReadOnlyList<TGenotype> Create(int count, IRandomNumberGenerator random, TSearchSpace searchSpace);

  IReadOnlyList<TGenotype> ICreator<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>>.Create(int count, IRandomNumberGenerator random, TSearchSpace searchSpace, IProblem<TGenotype, TSearchSpace> problem) => Create(count, random, searchSpace);
}

public abstract class Creator<TGenotype> : ICreator<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>
  where TGenotype : class
{
  public abstract IReadOnlyList<TGenotype> Create(int count, IRandomNumberGenerator random);

  IReadOnlyList<TGenotype> ICreator<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>.Create(int count, IRandomNumberGenerator random, ISearchSpace<TGenotype> searchSpace, IProblem<TGenotype, ISearchSpace<TGenotype>> problem) => Create(count, random);
}
