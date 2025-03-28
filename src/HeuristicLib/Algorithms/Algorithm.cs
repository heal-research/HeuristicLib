using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Algorithms;

public interface IAlgorithm { }

public interface IAlgorithm<out TGenotype, in TEncodingParameter, in TStartState, out TResultState> : IAlgorithm 
  where TStartState : IStartState where TResultState : IResultState
  where TEncodingParameter : IEncodingParameter<TGenotype>
{
  TResultState Execute(IRandomNumberGenerator random, TEncodingParameter encodingParameter, TStartState? startState = default);
}

public interface IContinuableAlgorithm<out TGenotype, in TEncodingParameter, in TStartState, in TContinuationState, out TResultState> : IAlgorithm<TGenotype, TEncodingParameter, TStartState, TResultState> 
  where TStartState : IStartState
  where TContinuationState : IContinuationState 
  where TResultState : IContinuableResultState<TContinuationState>
  where TEncodingParameter : IEncodingParameter<TGenotype>
{
  TResultState Execute(IRandomNumberGenerator random, TEncodingParameter encodingParameter, TContinuationState continuationState);
}

public static class AlgorithmExtensions {
  public static IEnumerable<TResultState> CreateResultStream
    <TGenotype, TEncodingParameter, TStartState, TContinuationState, TResultState>
    (this IContinuableAlgorithm<TGenotype, TEncodingParameter, TStartState, TContinuationState, TResultState> algorithm, IRandomNumberGenerator random, TEncodingParameter encodingParameter, TStartState? startState = default, ITerminator<TResultState>? terminator = null)
    where TStartState : IStartState 
    where TContinuationState : IContinuationState
    where TResultState : IContinuableResultState<TContinuationState>
    where TEncodingParameter : IEncodingParameter<TGenotype>
  {
    var currentResult = algorithm.Execute(random, encodingParameter, startState);
    yield return currentResult;
    while (terminator?.ShouldContinue(currentResult) ?? true) {
      var continuationState = currentResult.GetNextContinuationState();
      currentResult = algorithm.Execute(random, encodingParameter, continuationState);
      yield return currentResult;
    }
  }
  public static IEnumerable<TResultState> CreateResultStream
    <TGenotype, TEncodingParameter, TStartState, TContinuationState, TResultState>
    (this IContinuableAlgorithm<TGenotype, TEncodingParameter, TStartState, TContinuationState, TResultState> algorithm, IRandomNumberGenerator random, TEncodingParameter encodingParameter, TContinuationState continuationState, ITerminator<TResultState>? terminator = null)
    where TStartState : IStartState 
    where TContinuationState : IContinuationState
    where TResultState : IContinuableResultState<TContinuationState>
    where TEncodingParameter : IEncodingParameter<TGenotype>
  {
    TResultState currentResult;
    var currentContinuationState = continuationState;
    do {
      currentResult = algorithm.Execute(random, encodingParameter, currentContinuationState);
      yield return currentResult;
      currentContinuationState = currentResult.GetNextContinuationState();
    } while (terminator?.ShouldContinue(currentResult) ?? true);
  }
  
  public static TResultState Execute
    <TGenotype, TEncodingParameter, TStartState, TContinuationState, TResultState>
    (this IContinuableAlgorithm<TGenotype, TEncodingParameter, TStartState, TContinuationState, TResultState> algorithm, IRandomNumberGenerator random, TEncodingParameter encodingParameter, ITerminator<TResultState> terminator, TStartState? startState = default)
    where TStartState : IStartState where TContinuationState : IContinuationState where TResultState : IContinuableResultState<TContinuationState> 
    where TEncodingParameter : IEncodingParameter<TGenotype>
  {
    return algorithm.CreateResultStream(random, encodingParameter, startState, terminator).Last();
  }
  public static TResultState Execute
    <TGenotype, TEncodingParameter, TStartState, TContinuationState, TResultState>
    (this IContinuableAlgorithm<TGenotype, TEncodingParameter, TStartState, TContinuationState, TResultState> algorithm, IRandomNumberGenerator random, TEncodingParameter encodingParameter, ITerminator<TResultState> terminator, TContinuationState continuationState)
    where TStartState : IStartState where TContinuationState : IContinuationState where TResultState : IContinuableResultState<TContinuationState>
    where TEncodingParameter : IEncodingParameter<TGenotype>
  {
    return algorithm.CreateResultStream(random, encodingParameter, continuationState, terminator).Last();
  }
}

