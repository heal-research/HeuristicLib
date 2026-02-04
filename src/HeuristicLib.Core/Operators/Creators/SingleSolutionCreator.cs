using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Creators;

public abstract class SingleSolutionCreator<TGenotype, TSearchSpace, TProblem> : ISingleSolutionCreator<TGenotype, TSearchSpace, TProblem>
  where TGenotype : class
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  public abstract ISingleSolutionCreatorInstance<TGenotype, TSearchSpace, TProblem> CreateExecutionInstance(ExecutionScope scope);
}

public abstract class SingleSolutionCreatorInstance<TGenotype, TSearchSpace, TProblem> : ISingleSolutionCreatorInstance<TGenotype, TSearchSpace, TProblem>
  where TGenotype : class
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  public abstract TGenotype Create(IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
  public IBatchExecutor BatchExecutor => BatchExecutors.Sequential;
}


public abstract class SingleSolutionCreator<TGenotype, TSearchSpace> : ISingleSolutionCreator<TGenotype, TSearchSpace>
  where TGenotype : class
  where TSearchSpace : class, ISearchSpace<TGenotype>
{
  public abstract ISingleSolutionCreatorInstance<TGenotype, TSearchSpace> CreateExecutionInstance(ExecutionScope scope);
}

public abstract class SingleSolutionCreatorInstance<TGenotype, TSearchSpace> : ISingleSolutionCreatorInstance<TGenotype, TSearchSpace>
  where TGenotype : class
  where TSearchSpace : class, ISearchSpace<TGenotype>
{
  public abstract TGenotype Create(IRandomNumberGenerator random, TSearchSpace searchSpace);
  
  public IBatchExecutor BatchExecutor  => BatchExecutors.Sequential;
}


public abstract class SingleSolutionCreator<TGenotype> : ISingleSolutionCreator<TGenotype>
  where TGenotype : class
{
  public abstract ISingleSolutionCreatorInstance<TGenotype> CreateExecutionInstance(ExecutionScope scope);
}

public abstract class SingleSolutionCreatorInstance<TGenotype> : ISingleSolutionCreatorInstance<TGenotype>
  where TGenotype : class
{
  public abstract TGenotype Create(IRandomNumberGenerator random);
  
  public IBatchExecutor BatchExecutor  => BatchExecutors.Sequential;
}


public abstract class StatelessSingleSolutionCreator<TGenotype, TSearchSpace, TProblem> 
  : ISingleSolutionCreator<TGenotype, TSearchSpace, TProblem>, ISingleSolutionCreatorInstance<TGenotype, TSearchSpace, TProblem>
  where TGenotype : class
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  public abstract TGenotype Create(IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

  public ISingleSolutionCreatorInstance<TGenotype, TSearchSpace, TProblem> CreateExecutionInstance(ExecutionScope scope) => this;

  public IBatchExecutor BatchExecutor => BatchExecutors.Sequential;
}

public abstract class StatelessSingleSolutionCreator<TGenotype, TSearchSpace> 
  : IStatelessSingleSolutionCreator<TGenotype, TSearchSpace>
  where TGenotype : class
  where TSearchSpace : class, ISearchSpace<TGenotype>
{
  public abstract TGenotype Create(IRandomNumberGenerator random, TSearchSpace searchSpace);

  public ISingleSolutionCreatorInstance<TGenotype, TSearchSpace> CreateExecutionInstance(ExecutionScope scope) => this;

  public IBatchExecutor BatchExecutor => BatchExecutors.Sequential;
}

public abstract class StatelessSingleSolutionCreator<TGenotype> 
  : IStatelessSingleSolutionCreator<TGenotype>
  where TGenotype : class
{
  public abstract TGenotype Create(IRandomNumberGenerator random);

  public ISingleSolutionCreatorInstance<TGenotype> CreateExecutionInstance(ExecutionScope scope) => this;

  public IBatchExecutor BatchExecutor => BatchExecutors.Sequential;
}
