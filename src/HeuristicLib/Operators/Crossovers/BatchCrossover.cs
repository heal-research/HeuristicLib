using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Crossovers;

public abstract class BatchCrossover<TGenotype, TSearchSpace, TProblem> : ICrossover<TGenotype, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace> {
  public abstract IReadOnlyList<TGenotype> Cross(IReadOnlyList<IParents<TGenotype>> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
}

public abstract class BatchCrossover<TGenotype> : ICrossover<TGenotype> {
  public abstract IReadOnlyList<TGenotype> Cross(IReadOnlyList<IParents<TGenotype>> parents, IRandomNumberGenerator random);

  IReadOnlyList<TGenotype> ICrossover<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>.Cross(IReadOnlyList<IParents<TGenotype>> parents, IRandomNumberGenerator random, ISearchSpace<TGenotype> searchSpace, IProblem<TGenotype, ISearchSpace<TGenotype>> problem) {
    return Cross(parents, random);
  }

  IReadOnlyList<TGenotype> ICrossover<TGenotype, ISearchSpace<TGenotype>>.Cross(IReadOnlyList<IParents<TGenotype>> parents, IRandomNumberGenerator random, ISearchSpace<TGenotype> searchSpace) {
    return Cross(parents, random);
  }
}

public abstract class BatchCrossover<TGenotype, TSearchSpace> : ICrossover<TGenotype, TSearchSpace>
  where TSearchSpace : class, ISearchSpace<TGenotype> {
  public abstract IReadOnlyList<TGenotype> Cross(IReadOnlyList<IParents<TGenotype>> parents, IRandomNumberGenerator random, TSearchSpace searchSpace);

  IReadOnlyList<TGenotype> ICrossover<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>>.Cross(IReadOnlyList<IParents<TGenotype>> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, IProblem<TGenotype, TSearchSpace> problem) {
    return Cross(parents, random, searchSpace);
  }
}
