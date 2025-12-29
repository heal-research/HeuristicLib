using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Crossovers;

public abstract class Crossover<TGenotype, TSearchSpace, TProblem> : ICrossover<TGenotype, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace> {
  public abstract TGenotype Cross(IParents<TGenotype> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

  public IReadOnlyList<TGenotype> Cross(IReadOnlyList<IParents<TGenotype>> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) {
    return parents.ParallelSelect(random, (_, x, r) => Cross(x, r, searchSpace, problem));
  }
}

public abstract class Crossover<TGenotype, TSearchSpace> : ICrossover<TGenotype, TSearchSpace>
  where TSearchSpace : class, ISearchSpace<TGenotype> {
  public abstract TGenotype Cross(IParents<TGenotype> parents, IRandomNumberGenerator random, TSearchSpace searchSpace);

  public IReadOnlyList<TGenotype> Cross(IReadOnlyList<IParents<TGenotype>> parents, IRandomNumberGenerator random, TSearchSpace searchSpace) {
    return parents.ParallelSelect(random, (_, x, r) => Cross(x, r, searchSpace));
  }

  IReadOnlyList<TGenotype> ICrossover<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>>.Cross(IReadOnlyList<IParents<TGenotype>> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, IProblem<TGenotype, TSearchSpace> problem) {
    return Cross(parents, random, searchSpace);
  }
}

public abstract class Crossover<TGenotype> : ICrossover<TGenotype> {
  public abstract TGenotype Cross(IParents<TGenotype> parents, IRandomNumberGenerator random);

  public IReadOnlyList<TGenotype> Cross(IReadOnlyList<IParents<TGenotype>> parents, IRandomNumberGenerator random) {
    return parents.ParallelSelect(random, (_, x, r) => Cross(x, r));
  }

  IReadOnlyList<TGenotype> ICrossover<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>.Cross(IReadOnlyList<IParents<TGenotype>> parents, IRandomNumberGenerator random, ISearchSpace<TGenotype> searchSpace, IProblem<TGenotype, ISearchSpace<TGenotype>> problem) {
    return Cross(parents, random);
  }

  IReadOnlyList<TGenotype> ICrossover<TGenotype, ISearchSpace<TGenotype>>.Cross(IReadOnlyList<IParents<TGenotype>> parents, IRandomNumberGenerator random, ISearchSpace<TGenotype> searchSpace) {
    return Cross(parents, random);
  }
}

public class NoCrossOver<TGenotype> : BatchCrossover<TGenotype> {
  public static readonly NoCrossOver<TGenotype> Instance = new();

  public override IReadOnlyList<TGenotype> Cross(IReadOnlyList<IParents<TGenotype>> parents, IRandomNumberGenerator random) {
    return parents.Select(x => x.Parent1).ToArray();
  }
}