public abstract class AlgorithmBase<TGenotype, TEncodingParameter, TStartState, TResultState> 
  : IAlgorithm<TGenotype, TEncodingParameter, TStartState, TResultState>
  where TStartState : IStartState
  where TResultState : IResultState
  where TEncodingParameter : IEncodingParameter<TGenotype>
{
  public abstract TResultState Execute(IRandomNumberGenerator random, TEncodingParameter encodingParameter, TStartState? startState = default);
}

public abstract class AlgorithmBase<TGenotype, TEncodingParameter, TStartState, TContinuationState, TResultState> 
  : AlgorithmBase<TGenotype, TEncodingParameter, TStartState, TResultState>, 
    IContinuableAlgorithm<TGenotype, TEncodingParameter, TStartState, TContinuationState, TResultState>
  where TStartState : IStartState 
  where TContinuationState : IContinuationState 
  where TResultState : IContinuableResultState<TContinuationState> 
  where TEncodingParameter : IEncodingParameter<TGenotype>
{
  public abstract TResultState Execute(IRandomNumberGenerator random, TEncodingParameter encodingParameter, TContinuationState continuationState);
}


public interface IBoundAlgorithm<TGenotype, out TEncodingParameter> : IAlgorithm
  where TEncodingParameter : IEncodingParameter<TGenotype>
{
  TEncodingParameter EncodingParameter { get; }
}

public interface IBoundAlgorithm<TGenotype, out TEncodingParameter, in TStartState, out TResultState> : IBoundAlgorithm<TGenotype, TEncodingParameter> 
  where TStartState : IStartState where TResultState : IResultState
  where TEncodingParameter : IEncodingParameter<TGenotype>
{
  TResultState Execute(IRandomNumberGenerator random, TStartState? startState = default);
}

public interface IContinuableBoundAlgorithm<TGenotype, out TEncodingParameter, in TStartState, in TContinuationState, out TResultState> 
  : IBoundAlgorithm<TGenotype, TEncodingParameter, TStartState, TResultState> 
  where TStartState : IStartState where TContinuationState : IContinuationState where TResultState : IContinuableResultState<TContinuationState>
  where TEncodingParameter : IEncodingParameter<TGenotype>
{
  TResultState Execute(IRandomNumberGenerator random, TContinuationState continuationState = default);
}

public class BoundAlgorithm<TGenotype, TEncodingParameter, TStartState, TResultState> 
  : IBoundAlgorithm<TGenotype, TEncodingParameter, TStartState, TResultState>
  where TEncodingParameter : IEncodingParameter<TGenotype>
  where TStartState : IStartState where TResultState : IResultState
{
  public IAlgorithm<TGenotype, TEncodingParameter, TStartState, TResultState> Algorithm { get; }
  public TEncodingParameter EncodingParameter { get; }
  
  public BoundAlgorithm(IAlgorithm<TGenotype, TEncodingParameter, TStartState, TResultState> algorithm, TEncodingParameter encodingParameter) {
    Algorithm = algorithm;
    EncodingParameter = encodingParameter;
  }
  public TResultState Execute(IRandomNumberGenerator random, TStartState? startState = default) {
    return Algorithm.Execute(random, EncodingParameter, startState);
  }
}

