using System.Collections.Concurrent;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Execution;

// Note on usage: 
// Per default, most operators will use the Sequential execution to simplify batch creation.
// If an operator is parallel-safe, it can override the default implementation and offer a Parallel execution option.
// It then should offer a MaxDegreeOfParallelism parameter to control the degree of parallelism and a sensible default. (e.g. -1 for (probably expensive) evaluators, 1 for most others)
// If the degree of parallelism is set to 1, the Parallel execution method should internally fall back to the Sequential implementation to avoid overhead.

public static class BatchExecution
{
  public static IReadOnlyList<T> Sequential<T>(int count, Func<IRandomNumberGenerator, T> func, IRandomNumberGenerator random)
  {
    var result = new T[count];
    for (int i = 0; i < count; i++) {
      var rng = random.Fork(i);
      result[i] = func(rng);
    }
    return result;
  }

  public static IReadOnlyList<T> Sequential<T>(IReadOnlyList<T> list, Func<IRandomNumberGenerator, T> func, IRandomNumberGenerator random)
  {
    var result = new T[list.Count];
    for (int i = 0; i < list.Count; i++) {
      var rng = random.Fork(i);
      result[i] = func(rng);
    }
    return result;
  }
  
  public static IReadOnlyList<T> Parallel<T>(int count, Func<IRandomNumberGenerator, T> func, IRandomNumberGenerator random, int maxDegreeOfParallelism)
  {
    ArgumentOutOfRangeException.ThrowIfLessThan(maxDegreeOfParallelism, 1);

    if (maxDegreeOfParallelism == 1) {
      return Sequential(count, func, random);
    }
    
    var partitions = Partitioner.Create(0, count);
    var options = new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism };
    var result = new T[count];
    System.Threading.Tasks.Parallel.ForEach(partitions, options, range => {
      var (start, end) = range;
      for (int i = start; i < end; i++) {
        var rng = random.Fork(i);
        result[i] = func(rng);
      }
    });
    return result;
  }

  public static IReadOnlyList<T> Parallel<T>(IReadOnlyList<T> list, Func<IRandomNumberGenerator, T> func, IRandomNumberGenerator random, int maxDegreeOfParallelism)
  {
    ArgumentOutOfRangeException.ThrowIfLessThan(maxDegreeOfParallelism, 1);

    if (maxDegreeOfParallelism == 1) {
      return Sequential(list, func, random);
    }
    var partitions = Partitioner.Create(0, list.Count);
    var options = new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism };
    var result = new T[list.Count];
    System.Threading.Tasks.Parallel.ForEach(partitions, options, range => {
      var (start, end) = range;
      for (int i = start; i < end; i++) {
        var rng = random.Fork(i);
        result[i] = func(rng);
      }
    });
    return result;
  }
}
