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
  public abstract IReadOnlyList<TGenotype> Create(int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
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
  public abstract IReadOnlyList<TGenotype> Create(int count, IRandomNumberGenerator random, TSearchSpace searchSpace);
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
  public abstract IReadOnlyList<TGenotype> Create(int count, IRandomNumberGenerator random);
}



public abstract class StatelessCreator<TGenotype, TSearchSpace, TProblem> 
  : ICreator<TGenotype, TSearchSpace, TProblem>, ICreatorInstance<TGenotype, TSearchSpace, TProblem>
  where TGenotype : class
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  public ICreatorInstance<TGenotype, TSearchSpace, TProblem> CreateExecutionInstance(ExecutionScope scope) => this;
  
  public abstract IReadOnlyList<TGenotype> Create(int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
}

public abstract class StatelessCreator<TGenotype, TSearchSpace> 
  : ICreator<TGenotype, TSearchSpace>, ICreatorInstance<TGenotype, TSearchSpace>
  where TGenotype : class
  where TSearchSpace : class, ISearchSpace<TGenotype>
{
  public ICreatorInstance<TGenotype, TSearchSpace> CreateExecutionInstance(ExecutionScope scope) => this;
  
  public abstract IReadOnlyList<TGenotype> Create(int count, IRandomNumberGenerator random, TSearchSpace searchSpace);
}

public abstract class StatelessCreator<TGenotype>
  : ICreator<TGenotype>, ICreatorInstance<TGenotype>
  where TGenotype : class
{
  public ICreatorInstance<TGenotype> CreateExecutionInstance(ExecutionScope scope) => this;
  
  public abstract IReadOnlyList<TGenotype> Create(int count, IRandomNumberGenerator random);
}

