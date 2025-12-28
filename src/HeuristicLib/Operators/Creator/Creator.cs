using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Creator;

public abstract class Creator<TGenotype, TSearchSpace, TProblem> : ICreator<TGenotype, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace> {
  public abstract TGenotype Create(IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

  public IReadOnlyList<TGenotype> Create(int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) {
    return Enumerable.Range(0, count).ParallelSelect(random, (_, _, r) => Create(r, searchSpace, problem)).ToArray();
  }
}

public abstract class Creator<TGenotype, TSearchSpace> : ICreator<TGenotype, TSearchSpace>
  where TSearchSpace : class, ISearchSpace<TGenotype> {
  public abstract TGenotype Create(IRandomNumberGenerator random, TSearchSpace encoding);

  public IReadOnlyList<TGenotype> Create(int count, IRandomNumberGenerator random, TSearchSpace searchSpace) {
    return Enumerable.Range(0, count).ParallelSelect(random, (_, _, r) => Create(r, searchSpace)).ToArray();
  }

  IReadOnlyList<TGenotype> ICreator<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>>.Create(int count, IRandomNumberGenerator random, TSearchSpace searchSpace, IProblem<TGenotype, TSearchSpace> problem) {
    return Create(count, random, searchSpace);
  }
}

public abstract class Creator<TGenotype> : ICreator<TGenotype> {
  public abstract TGenotype Create(IRandomNumberGenerator random);

  public IReadOnlyList<TGenotype> Create(int count, IRandomNumberGenerator random) {
    return Enumerable.Range(0, count).ParallelSelect(random, (_, _, r) => Create(r)).ToArray();
  }

  IReadOnlyList<TGenotype> ICreator<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>.Create(int count, IRandomNumberGenerator random, ISearchSpace<TGenotype> searchSpace, IProblem<TGenotype, ISearchSpace<TGenotype>> problem) {
    return Create(count, random);
  }

  IReadOnlyList<TGenotype> ICreator<TGenotype, ISearchSpace<TGenotype>>.Create(int count, IRandomNumberGenerator random, ISearchSpace<TGenotype> searchSpace) {
    return Create(count, random);
  }
}
