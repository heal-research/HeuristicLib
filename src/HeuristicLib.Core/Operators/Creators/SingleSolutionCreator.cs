using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Creators;

public abstract record SingleSolutionCreator<TGenotype, TSearchSpace, TProblem>
  : StatelessCreator<TGenotype, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  public abstract TGenotype Create(IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

  public override IReadOnlyList<TGenotype> Create(int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
    BatchExecution.Sequential(count, r => Create(r, searchSpace, problem), random);
}

public abstract record SingleSolutionCreator<TGenotype, TSearchSpace>
  : StatelessCreator<TGenotype, TSearchSpace>
  where TSearchSpace : class, ISearchSpace<TGenotype>
{
  public abstract TGenotype Create(IRandomNumberGenerator random, TSearchSpace searchSpace);

  public override IReadOnlyList<TGenotype> Create(int count, IRandomNumberGenerator random, TSearchSpace searchSpace) =>
    BatchExecution.Sequential(count, r => Create(r, searchSpace), random);
}

public abstract record SingleSolutionCreator<TGenotype>
  : StatelessCreator<TGenotype>
{
  public abstract TGenotype Create(IRandomNumberGenerator random);

  public override IReadOnlyList<TGenotype> Create(int count, IRandomNumberGenerator random) =>
    BatchExecution.Sequential(count, r => Create(r), random);
}
