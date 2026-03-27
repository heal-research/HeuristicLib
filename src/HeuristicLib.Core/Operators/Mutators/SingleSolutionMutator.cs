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
  public abstract TGenotype Mutate(TGenotype parent, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

  public override IReadOnlyList<TGenotype> Mutate(IReadOnlyList<TGenotype> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
    BatchExecution.Sequential(parents, (p, r) => Mutate(p, r, searchSpace, problem), random);
}

public abstract record SingleSolutionMutator<TGenotype, TSearchSpace>
  : Mutator<TGenotype, TSearchSpace>
  where TSearchSpace : class, ISearchSpace<TGenotype>
{
  public abstract TGenotype Mutate(TGenotype parent, IRandomNumberGenerator random, TSearchSpace searchSpace);

  public override IReadOnlyList<TGenotype> Mutate(IReadOnlyList<TGenotype> parents, IRandomNumberGenerator random, TSearchSpace searchSpace) =>
    BatchExecution.Sequential(parents, (p, r) => Mutate(p, r, searchSpace), random);
}

public abstract record SingleSolutionMutator<TGenotype>
  : Mutator<TGenotype>
{
  public abstract TGenotype Mutate(TGenotype parent, IRandomNumberGenerator random);

  public override IReadOnlyList<TGenotype> Mutate(IReadOnlyList<TGenotype> parents, IRandomNumberGenerator random) =>
    BatchExecution.Sequential(parents, (p, r) => Mutate(p, r), random);
}
