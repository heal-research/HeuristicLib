using System.Collections;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Algorithms;

public interface IAlgorithm { }

public interface IAlgorithm<TState> : IAlgorithm where TState : class {
  TState? Execute(TState? initialState = null, ITerminator<TState>? termination = null);
  ExecutionStream<TState> CreateExecutionStream(TState? initialState = null, ITerminator<TState>? termination = null);
}

public abstract class AlgorithmBase<TState> : IAlgorithm<TState> where TState : class {
  public abstract TState? Execute(TState? initialState = null, ITerminator<TState>? termination = null);
  
  public abstract ExecutionStream<TState> CreateExecutionStream(TState? initialState = null, ITerminator<TState>? termination = null);
}

public class ExecutionStream<TState> : IEnumerable<TState> {
  private readonly IEnumerable<TState> internalStream;
  public ExecutionStream(IEnumerable<TState> internalStream) {
    this.internalStream = internalStream;
  }
  public IEnumerator<TState> GetEnumerator() => internalStream.GetEnumerator();
  IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

public static class ExecutionStreamExtensions {
  public static (TGenotype, ObjectiveValue) GetBest<TGenotype>(this ExecutionStream<PopulationState<TGenotype>> stream) {
    return stream
      .SelectMany(state => state.Population.Zip(state.Objectives, (genotype, objective) => (genotype, objective)))
      .MinBy(pair => pair.objective);
  }
  
  public static IEnumerable<(ObjectiveValue best, ObjectiveValue average, ObjectiveValue worst)> GetObjectiveStatisticsStream<TGenotype>(this ExecutionStream<PopulationState<TGenotype>> stream) {
    return stream.Select(state => {
      if (state.Objectives.Length == 0) throw new InvalidOperationException("Population must not be empty.");
      return (state.Objectives.Min(), new ObjectiveValue(state.Objectives.Average(o => o.Value), state.Objectives[0].Direction), state.Objectives.Max());
    });
  }
  
  public static IEnumerable<ObjectiveValue> GetBestObjectiveStream<TGenotype>(this ExecutionStream<PopulationState<TGenotype>> stream) {
    var bestObjectives = stream.Select(state => state.Objectives.Min());
    using var enumerator = bestObjectives.GetEnumerator();

    if (!enumerator.MoveNext()) yield break;
    var currentBest = enumerator.Current;
    while (enumerator.MoveNext()) {
      ObjectiveValue current = enumerator.Current;
      if (current.IsBetterThan(currentBest)) {
        currentBest = current;
      }
      yield return currentBest;
    }
    

  }
}
