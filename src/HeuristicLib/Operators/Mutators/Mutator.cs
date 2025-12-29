using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Mutators;

public abstract class Mutator<TGenotype, TSearchSpace, TProblem> : IMutator<TGenotype, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace> {
  public abstract TGenotype Mutate(TGenotype parent, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

  IReadOnlyList<TGenotype> IMutator<TGenotype, TSearchSpace, TProblem>.Mutate(IReadOnlyList<TGenotype> parent, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) {
    return parent.ParallelSelect(random, (_, x, r) => Mutate(x, r, searchSpace, problem));
  }
}

public abstract class Mutator<TGenotype, TSearchSpace> : IMutator<TGenotype, TSearchSpace>
  where TSearchSpace : class, ISearchSpace<TGenotype> {
  public abstract TGenotype Mutate(TGenotype parent, IRandomNumberGenerator random, TSearchSpace searchSpace);

  public IReadOnlyList<TGenotype> Mutate(IReadOnlyList<TGenotype> parent, IRandomNumberGenerator random, TSearchSpace searchSpace) {
    return parent.ParallelSelect(random, (_, x, r) => Mutate(x, r, searchSpace));
  }

  IReadOnlyList<TGenotype> IMutator<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>>.Mutate(IReadOnlyList<TGenotype> parent, IRandomNumberGenerator random, TSearchSpace searchSpace, IProblem<TGenotype, TSearchSpace> problem) {
    return Mutate(parent, random, searchSpace);
  }
}

public abstract class Mutator<TGenotype> : IMutator<TGenotype> {
  public abstract TGenotype Mutate(TGenotype parent, IRandomNumberGenerator random);

  public IReadOnlyList<TGenotype> Mutate(IReadOnlyList<TGenotype> parent, IRandomNumberGenerator random) {
    return parent.ParallelSelect(random, (_, x, r) => Mutate(x, r));
  }

  IReadOnlyList<TGenotype> IMutator<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>.Mutate(IReadOnlyList<TGenotype> parent, IRandomNumberGenerator random, ISearchSpace<TGenotype> searchSpace, IProblem<TGenotype, ISearchSpace<TGenotype>> problem) {
    return Mutate(parent, random);
  }

  IReadOnlyList<TGenotype> IMutator<TGenotype, ISearchSpace<TGenotype>>.Mutate(IReadOnlyList<TGenotype> parent, IRandomNumberGenerator random, ISearchSpace<TGenotype> searchSpace) {
    return Mutate(parent, random);
  }
}

// This would also work for other operators

// ToDo: extract base class for multi-operators 
