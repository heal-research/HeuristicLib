using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Creators;

public abstract record Creator<TGenotype, TSearchSpace, TProblem>
  : ICreator<TGenotype, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  public abstract ICreatorInstance<TGenotype, TSearchSpace, TProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry);
  
  public abstract class Instance
    : ICreatorInstance<TGenotype, TSearchSpace, TProblem>
  {
    public abstract IReadOnlyList<TGenotype> Create(int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
  }
}

public abstract record Creator<TGenotype, TSearchSpace>
  : ICreator<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>>
  where TSearchSpace : class, ISearchSpace<TGenotype>
{
  public abstract ICreatorInstance<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry);
  
  public abstract class Instance
    : ICreatorInstance<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>>
  {
    public abstract IReadOnlyList<TGenotype> Create(int count, IRandomNumberGenerator random, TSearchSpace searchSpace);

    IReadOnlyList<TGenotype> ICreatorInstance<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>>.Create(int count, IRandomNumberGenerator random, TSearchSpace searchSpace, IProblem<TGenotype, TSearchSpace> problem) =>
      Create(count, random, searchSpace);
  }
}

public abstract record Creator<TGenotype>
  : ICreator<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>
{
  public abstract ICreatorInstance<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry);
  
  public abstract class Instance
    : ICreatorInstance<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>
  {
    public abstract IReadOnlyList<TGenotype> Create(int count, IRandomNumberGenerator random);

    IReadOnlyList<TGenotype> ICreatorInstance<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>.Create(int count, IRandomNumberGenerator random, ISearchSpace<TGenotype> searchSpace, IProblem<TGenotype, ISearchSpace<TGenotype>> problem) =>
      Create(count, random);
  }
}
