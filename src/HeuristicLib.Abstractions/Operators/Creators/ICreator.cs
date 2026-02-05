using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Creators;

public interface ICreator<TGenotype, in TSearchSpace, in TProblem>
  : IOperator<ICreatorInstance<TGenotype, TSearchSpace, TProblem>>
  where TGenotype : class
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{

}

public interface ICreatorInstance<TGenotype, in TSearchSpace, in TProblem>
  : IExecutionInstance
  where TGenotype : class
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  IReadOnlyList<TGenotype> Create(int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
}

#region Simplified Versions

public interface ICreator<TGenotype, in TSearchSpace>
  : ICreator<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>>//, IOperator<ICreatorInstance<TGenotype, TSearchSpace>>
  where TGenotype : class
  where TSearchSpace : class, ISearchSpace<TGenotype>
{
  new ICreatorInstance<TGenotype, TSearchSpace> CreateExecutionInstance(ExecutionScope scope);
  
 ICreatorInstance<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>> IOperator<ICreatorInstance<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>>>.CreateExecutionInstance(ExecutionScope scope) => CreateExecutionInstance(scope);
}

public interface ICreatorInstance<TGenotype, in TSearchSpace>
  : ICreatorInstance<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>>
  where TGenotype : class
  where TSearchSpace : class, ISearchSpace<TGenotype>
{
  IReadOnlyList<TGenotype> Create(int count, IRandomNumberGenerator random, TSearchSpace searchSpace);
  
  IReadOnlyList<TGenotype> ICreatorInstance<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>>.Create(int count, IRandomNumberGenerator random, TSearchSpace searchSpace, IProblem<TGenotype, TSearchSpace> problem) => Create(count, random, searchSpace);
}


public interface ICreator<TGenotype>
  : ICreator<TGenotype, ISearchSpace<TGenotype>>//, IOperator<ICreatorInstance<TGenotype>>
  where TGenotype : class
{
  new ICreatorInstance<TGenotype> CreateExecutionInstance(ExecutionScope scope);
  
  ICreatorInstance<TGenotype, ISearchSpace<TGenotype>> ICreator<TGenotype, ISearchSpace<TGenotype>>.CreateExecutionInstance(ExecutionScope scope) => CreateExecutionInstance(scope);
}

public interface ICreatorInstance<TGenotype>
  : ICreatorInstance<TGenotype, ISearchSpace<TGenotype>>
  where TGenotype : class
{
  IReadOnlyList<TGenotype> Create(int count, IRandomNumberGenerator random);

  IReadOnlyList<TGenotype> ICreatorInstance<TGenotype, ISearchSpace<TGenotype>>.Create(int count, IRandomNumberGenerator random, ISearchSpace<TGenotype> searchSpace) => Create(count, random);
}

#endregion
