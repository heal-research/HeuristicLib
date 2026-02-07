using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Creators;

public abstract class SingleSolutionCreator<TGenotype, TSearchSpace, TProblem> 
  : Creator<TGenotype, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
}

public abstract class SingleSolutionCreatorInstance<TGenotype, TSearchSpace, TProblem>
  : CreatorInstance<TGenotype, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  public abstract TGenotype Create(IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
  
  public override IReadOnlyList<TGenotype> Create(int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
    BatchExecution.Sequential(count, r => Create(r, searchSpace, problem), random);
}


public abstract class SingleSolutionCreator<TGenotype, TSearchSpace>
  : Creator<TGenotype, TSearchSpace>
  where TSearchSpace : class, ISearchSpace<TGenotype>
{
}

public abstract class SingleSolutionCreatorInstance<TGenotype, TSearchSpace> 
  : CreatorInstance<TGenotype, TSearchSpace>
  where TSearchSpace : class, ISearchSpace<TGenotype>
{
  public abstract TGenotype Create(IRandomNumberGenerator random, TSearchSpace searchSpace);
  
  public override IReadOnlyList<TGenotype> Create(int count, IRandomNumberGenerator random, TSearchSpace searchSpace) =>
    BatchExecution.Sequential(count, r => Create(r, searchSpace), random);
}


public abstract class SingleSolutionCreator<TGenotype>
  : Creator<TGenotype>
{
}

public abstract class SingleSolutionCreatorInstance<TGenotype>
  : CreatorInstance<TGenotype>
{
  public abstract TGenotype Create(IRandomNumberGenerator random);

  public override IReadOnlyList<TGenotype> Create(int count, IRandomNumberGenerator random) =>
    BatchExecution.Sequential(count, r => Create(r), random);
}


// ToDo: Since the stateless versions will be the most commonly used ones, think about naming them "Creator" and the other ones the "StatefulCreator".

public abstract class SingleSolutionStatelessCreator<TGenotype, TSearchSpace, TProblem> 
  : StatelessCreator<TGenotype, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  public abstract TGenotype Create(IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

  public override IReadOnlyList<TGenotype> Create(int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
    BatchExecution.Sequential(count, r => Create(r, searchSpace, problem), random);
}

public abstract class SingleSolutionStatelessCreator<TGenotype, TSearchSpace> 
  : StatelessCreator<TGenotype, TSearchSpace>
  where TSearchSpace : class, ISearchSpace<TGenotype>
{
  public abstract TGenotype Create(IRandomNumberGenerator random, TSearchSpace searchSpace);
  
  public override IReadOnlyList<TGenotype> Create(int count, IRandomNumberGenerator random, TSearchSpace searchSpace) =>
    BatchExecution.Sequential(count, r => Create(r, searchSpace), random);
}

public abstract class SingleSolutionStatelessCreator<TGenotype>
  : StatelessCreator<TGenotype>
{
  public abstract TGenotype Create(IRandomNumberGenerator random);
  
  public override IReadOnlyList<TGenotype> Create(int count, IRandomNumberGenerator random) =>
    BatchExecution.Sequential(count, r => Create(r), random);
}

