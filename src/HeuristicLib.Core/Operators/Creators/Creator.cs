using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Creators;

public abstract class Creator<TGenotype, TSearchSpace, TProblem> 
  : ICreator<TGenotype, TSearchSpace, TProblem>
  where TGenotype : class
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  public abstract ICreatorInstance<TGenotype, TSearchSpace, TProblem> CreateExecutionInstance(ExecutionScope scope);
}

public abstract class CreatorInstance<TGenotype, TSearchSpace, TProblem>
  : ICreatorInstance<TGenotype, TSearchSpace, TProblem>
  where TGenotype : class
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  public IBatchExecutor BatchExecutor { get; init; } = BatchExecutors.Sequential;
  
  public abstract TGenotype Create(IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
  
  public virtual IReadOnlyList<TGenotype> Create(int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
    BatchExecutor.ExecuteBatch<TGenotype>(count, r => Create(r, searchSpace, problem), random);
}

public abstract class Creator<TGenotype, TSearchSpace>
  : ICreator<TGenotype, TSearchSpace>
  where TGenotype : class
  where TSearchSpace : class, ISearchSpace<TGenotype>
{
  public abstract ICreatorInstance<TGenotype, TSearchSpace> CreateExecutionInstance(ExecutionScope scope);
}

public abstract class CreatorInstance<TGenotype, TSearchSpace> 
  : ICreatorInstance<TGenotype, TSearchSpace>
  where TGenotype : class
  where TSearchSpace : class, ISearchSpace<TGenotype>
{
  public IBatchExecutor BatchExecutor { get; init; } = BatchExecutors.Sequential;
  
  public abstract TGenotype Create(IRandomNumberGenerator random, TSearchSpace searchSpace);
  
  public virtual IReadOnlyList<TGenotype> Create(int count, IRandomNumberGenerator random, TSearchSpace searchSpace) =>
    BatchExecutor.ExecuteBatch<TGenotype>(count, r => Create(r, searchSpace), random);
}

public abstract class Creator<TGenotype>
  : ICreator<TGenotype>
  where TGenotype : class
{
  public abstract ICreatorInstance<TGenotype> CreateExecutionInstance(ExecutionScope scope);
}

public abstract class CreatorInstance<TGenotype>
  : ICreatorInstance<TGenotype>
  where TGenotype : class
{
  public IBatchExecutor BatchExecutor { get; init; } = BatchExecutors.Sequential;
  
  public abstract TGenotype Create(IRandomNumberGenerator random);

  public virtual IReadOnlyList<TGenotype> Create(int count, IRandomNumberGenerator random) =>
    BatchExecutor.ExecuteBatch<TGenotype>(count, r => Create(r), random);
}

// ToDo: Since the stateless versions will be the most commonly used ones, think about naming them "Creator" and the other ones the "StatefulCreator".

public abstract class StatelessCreator<TGenotype, TSearchSpace, TProblem> 
  : ICreator<TGenotype, TSearchSpace, TProblem>, ICreatorInstance<TGenotype, TSearchSpace, TProblem>
  where TGenotype : class
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  public IBatchExecutor BatchExecutor { get; init; } = BatchExecutors.Sequential;
  
  public ICreatorInstance<TGenotype, TSearchSpace, TProblem> CreateExecutionInstance(ExecutionScope scope) => this;
  
  public abstract TGenotype Create(IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

  public virtual IReadOnlyList<TGenotype> Create(int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
    BatchExecutor.ExecuteBatch<TGenotype>(count, r => Create(r, searchSpace, problem), random);
}

public abstract class StatelessCreator<TGenotype, TSearchSpace> 
  : ICreator<TGenotype, TSearchSpace>, ICreatorInstance<TGenotype, TSearchSpace>
  where TGenotype : class
  where TSearchSpace : class, ISearchSpace<TGenotype>
{
  public IBatchExecutor BatchExecutor { get; init; } = BatchExecutors.Sequential;
  
  public ICreatorInstance<TGenotype, TSearchSpace> CreateExecutionInstance(ExecutionScope scope) => this;
  
  public abstract TGenotype Create(IRandomNumberGenerator random, TSearchSpace searchSpace);
  
  public virtual IReadOnlyList<TGenotype> Create(int count, IRandomNumberGenerator random, TSearchSpace searchSpace) =>
    BatchExecutor.ExecuteBatch<TGenotype>(count, r => Create(r, searchSpace), random);
}

public abstract class StatelessCreator<TGenotype>
  : ICreator<TGenotype>, ICreatorInstance<TGenotype>
  where TGenotype : class
{
  public IBatchExecutor BatchExecutor { get; init; } = BatchExecutors.Sequential;
  
  public ICreatorInstance<TGenotype> CreateExecutionInstance(ExecutionScope scope) => this;
  
  public abstract TGenotype Create(IRandomNumberGenerator random);
  
  public virtual IReadOnlyList<TGenotype> Create(int count, IRandomNumberGenerator random) =>
    BatchExecutor.ExecuteBatch<TGenotype>(count, r => Create(r), random);
}

