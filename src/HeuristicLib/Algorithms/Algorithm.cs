using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Algorithms;

public interface IAlgorithm { }

public interface IAlgorithm<in TStartState, out TResultState> : IAlgorithm 
  where TStartState : IStartState where TResultState : IResultState
{
  TResultState Execute(IRandomNumberGenerator random, TStartState? startState = default);
}

public interface IContinuableAlgorithm<in TStartState, in TContinuationState, out TResultState> : IAlgorithm<TStartState, TResultState> 
  where TStartState : IStartState
  where TContinuationState : IContinuationState 
  where TResultState : IContinuableResultState<TContinuationState>
{
  TResultState Execute(IRandomNumberGenerator random, TContinuationState continuationState);
}

public static class AlgorithmExtensions {
  public static IEnumerable<TResultState> CreateResultStream<TStartState, TContinuationState, TResultState>(
    this IContinuableAlgorithm<TStartState, TContinuationState, TResultState> algorithm, IRandomNumberGenerator random, TStartState? startState = default, ITerminator<TResultState>? terminator = null)
    where TStartState : IStartState 
    where TContinuationState : IContinuationState
    where TResultState : IContinuableResultState<TContinuationState>
  {
    var currentResult = algorithm.Execute(random, startState);
    yield return currentResult;
    while (terminator?.ShouldContinue(currentResult) ?? true) {
      var continuationState = currentResult.GetNextContinuationState();
      currentResult = algorithm.Execute(random, continuationState);
      yield return currentResult;
    }
  }
  public static IEnumerable<TResultState> CreateResultStream<TStartState, TContinuationState, TResultState>(
    this IContinuableAlgorithm<TStartState, TContinuationState, TResultState> algorithm, IRandomNumberGenerator random, TContinuationState continuationState, ITerminator<TResultState>? terminator = null)
    where TStartState : IStartState 
    where TContinuationState : IContinuationState
    where TResultState : IContinuableResultState<TContinuationState> {
    TResultState currentResult;
    var currentContinuationState = continuationState;
    do {
      currentResult = algorithm.Execute(random, currentContinuationState);
      yield return currentResult;
      currentContinuationState = currentResult.GetNextContinuationState();
    } while (terminator?.ShouldContinue(currentResult) ?? true);
  }
  
  public static TResultState Execute<TStartState, TContinuationState, TResultState>(this IContinuableAlgorithm<TStartState, TContinuationState, TResultState> algorithm, IRandomNumberGenerator random, ITerminator<TResultState> terminator, TStartState? startState = default)
    where TStartState : IStartState where TContinuationState : IContinuationState where TResultState : IContinuableResultState<TContinuationState> 
  {
    return algorithm.CreateResultStream(random, startState, terminator).Last();
  }
  public static TResultState Execute<TStartState, TContinuationState, TResultState>(this IContinuableAlgorithm<TStartState, TContinuationState, TResultState> algorithm, IRandomNumberGenerator random, ITerminator<TResultState> terminator, TContinuationState continuationState)
    where TStartState : IStartState where TContinuationState : IContinuationState where TResultState : IContinuableResultState<TContinuationState> 
  {
    return algorithm.CreateResultStream(random, continuationState, terminator).Last();
  }
}

public abstract class AlgorithmBase<TStartState, TResultState> : IAlgorithm<TStartState, TResultState>
  where TStartState : IStartState 
  where TResultState : IResultState 
{
  public abstract TResultState Execute(IRandomNumberGenerator random, TStartState? startState = default);
}

public abstract class AlgorithmBase<TStartState, TContinuationState, TResultState> : AlgorithmBase<TStartState, TResultState>, IContinuableAlgorithm<TStartState, TContinuationState, TResultState>
  where TStartState : IStartState 
  where TContinuationState : IContinuationState 
  where TResultState : IContinuableResultState<TContinuationState> 
{
  public abstract TResultState Execute(IRandomNumberGenerator random, TContinuationState continuationState);
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
