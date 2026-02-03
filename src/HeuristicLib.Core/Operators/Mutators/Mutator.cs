using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Mutators;

public abstract class Mutator<TGenotype, TSearchSpace, TProblem> : IMutator<TGenotype, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{

  IReadOnlyList<TGenotype> IMutator<TGenotype, TSearchSpace, TProblem>.Mutate(IReadOnlyList<TGenotype> parent, IRandomNumberGenerator random, TSearchSpace encoding, TProblem problem) => parent.ParallelSelect(random, action: (_, x, r) => Mutate(x, r, encoding, problem));
  public abstract TGenotype Mutate(TGenotype parent, IRandomNumberGenerator random, TSearchSpace encoding, TProblem problem);
}

public abstract class Mutator<TGenotype, TSearchSpace> : IMutator<TGenotype, TSearchSpace>
  where TSearchSpace : class, ISearchSpace<TGenotype>
{

  public IReadOnlyList<TGenotype> Mutate(IReadOnlyList<TGenotype> parent, IRandomNumberGenerator random, TSearchSpace encoding) => parent.ParallelSelect(random, action: (_, x, r) => Mutate(x, r, encoding));

  IReadOnlyList<TGenotype> IMutator<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>>.Mutate(IReadOnlyList<TGenotype> parent, IRandomNumberGenerator random, TSearchSpace encoding, IProblem<TGenotype, TSearchSpace> problem) => Mutate(parent, random, encoding);
  public abstract TGenotype Mutate(TGenotype parent, IRandomNumberGenerator random, TSearchSpace encoding);
}

public abstract class Mutator<TGenotype> : IMutator<TGenotype>
{

  public IReadOnlyList<TGenotype> Mutate(IReadOnlyList<TGenotype> parent, IRandomNumberGenerator random) => parent.ParallelSelect(random, action: (_, x, r) => Mutate(x, r));

  IReadOnlyList<TGenotype> IMutator<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>.Mutate(IReadOnlyList<TGenotype> parent, IRandomNumberGenerator random, ISearchSpace<TGenotype> searchSpace, IProblem<TGenotype, ISearchSpace<TGenotype>> problem) => Mutate(parent, random);

  IReadOnlyList<TGenotype> IMutator<TGenotype, ISearchSpace<TGenotype>>.Mutate(IReadOnlyList<TGenotype> parent, IRandomNumberGenerator random, ISearchSpace<TGenotype> searchSpace) => Mutate(parent, random);
  public abstract TGenotype Mutate(TGenotype parent, IRandomNumberGenerator random);
}

// This would also work for other operators

// ToDo: extract base class for multi-operators 
