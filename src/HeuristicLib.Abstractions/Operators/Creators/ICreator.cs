using System.Collections.Concurrent;
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
  TGenotype Create(IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
  
  IReadOnlyList<TGenotype> Create(int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
}

#region Simplified Versions

public interface ICreator<TGenotype, in TSearchSpace>
  : ICreator<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>>//, IOperator<ICreatorInstance<TGenotype, TSearchSpace>>
  where TGenotype : class
  where TSearchSpace : class, ISearchSpace<TGenotype>
{
  ICreatorInstance<TGenotype, TSearchSpace> CreateExecutionInstance(ExecutionScope scope);
  
  //ICreatorInstance<TGenotype, TSearchSpace> IOperator<ICreatorInstance<TGenotype, TSearchSpace>>.CreateExecutionInstance(ExecutionScope scope) => CreateExecutionInstance(scope);
  
  ICreatorInstance<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>> IOperator<ICreatorInstance<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>>>.CreateExecutionInstance(ExecutionScope scope) => CreateExecutionInstance(scope);
}

public interface ICreatorInstance<TGenotype, in TSearchSpace>
  : ICreatorInstance<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>>
  where TGenotype : class
  where TSearchSpace : class, ISearchSpace<TGenotype>
{
  TGenotype Create(IRandomNumberGenerator random, TSearchSpace searchSpace);
  
  IReadOnlyList<TGenotype> Create(int count, IRandomNumberGenerator random, TSearchSpace searchSpace);

  TGenotype ICreatorInstance<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>>.Create(IRandomNumberGenerator random, TSearchSpace searchSpace, IProblem<TGenotype, TSearchSpace> problem) => Create(random, searchSpace);
  
  IReadOnlyList<TGenotype> ICreatorInstance<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>>.Create(int count, IRandomNumberGenerator random, TSearchSpace searchSpace, IProblem<TGenotype, TSearchSpace> problem) => Create(count, random, searchSpace);
}


public interface ICreator<TGenotype>
  : ICreator<TGenotype, ISearchSpace<TGenotype>>//, IOperator<ICreatorInstance<TGenotype>>
  where TGenotype : class
{
  new ICreatorInstance<TGenotype> CreateExecutionInstance(ExecutionScope scope);
  
  //ICreatorInstance<TGenotype> IOperator<ICreatorInstance<TGenotype>>.CreateExecutionInstance(ExecutionScope scope) => CreateExecutionInstance(scope);
  
  ICreatorInstance<TGenotype, ISearchSpace<TGenotype>> ICreator<TGenotype, ISearchSpace<TGenotype>>.CreateExecutionInstance(ExecutionScope scope) => CreateExecutionInstance(scope);
}

public interface ICreatorInstance<TGenotype>
  : ICreatorInstance<TGenotype, ISearchSpace<TGenotype>>
  where TGenotype : class
{
  TGenotype Create(IRandomNumberGenerator random);
  
  IReadOnlyList<TGenotype> Create(int count, IRandomNumberGenerator random);

  TGenotype ICreatorInstance<TGenotype, ISearchSpace<TGenotype>>.Create(IRandomNumberGenerator random, ISearchSpace<TGenotype> searchSpace) => Create(random);
  
  IReadOnlyList<TGenotype> ICreatorInstance<TGenotype, ISearchSpace<TGenotype>>.Create(int count, IRandomNumberGenerator random, ISearchSpace<TGenotype> searchSpace) => Create(count, random);
}

#endregion

#region Batch Executors (move to other file)

public interface IBatchExecutor
{
  IReadOnlyList<T> ExecuteBatch<T>(int count, Func<IRandomNumberGenerator, T> createFunc, IRandomNumberGenerator random);
  IReadOnlyList<T> ExecuteBatch<T>(IReadOnlyList<T> list, Func<IRandomNumberGenerator, T> createFunc, IRandomNumberGenerator random);
}

public static class BatchExecutors
{
  public static readonly IBatchExecutor Sequential = new SequentialBatchExecutor();
  public static readonly IBatchExecutor Parallel = new ParallelBatchExecutor();
  
  private sealed class SequentialBatchExecutor : IBatchExecutor
  {
    public IReadOnlyList<T> ExecuteBatch<T>(int count, Func<IRandomNumberGenerator, T> createFunc, IRandomNumberGenerator random)
    {
      var result = new T[count];
      for (int i = 0; i < count; i++) {
        var rng = random.Fork(i);
        result[i] = createFunc(rng);
      }
      return result;
    }

    public IReadOnlyList<T> ExecuteBatch<T>(IReadOnlyList<T> list, Func<IRandomNumberGenerator, T> createFunc, IRandomNumberGenerator random)
    {
      var result = new T[list.Count];
      for (int i = 0; i < list.Count; i++) {
        var rng = random.Fork(i);
        result[i] = createFunc(rng);
      }
      return result;
    }
  }
  
  
  public sealed class ParallelBatchExecutor : IBatchExecutor
  {
    public IReadOnlyList<T> ExecuteBatch<T>(int count, Func<IRandomNumberGenerator, T> createFunc, IRandomNumberGenerator random)
    {
      var result = new T[count];
      System.Threading.Tasks.Parallel.ForEach(Partitioner.Create(0, count), range => {
        var (start, end) = range;
        for (int i = start; i < end; i++) {
          var rng = random.Fork(i);
          result[i] = createFunc(rng);
        }
      });
      return result;
    }

    public IReadOnlyList<T> ExecuteBatch<T>(IReadOnlyList<T> list, Func<IRandomNumberGenerator, T> createFunc, IRandomNumberGenerator random)
    {
      var result = new T[list.Count];
      System.Threading.Tasks.Parallel.ForEach(Partitioner.Create(0, list.Count), range => {
        var (start, end) = range;
        for (int i = start; i < end; i++) {
          var rng = random.Fork(i);
          result[i] = createFunc(rng);
        }
      });
      return result;
    }
  }
}

#endregion