public class ContinuableBoundAlgorithm<TGenotype, TEncodingParameter, TStartState, TContinuationState, TResultState> 
  : BoundAlgorithm<TGenotype, TEncodingParameter, TStartState, TResultState>,
    IContinuableBoundAlgorithm<TGenotype, TEncodingParameter, TStartState, TContinuationState, TResultState>
  where TEncodingParameter : IEncodingParameter<TGenotype>
  where TStartState : IStartState where TContinuationState : IContinuationState where TResultState : IContinuableResultState<TContinuationState>
{
  public new IContinuableAlgorithm<TGenotype, TEncodingParameter, TStartState, TContinuationState, TResultState> Algorithm { get; }
  
  public ContinuableBoundAlgorithm(IContinuableAlgorithm<TGenotype, TEncodingParameter, TStartState, TContinuationState, TResultState> algorithm, TEncodingParameter encodingParameter) 
    : base(algorithm, encodingParameter)
  {
    Algorithm = algorithm;
  }
  public TResultState Execute(IRandomNumberGenerator random, TContinuationState continuationState = default) {
    return Algorithm.Execute(random, EncodingParameter, continuationState);
  }
}

public static class BoundAlgorithmExtensions {
  public static BoundAlgorithm<TGenotype, TEncodingParameter, TStartState, TResultState> BindTo
    <TGenotype, TEncodingParameter, TStartState, TResultState>
    (this IAlgorithm<TGenotype, TEncodingParameter, TStartState, TResultState> algorithm, TEncodingParameter encodingParameter)
    where TStartState : IStartState where TResultState : IResultState
    where TEncodingParameter : IEncodingParameter<TGenotype>
  {
    return new BoundAlgorithm<TGenotype, TEncodingParameter, TStartState, TResultState>(algorithm, encodingParameter);
  }
  
  public static ContinuableBoundAlgorithm<TGenotype, TEncodingParameter, TStartState, TContinuationState, TResultState> BindTo
    <TGenotype, TEncodingParameter, TStartState, TContinuationState, TResultState>
    (this IContinuableAlgorithm<TGenotype, TEncodingParameter, TStartState, TContinuationState, TResultState> algorithm, TEncodingParameter encodingParameter)
    where TStartState : IStartState 
    where TContinuationState : IContinuationState
    where TResultState : IContinuableResultState<TContinuationState>
    where TEncodingParameter : IEncodingParameter<TGenotype>
  {
    return new ContinuableBoundAlgorithm<TGenotype, TEncodingParameter, TStartState, TContinuationState, TResultState>(algorithm, encodingParameter);
  }
  
  public static IEnumerable<TResultState> CreateResultStream
    <TGenotype, TEncodingParameter, TStartState, TContinuationState, TResultState>
    (this IContinuableBoundAlgorithm<TGenotype, TEncodingParameter, TStartState, TContinuationState, TResultState> algorithm, IRandomNumberGenerator random, TStartState? startState = default, ITerminator<TResultState>? terminator = null)
    where TStartState : IStartState 
    where TContinuationState : IContinuationState
    where TResultState : IContinuableResultState<TContinuationState>
    where TEncodingParameter : IEncodingParameter<TGenotype>
  {
    var currentResult = algorithm.Execute(random, startState);
    yield return currentResult;
    while (terminator?.ShouldContinue(currentResult) ?? true) {
      var continuationState = currentResult.GetNextContinuationState();
      currentResult = algorithm.Execute(random, continuationState);
      yield return currentResult;
    }
  }
  public static IEnumerable<TResultState> CreateResultStream
    <TGenotype, TEncodingParameter, TStartState, TContinuationState, TResultState>
    (this IContinuableBoundAlgorithm<TGenotype, TEncodingParameter, TStartState, TContinuationState, TResultState> algorithm, IRandomNumberGenerator random, TContinuationState continuationState, ITerminator<TResultState>? terminator = null)
    where TStartState : IStartState 
    where TContinuationState : IContinuationState
    where TResultState : IContinuableResultState<TContinuationState>
    where TEncodingParameter : IEncodingParameter<TGenotype>
  {
    TResultState currentResult;
    var currentContinuationState = continuationState;
    do {
      currentResult = algorithm.Execute(random, currentContinuationState);
      yield return currentResult;
      currentContinuationState = currentResult.GetNextContinuationState();
    } while (terminator?.ShouldContinue(currentResult) ?? true);
  }
  
