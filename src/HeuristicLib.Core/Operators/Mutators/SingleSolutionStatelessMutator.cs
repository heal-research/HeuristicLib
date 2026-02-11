using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Mutators;

public abstract record SingleSolutionStatelessMutator<TGenotype, TSearchSpace, TProblem>
  : StatelessMutator<TGenotype, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  public abstract TGenotype Mutate(TGenotype parent, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

  public override IReadOnlyList<TGenotype> Mutate(IReadOnlyList<TGenotype> parent, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
    BatchExecution.Sequential(parent, (p, r) => Mutate(p, r, searchSpace, problem), random);
}

public abstract record SingleSolutionStatelessMutator<TGenotype, TSearchSpace>
  : StatelessMutator<TGenotype, TSearchSpace>
  where TSearchSpace : class, ISearchSpace<TGenotype>
{
  public abstract TGenotype Mutate(TGenotype parent, IRandomNumberGenerator random, TSearchSpace searchSpace);

  public override IReadOnlyList<TGenotype> Mutate(IReadOnlyList<TGenotype> parent, IRandomNumberGenerator random, TSearchSpace searchSpace) =>
    BatchExecution.Sequential(parent, (p, r) => Mutate(p, r, searchSpace), random);
}

public abstract record SingleSolutionStatelessMutator<TGenotype>
  : StatelessMutator<TGenotype>
{
  public abstract TGenotype Mutate(TGenotype parent, IRandomNumberGenerator random);

  public override IReadOnlyList<TGenotype> Mutate(IReadOnlyList<TGenotype> parent, IRandomNumberGenerator random) =>
    BatchExecution.Sequential(parent, (p, r) => Mutate(p, r), random);
}
