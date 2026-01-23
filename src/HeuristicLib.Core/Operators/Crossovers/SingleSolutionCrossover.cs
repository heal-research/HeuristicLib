using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Crossovers;

public abstract class SingleSolutionCrossover<TGenotype, TSearchSpace, TProblem> : ICrossover<TGenotype, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  public abstract TGenotype Cross(IParents<TGenotype> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

  public IReadOnlyList<TGenotype> Cross(IReadOnlyList<IParents<TGenotype>> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
  {
    // ToDo: this should be a unified implementation acrosso all operators
    return parents.ParallelSelect(random, (_, x, r) => Cross(x, r, searchSpace, problem));
  }
}

public abstract class SingleSolutionCrossover<TGenotype, TSearchSpace> : ICrossover<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>>
  where TSearchSpace : class, ISearchSpace<TGenotype>
{
  public abstract TGenotype Cross(IParents<TGenotype> parents, IRandomNumberGenerator random, TSearchSpace searchSpace);

  public IReadOnlyList<TGenotype> Cross(IReadOnlyList<IParents<TGenotype>> parents, IRandomNumberGenerator random, TSearchSpace searchSpace) => parents.ParallelSelect(random, (_, x, r) => Cross(x, r, searchSpace));

  IReadOnlyList<TGenotype> ICrossover<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>>.Cross(IReadOnlyList<IParents<TGenotype>> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, IProblem<TGenotype, TSearchSpace> problem) => Cross(parents, random, searchSpace);
}

public abstract class SingleSolutionCrossover<TGenotype> : ICrossover<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>
{
  public abstract TGenotype Cross(IParents<TGenotype> parents, IRandomNumberGenerator random);

  public IReadOnlyList<TGenotype> Cross(IReadOnlyList<IParents<TGenotype>> parents, IRandomNumberGenerator random) => parents.ParallelSelect(random, (_, x, r) => Cross(x, r));

  IReadOnlyList<TGenotype> ICrossover<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>.Cross(IReadOnlyList<IParents<TGenotype>> parents, IRandomNumberGenerator random, ISearchSpace<TGenotype> searchSpace, IProblem<TGenotype, ISearchSpace<TGenotype>> problem) => Cross(parents, random);
}
