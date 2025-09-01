using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators;

public class ExecutionContext {
  public IRandomNumberGenerator Random { get; }

  public CancellationToken CancellationToken { get; init; }

  //public ParallelOptions ParallelOptions { get; }

  public ExecutionContext(IRandomNumberGenerator random, CancellationToken? cancellationToken = null) {
    Random = random;
    CancellationToken = cancellationToken ?? CancellationToken.None;
    //ParallelOptions = parallelOptions ?? new ParallelOptions { CancellationToken = CancellationToken };
  }

  public ExecutionContext Fork(int key) {
    return new ExecutionContext(Random.Fork(key), CancellationToken);
  }

  public IReadOnlyList<ExecutionContext> Spawn(int count) {
    var contexts = new ExecutionContext[count];
    var randoms = Random.Spawn(count);
    for (int i = 0; i < count; i++) {
      contexts[i] = new ExecutionContext(randoms[i], CancellationToken);
    }

    return contexts;
  }

  // public void ParallelFor(int fromInclusive, int toExclusive, Action<IExecutionContext> body) {
  //   var partitioner = Partitioner.Create(fromInclusive, toExclusive);
  //   
  //   Parallel.ForEach(partitioner, (range, state, partitionIndex) => {
  //     var forkedContext = Fork((int)partitionIndex);
  //     body(forkedContext);
  //   });
  // }
  //
  // public IReadOnlyList<T> ParallelFor<T>(int fromInclusive, int toExclusive, Func<IExecutionContext, T> body) {
  //   var results = new T[toExclusive - fromInclusive];
  //   var partitioner = Partitioner.Create(fromInclusive, toExclusive);
  //   
  //   Parallel.ForEach(partitioner, (range, state, partitionIndex) => {
  //     var forkedContext = Fork((int)partitionIndex);
  //     for (int i = range.Item1; i < range.Item2; i++) {
  //       results[i - fromInclusive] = body(forkedContext);
  //     }
  //   });
  //   
  //   return results;
  // }
}

// public interface IExecutionContext
// {
//   // IEncoding Encoding { get; }
//   // IProblem Problem { get; }
//   
//   IRandomNumberGenerator Random { get; }
//   
//   CancellationToken CancellationToken { get; }
//   
//   //ParallelOptions ParallelOptions { get; }
//   
//   IExecutionContext Fork(int key);
//   IReadOnlyList<IExecutionContext> Spawn(int count);
//
//
//   //void ParallelFor(int fromInclusive, int toExclusive, Action<IExecutionContext> body);
//   //IReadOnlyList<T> ParallelFor<T>(int fromInclusive, int toExclusive, Func<IExecutionContext, T> body);
// }

// public interface IExecutionContext<out TEncoding> : IExecutionContext
//   where TEncoding : IEncoding
// {
//   TEncoding Encoding { get; }
//
//   new IExecutionContext<TEncoding> Fork(params int[] keys);
//   
//   void ParallelFor(int fromInclusive, int toExclusive, Action<IExecutionContext<TEncoding>> body);
//   IReadOnlyList<T> ParallelFor<T>(int fromInclusive, int toExclusive, Func<IExecutionContext<TEncoding>, T> body);
// }
//
// public interface IExecutionContext<out TEncoding, out TProblem> : IExecutionContext<TEncoding>
//   where TEncoding : IEncoding
//   where TProblem : IProblem
// {
//   TProblem Problem { get; }
//   
//   new IExecutionContext<TEncoding, TProblem> Fork(params int[] keys);
//   
//   void ParallelFor(int fromInclusive, int toExclusive, Action<IExecutionContext<TEncoding, TProblem>> body);
//   IReadOnlyList<T> ParallelFor<T>(int fromInclusive, int toExclusive, Func<IExecutionContext<TEncoding, TProblem>, T> body);
// }

// public class ExecutionContext<TEncoding> : ExecutionContext, IExecutionContext<TEncoding>
//   where TEncoding : IEncoding
// {
//   public TEncoding Encoding { get; }
//
//   public ExecutionContext(IRandomNumberGenerator random, TEncoding encoding) : base(random) {
//     Encoding = encoding;
//   }
//   
//   public new IExecutionContext<TEncoding> Fork(params int[] keys) {
//     return new ExecutionContext<TEncoding>(Random.Fork(keys), Encoding);
//   }
//   
//   public void ParallelFor(int fromInclusive, int toExclusive, Action<IExecutionContext<TEncoding>> body) {
//     var partitioner = Partitioner.Create(fromInclusive, toExclusive);
//     
//     Parallel.ForEach(partitioner, (range, state, partitionIndex) => {
//       var forkedContext = Fork((int)partitionIndex);
//       body(forkedContext);
//     });
//   }
//   
//   public IReadOnlyList<T> ParallelFor<T>(int fromInclusive, int toExclusive, Func<IExecutionContext<TEncoding>, T> body) {
//     var results = new T[toExclusive - fromInclusive];
//     var partitioner = Partitioner.Create(fromInclusive, toExclusive);
//     
//     Parallel.ForEach(partitioner, (range, state, partitionIndex) => {
//       var forkedContext = Fork((int)partitionIndex);
//       for (int i = range.Item1; i < range.Item2; i++) {
//         results[i - fromInclusive] = body(forkedContext);
//       }
//     });
//     
//     return results;
//   }
// }
//
// public class ExecutionContext<TEncoding, TProblem> : ExecutionContext<TEncoding>, IExecutionContext<TEncoding, TProblem> 
//   where TEncoding : IEncoding
//   where TProblem : IProblem
// {
//   public TProblem Problem { get; }
//
//   public ExecutionContext(IRandomNumberGenerator random, TEncoding encoding, TProblem problem) : base(random, encoding) {
//     Problem = problem;
//   }
//   
//   public new IExecutionContext<TEncoding, TProblem> Fork(params int[] keys) {
//     return new ExecutionContext<TEncoding, TProblem>(Random.Fork(keys), Encoding, Problem);
//   }
//
//   public void ParallelFor(int fromInclusive, int toExclusive, Action<IExecutionContext<TEncoding, TProblem>> body) {
//     var partitioner = Partitioner.Create(fromInclusive, toExclusive);
//     
//     Parallel.ForEach(partitioner, (range, state, partitionIndex) => {
//       var forkedContext = Fork((int)partitionIndex);
//       body(forkedContext);
//     });
//   }
//   
//   public IReadOnlyList<T> ParallelFor<T>(int fromInclusive, int toExclusive, Func<IExecutionContext<TEncoding, TProblem>, T> body) {
//     var results = new T[toExclusive - fromInclusive];
//     var partitioner = Partitioner.Create(fromInclusive, toExclusive);
//     
//     Parallel.ForEach(partitioner, (range, state, partitionIndex) => {
//       var forkedContext = Fork((int)partitionIndex);
//       for (int i = range.Item1; i < range.Item2; i++) {
//         results[i - fromInclusive] = body(forkedContext);
//       }
//     });
//     
//     return results;
//   }
// }
