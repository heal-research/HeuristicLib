// namespace HEAL.HeuristicLib.Algorithms.MetaAlgorithms;
//
// public abstract class MetaAlgorithm<TState> : AlgorithmBase<TState>where TState : class, IState {
//   public IReadOnlyList<IAlgorithm<TState>> Algorithms { get; }
//   protected MetaAlgorithm(IEnumerable<IAlgorithm<TState>> algorithms) {
//     Algorithms = algorithms.ToList();
//     if (Algorithms.Count == 0) throw new ArgumentException("At least one algorithm must be provided.", nameof(algorithms));
//   }
// }
//
// public class ConcatAlgorithm<TState> : MetaAlgorithm<TState>where TState : class, IGenerationalState  {
//   public ConcatAlgorithm(params IEnumerable<IAlgorithm<TState>> algorithms) : base(algorithms) { }
//   
//   public override ResultStream<TState> CreateExecutionStream(TState? initialState = null) {
//     var stream = InternalCreateExecutionStream(initialState);
//     // if (termination is not null) {
//     //   stream = stream.TakeUntil(termination.ShouldTerminate);
//     // }
//     return new ResultStream<TState>(stream);
//   }
//   private IEnumerable<TState> InternalCreateExecutionStream(TState? initialState) {
//     TState? currentState = initialState;
//     foreach (var algorithm in Algorithms) {
//       var currentStream = algorithm.CreateExecutionStream(currentState);
//       foreach (var state in currentStream) {
//         yield return currentState = state;
//       }
//       if (currentState is null) yield break;
//       currentState = currentState.Reset<TState>();
//     }
//   }
// }
//
// public class CyclicAlgorithm<TState> : MetaAlgorithm<TState> where TState : class, IGenerationalState {
//   public CyclicAlgorithm(params IEnumerable<IAlgorithm<TState>> algorithms) : base(algorithms) { }
//   
//   public override ResultStream<TState> CreateExecutionStream(TState? initialState = null) {
//     //if (termination is null) throw new InvalidOperationException("Cyclic Algorithms require a termination to avoid infinite loops.");
//     var stream = InternalCreateExecutionStream(initialState);
//     //return new ExecutionStream<TState>(stream.TakeWhile(s => termination?.ShouldTerminate(s) ?? true));
//     return new ResultStream<TState>(stream);
//   }
//   
//   #pragma warning disable S2190
//   private IEnumerable<TState> InternalCreateExecutionStream(TState? initialState) {
//     TState? currentState = initialState;
//     while (true) {
//       // CreateExecutionStream always returning an empty stream results in an infinite loop
//       foreach (var algorithm in Algorithms) {
//         var currentStream = algorithm.CreateExecutionStream(currentState);
//         foreach (var state in currentStream) {
//           yield return currentState = state;
//         }
//         if (currentState is null) yield break;
//         currentState = currentState.Reset<TState>();
//       }
//     }
//   }
//   #pragma warning restore S2190
// }
//
//
// public class ConcatAlgorithm<TState, TSourceState, TTargetState> : AlgorithmBase<TState>
//   where TState : class, IState
//   where TSourceState : class, TState 
//   where TTargetState : class, TState {
//   
//   public IAlgorithm<TSourceState> FirstAlgorithm { get; }
//   public IAlgorithm<TTargetState> SecondAlgorithm { get; }
//   public IStateTransformer<TSourceState, TTargetState> Transformer { get; }
//   
//   public ConcatAlgorithm(IAlgorithm<TSourceState> firstAlgorithm, IAlgorithm<TTargetState> secondAlgorithm, IStateTransformer<TSourceState, TTargetState> transformer) {
//     FirstAlgorithm = firstAlgorithm;
//     SecondAlgorithm = secondAlgorithm;
//     Transformer = transformer;
//   }
//   
//   public override ResultStream<TState> CreateExecutionStream(TState? initialState = null) {
//     if (initialState is not null && initialState is not TSourceState) throw new ArgumentException("Initial state must be of type TSourceState.", nameof(initialState));
//     var stream = InternalCreateExecutionStream(initialState as TSourceState);
//     // if (termination is not null) {
//     //   stream = stream.TakeUntil(termination.ShouldTerminate);
//     // }
//     return new ResultStream<TState>(stream);
//   }
//
//   private IEnumerable<TState> InternalCreateExecutionStream(TSourceState? initialState) {
//     TSourceState? currentSourceState = initialState;
//     var sourceStream = FirstAlgorithm.CreateExecutionStream(initialState);
//     foreach (var sourceState in sourceStream) {
//       yield return currentSourceState = sourceState;
//     }
//     var initialTargetState = currentSourceState is not null ? Transformer.Transform(currentSourceState) : null;
//     var targetStream = SecondAlgorithm.CreateExecutionStream(initialTargetState);
//     foreach (var targetState in targetStream) {
//       yield return targetState;
//     }
//   }
// }
//
// public class CyclicAlgorithm<TState, TSourceState, TTargetState> : AlgorithmBase<TState>
//   where TState : class, IState
//   where TSourceState : class, TState
//   where TTargetState : class, TState
// {
//   public IAlgorithm<TSourceState> FirstAlgorithm { get; }
//   public IAlgorithm<TTargetState> SecondAlgorithm { get; }
//   public IStateTransformer<TSourceState, TTargetState> Transformer { get; }
//   public IStateTransformer<TTargetState, TSourceState> RepetitionTransformer { get; }
//   
//   public CyclicAlgorithm(IAlgorithm<TSourceState> firstAlgorithm, IAlgorithm<TTargetState> secondAlgorithm, IStateTransformer<TSourceState, TTargetState> transformer, IStateTransformer<TTargetState, TSourceState> repetitionTransformer) {
//     FirstAlgorithm = firstAlgorithm;
//     SecondAlgorithm = secondAlgorithm;
//     Transformer = transformer;
//     RepetitionTransformer = repetitionTransformer;
//   }
//   
//   public override ResultStream<TState> CreateExecutionStream(TState? initialState = null) {
//     //if (termination is null) throw new InvalidOperationException("Cyclic Algorithms require a termination to avoid infinite loops.");
//     if (initialState is not null && initialState is not TSourceState) throw new ArgumentException("Initial state must be of type TSourceState.", nameof(initialState));
//     
//     var stream = InternalCreateExecutionStream(initialState as TSourceState);
//     return new ResultStream<TState>(stream/*.TakeWhile(state => termination?.ShouldContinue(state) ?? true)*/);
//   }
//
//    #pragma warning disable S2190
//   private IEnumerable<TState> InternalCreateExecutionStream(TSourceState? initialState) {
//     TSourceState? lastSourceState = initialState;
//     TTargetState? lastTargetState = null;
//
//     while (true) {
//       var sourceStream = FirstAlgorithm.CreateExecutionStream(lastSourceState);
//       foreach (var sourceState in sourceStream) {
//         yield return lastSourceState = sourceState;
//       }
//       if (lastSourceState is null) yield break;
//       
//       lastTargetState = Transformer.Transform(lastSourceState, lastTargetState);
//       
//       var targetStream = SecondAlgorithm.CreateExecutionStream(lastTargetState);
//       foreach (var targetState in targetStream) {
//         yield return lastTargetState = targetState;
//       }
//       if (lastTargetState is null) yield break;
//       
//       lastSourceState = RepetitionTransformer.Transform(lastTargetState, lastSourceState);
//     }
//   }
//   #pragma warning restore S2190
// }