  public static TResultState Execute
    <TGenotype, TEncodingParameter, TStartState, TContinuationState, TResultState>
    (this IContinuableBoundAlgorithm<TGenotype, TEncodingParameter, TStartState, TContinuationState, TResultState> algorithm, IRandomNumberGenerator random, ITerminator<TResultState> terminator, TStartState? startState = default)
    where TStartState : IStartState where TContinuationState : IContinuationState where TResultState : IContinuableResultState<TContinuationState> 
    where TEncodingParameter : IEncodingParameter<TGenotype>
  {
    return algorithm.CreateResultStream(random, startState, terminator).Last();
  }
  public static TResultState Execute
    <TGenotype, TEncodingParameter, TStartState, TContinuationState, TResultState>
    (this IContinuableBoundAlgorithm<TGenotype, TEncodingParameter, TStartState, TContinuationState, TResultState> algorithm, IRandomNumberGenerator random, ITerminator<TResultState> terminator, TContinuationState continuationState)
    where TStartState : IStartState where TContinuationState : IContinuationState where TResultState : IContinuableResultState<TContinuationState>
    where TEncodingParameter : IEncodingParameter<TGenotype>
  {
    return algorithm.CreateResultStream(random, continuationState, terminator).Last();
  }
}


//
// public static class ExecutionStreamExtensions {
//   public static Solution<TGenotype>? GetBest<TGenotype>(this IEnumerable<PopulationState<TGenotype>> stream, IComparer<Fitness> comparer) {
//     return stream
//       .SelectMany(state => state.Population)
//       .MinBy(individual => individual.Fitness, comparer);
//   }
//   public static Solution<TGenotype>? GetBestSingleObjective<TGenotype>(this IEnumerable<PopulationState<TGenotype>> stream) {
//     var states = stream.ToList();
//     var objective = states.Select(state => state.Objective).First().WithSingleObjective();
//     return states.GetBest(objective.TotalOrderComparer);
//   }
//
//
//   public static IEnumerable<(Fitness best, Fitness median, Fitness worst)> GetObjectiveStatistics<TGenotype>(this IEnumerable<PopulationState<TGenotype>> stream, IComparer<Fitness> comparer) {
//     return stream.Select(state => {
//       if (state.Population.Length == 0) throw new InvalidOperationException("Population must not be empty.");
//       var fitnessValues = state.Population.Select(p => p.Fitness);
//       var orderedFitness = fitnessValues.OrderBy(x => x, comparer).ToArray();
//       Fitness best = orderedFitness[0];
//       Fitness worst = orderedFitness[^1];
//       // ToDo: average only works for single objective
//       Fitness median = orderedFitness[orderedFitness.Length / 2];
//       return (best, median, worst);
//     });
//   }
//   public static IEnumerable<(SingleFitness best, SingleFitness mean, SingleFitness worst)> GetSingleObjectiveStatisticsStream<TGenotype>(this ResultStream<PopulationState<TGenotype>> stream) {
//     return stream.Select(state => {
//       if (state.Population.Length == 0) throw new InvalidOperationException("Population must not be empty.");
//       var objective = state.Objective.WithSingleObjective();
//       var comparer = objective.TotalOrderComparer;
//       var fitnessValues = state.Population.Select(p => p.Fitness);
//       var orderedFitness = fitnessValues.OrderBy(x => x, comparer).ToArray();
//       Fitness best = orderedFitness[0];
//       Fitness worst = orderedFitness[^1];
//       double mean = orderedFitness.Select(x => x.SingleFitness!.Value.Value).Average();
//       return (best.SingleFitness!.Value, new SingleFitness(mean), worst.SingleFitness!.Value);
//     });
//   }
// }
