using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Mutators;

public abstract record SingleSolutionMutator<TGenotype, TSearchSpace, TProblem>
  : Mutator<TGenotype, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  public new abstract class Instance
    : Mutator<TGenotype, TSearchSpace, TProblem>.Instance
  {
    public abstract TGenotype Mutate(TGenotype parent, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

    public override IReadOnlyList<TGenotype> Mutate(IReadOnlyList<TGenotype> parent, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
      BatchExecution.Sequential(parent, (p, r) => Mutate(p, r, searchSpace, problem), random);
  }
}



public abstract record SingleSolutionMutator<TGenotype, TSearchSpace>
  : Mutator<TGenotype, TSearchSpace>
  where TSearchSpace : class, ISearchSpace<TGenotype>
{
  public new abstract class Instance
    : Mutator<TGenotype, TSearchSpace>.Instance
  {
    public abstract TGenotype Mutate(TGenotype parent, IRandomNumberGenerator random, TSearchSpace searchSpace);

    public override IReadOnlyList<TGenotype> Mutate(IReadOnlyList<TGenotype> parent, IRandomNumberGenerator random, TSearchSpace searchSpace) =>
      BatchExecution.Sequential(parent, (p, r) => Mutate(p, r, searchSpace), random);
  }
}



public abstract record SingleSolutionMutator<TGenotype>
  : Mutator<TGenotype>
{
  public new abstract class Instance
    : Mutator<TGenotype>.Instance
  {
    public abstract TGenotype Mutate(TGenotype parent, IRandomNumberGenerator random);

    public override IReadOnlyList<TGenotype> Mutate(IReadOnlyList<TGenotype> parent, IRandomNumberGenerator random) =>
      BatchExecution.Sequential(parent, (p, r) => Mutate(p, r), random);
  }
}
