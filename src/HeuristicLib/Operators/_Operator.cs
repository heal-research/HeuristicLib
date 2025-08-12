using System.Collections.Concurrent;
using System.Diagnostics;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators;

// public interface IOperator
// {
// }
//
// public interface IOperator<TGenotype> : IOperator
// {
// }

public interface IExecutionContext
{
  // IEncoding Encoding { get; }
  // IProblem Problem { get; }
  
  IRandomNumberGenerator Random { get; }
  
  IExecutionContext Fork(params int[] keys);
  
  void ParallelFor(int fromInclusive, int toExclusive, Action<IExecutionContext> body);
  IReadOnlyList<T> ParallelFor<T>(int fromInclusive, int toExclusive, Func<IExecutionContext, T> body);
}

public interface IExecutionContext<out TEncoding> : IExecutionContext
  where TEncoding : IEncoding
{
  TEncoding Encoding { get; }

  new IExecutionContext<TEncoding> Fork(params int[] keys);
  
  void ParallelFor(int fromInclusive, int toExclusive, Action<IExecutionContext<TEncoding>> body);
  IReadOnlyList<T> ParallelFor<T>(int fromInclusive, int toExclusive, Func<IExecutionContext<TEncoding>, T> body);
}

public interface IExecutionContext<out TEncoding, out TProblem> : IExecutionContext<TEncoding>
  where TEncoding : IEncoding
  where TProblem : IProblem
{
  TProblem Problem { get; }
  
  new IExecutionContext<TEncoding, TProblem> Fork(params int[] keys);
  
  void ParallelFor(int fromInclusive, int toExclusive, Action<IExecutionContext<TEncoding, TProblem>> body);
  IReadOnlyList<T> ParallelFor<T>(int fromInclusive, int toExclusive, Func<IExecutionContext<TEncoding, TProblem>, T> body);
}


public class ExecutionContext : IExecutionContext
{
  public IRandomNumberGenerator Random { get; }

  public ExecutionContext(IRandomNumberGenerator random) {
    Random = random;
  }
  
  public IExecutionContext Fork(params int[] keys) {
    return new ExecutionContext(Random.Fork(keys));
  }
  
  public void ParallelFor(int fromInclusive, int toExclusive, Action<IExecutionContext> body) {
    var partitioner = Partitioner.Create(fromInclusive, toExclusive);
    
    Parallel.ForEach(partitioner, (range, state, partitionIndex) => {
      var forkedContext = Fork((int)partitionIndex);
      body(forkedContext);
    });
  }
  
  public IReadOnlyList<T> ParallelFor<T>(int fromInclusive, int toExclusive, Func<IExecutionContext, T> body) {
    var results = new T[toExclusive - fromInclusive];
    var partitioner = Partitioner.Create(fromInclusive, toExclusive);
    
    Parallel.ForEach(partitioner, (range, state, partitionIndex) => {
      var forkedContext = Fork((int)partitionIndex);
      for (int i = range.Item1; i < range.Item2; i++) {
        results[i - fromInclusive] = body(forkedContext);
      }
    });
    
    return results;
  }
}

public class ExecutionContext<TEncoding> : ExecutionContext, IExecutionContext<TEncoding>
  where TEncoding : IEncoding
{
  public TEncoding Encoding { get; }

  public ExecutionContext(IRandomNumberGenerator random, TEncoding encoding) : base(random) {
    Encoding = encoding;
  }
  
  public new IExecutionContext<TEncoding> Fork(params int[] keys) {
    return new ExecutionContext<TEncoding>(Random.Fork(keys), Encoding);
  }
  
  public void ParallelFor(int fromInclusive, int toExclusive, Action<IExecutionContext<TEncoding>> body) {
    var partitioner = Partitioner.Create(fromInclusive, toExclusive);
    
    Parallel.ForEach(partitioner, (range, state, partitionIndex) => {
      var forkedContext = Fork((int)partitionIndex);
      body(forkedContext);
    });
  }
  
  public IReadOnlyList<T> ParallelFor<T>(int fromInclusive, int toExclusive, Func<IExecutionContext<TEncoding>, T> body) {
    var results = new T[toExclusive - fromInclusive];
    var partitioner = Partitioner.Create(fromInclusive, toExclusive);
    
    Parallel.ForEach(partitioner, (range, state, partitionIndex) => {
      var forkedContext = Fork((int)partitionIndex);
      for (int i = range.Item1; i < range.Item2; i++) {
        results[i - fromInclusive] = body(forkedContext);
      }
    });
    
    return results;
  }
}

public class ExecutionContext<TEncoding, TProblem> : ExecutionContext<TEncoding>, IExecutionContext<TEncoding, TProblem> 
  where TEncoding : IEncoding
  where TProblem : IProblem
{
  public TProblem Problem { get; }

  public ExecutionContext(IRandomNumberGenerator random, TEncoding encoding, TProblem problem) : base(random, encoding) {
    Problem = problem;
  }
  
  public new IExecutionContext<TEncoding, TProblem> Fork(params int[] keys) {
    return new ExecutionContext<TEncoding, TProblem>(Random.Fork(keys), Encoding, Problem);
  }

  public void ParallelFor(int fromInclusive, int toExclusive, Action<IExecutionContext<TEncoding, TProblem>> body) {
    var partitioner = Partitioner.Create(fromInclusive, toExclusive);
    
    Parallel.ForEach(partitioner, (range, state, partitionIndex) => {
      var forkedContext = Fork((int)partitionIndex);
      body(forkedContext);
    });
  }
  
  public IReadOnlyList<T> ParallelFor<T>(int fromInclusive, int toExclusive, Func<IExecutionContext<TEncoding, TProblem>, T> body) {
    var results = new T[toExclusive - fromInclusive];
    var partitioner = Partitioner.Create(fromInclusive, toExclusive);
    
    Parallel.ForEach(partitioner, (range, state, partitionIndex) => {
      var forkedContext = Fork((int)partitionIndex);
      for (int i = range.Item1; i < range.Item2; i++) {
        results[i - fromInclusive] = body(forkedContext);
      }
    });
    
    return results;
  }
}






public readonly record struct OperatorMetric(int Count, TimeSpan Duration) {
  public static OperatorMetric Aggregate(OperatorMetric left, OperatorMetric right) {
    return new OperatorMetric(left.Count + right.Count, left.Duration + right.Duration);
  }
  public static OperatorMetric operator +(OperatorMetric left, OperatorMetric right) => Aggregate(left, right);
  
  public static OperatorMetric Zero => new(0, TimeSpan.Zero);

  public static OperatorMetric Measure(int count, Action action) {
    long start = Stopwatch.GetTimestamp();
    action();
    long end = Stopwatch.GetTimestamp();

    return new OperatorMetric(count, Stopwatch.GetElapsedTime(start, end));
  }
  
  public static OperatorMetric Measure(Action action) {
    return Measure(1, action);
  }
}
