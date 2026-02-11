using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Crossovers;

public abstract record SingleSolutionCrossover<TGenotype, TSearchSpace, TProblem>
  : Crossover<TGenotype, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  public new abstract class Instance
    : Crossover<TGenotype, TSearchSpace, TProblem>.Instance
  {
    public abstract TGenotype Cross(IParents<TGenotype> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

    public override IReadOnlyList<TGenotype> Cross(IReadOnlyList<IParents<TGenotype>> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
      BatchExecution.Sequential(parents, (p, r) => Cross(p, r, searchSpace, problem), random);
  }
}

public abstract record SingleSolutionCrossover<TGenotype, TSearchSpace>
  : Crossover<TGenotype, TSearchSpace>
  where TSearchSpace : class, ISearchSpace<TGenotype>
{
  public new abstract class Instance
    : Crossover<TGenotype, TSearchSpace>.Instance
  {
    public abstract TGenotype Cross(IParents<TGenotype> parents, IRandomNumberGenerator random, TSearchSpace searchSpace);

    public override IReadOnlyList<TGenotype> Cross(IReadOnlyList<IParents<TGenotype>> parents, IRandomNumberGenerator random, TSearchSpace searchSpace) =>
      BatchExecution.Sequential(parents, (p, r) => Cross(p, r, searchSpace), random);
  }
}

public abstract record SingleSolutionCrossover<TGenotype>
  : Crossover<TGenotype>
{
  public new abstract class Instance
    : Crossover<TGenotype>.Instance
  {
    public abstract TGenotype Cross(IParents<TGenotype> parents, IRandomNumberGenerator random);

    public override IReadOnlyList<TGenotype> Cross(IReadOnlyList<IParents<TGenotype>> parents, IRandomNumberGenerator random) =>
      BatchExecution.Sequential(parents, (p, r) => Cross(p, r), random);
  }
}
