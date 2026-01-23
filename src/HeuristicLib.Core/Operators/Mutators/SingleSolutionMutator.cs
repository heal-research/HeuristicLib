using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Mutators;

public abstract class SingleSolutionMutator<TGenotype, TSearchSpace, TProblem> : IMutator<TGenotype, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  public abstract TGenotype Mutate(TGenotype parent, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

  IReadOnlyList<TGenotype> IMutator<TGenotype, TSearchSpace, TProblem>.Mutate(IReadOnlyList<TGenotype> parent, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) => parent.ParallelSelect(random, (_, x, r) => Mutate(x, r, searchSpace, problem));
}

public abstract class SingleSolutionMutator<TGenotype, TSearchSpace> : IMutator<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>>
  where TSearchSpace : class, ISearchSpace<TGenotype>
{
  public abstract TGenotype Mutate(TGenotype parent, IRandomNumberGenerator random, TSearchSpace searchSpace);

  public IReadOnlyList<TGenotype> Mutate(IReadOnlyList<TGenotype> parent, IRandomNumberGenerator random, TSearchSpace searchSpace) => parent.ParallelSelect(random, (_, x, r) => Mutate(x, r, searchSpace));

  IReadOnlyList<TGenotype> IMutator<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>>.Mutate(IReadOnlyList<TGenotype> parent, IRandomNumberGenerator random, TSearchSpace searchSpace, IProblem<TGenotype, TSearchSpace> problem) => Mutate(parent, random, searchSpace);
}

public abstract class SingleSolutionMutator<TGenotype> : IMutator<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>
{
  public abstract TGenotype Mutate(TGenotype parent, IRandomNumberGenerator random);

  public IReadOnlyList<TGenotype> Mutate(IReadOnlyList<TGenotype> parent, IRandomNumberGenerator random) => parent.ParallelSelect(random, (_, x, r) => Mutate(x, r));

  IReadOnlyList<TGenotype> IMutator<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>.Mutate(IReadOnlyList<TGenotype> parent, IRandomNumberGenerator random, ISearchSpace<TGenotype> searchSpace, IProblem<TGenotype, ISearchSpace<TGenotype>> problem) => Mutate(parent, random);
}
