using System.Collections;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Algorithms;

public interface IAlgorithm { }

public interface IAlgorithm<TState> : IAlgorithm where TState : class, IState  {
  ExecutionStream<TState> CreateExecutionStream(TState? initialState = null);
}

public static class AlgorithmExtensions {
  public static TState? Execute<TState>(this IAlgorithm<TState> algorithm, TState? initialState = null, ITerminator<TState>? terminator = null) where TState : class, IState {
    return algorithm.CreateExecutionStream(initialState).TakeWhile(state => terminator?.ShouldContinue(state) ?? true).LastOrDefault();
  }
}

public abstract class AlgorithmBase<TState> : IAlgorithm<TState> where TState : class, IState {
  public abstract ExecutionStream<TState> CreateExecutionStream(TState? initialState = null);
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
  public static Phenotype<TGenotype>? GetBest<TGenotype>(this IEnumerable<PopulationState<TGenotype>> stream, IComparer<Fitness> comparer) {
    return stream
      .SelectMany(state => state.Population)
      .MinBy(individual => individual.Fitness, comparer);
  }
  public static Phenotype<TGenotype>? GetBestSingleObjective<TGenotype>(this IEnumerable<PopulationState<TGenotype>> stream) {
    var states = stream.ToList();
    var objective = states.Select(state => state.Objective).First().WithSingleObjective();
    return states.GetBest(objective.TotalOrderComparer);
  }


  public static IEnumerable<(Fitness best, Fitness median, Fitness worst)> GetObjectiveStatistics<TGenotype>(this IEnumerable<PopulationState<TGenotype>> stream, IComparer<Fitness> comparer) {
    return stream.Select(state => {
      if (state.Population.Length == 0) throw new InvalidOperationException("Population must not be empty.");
      var fitnessValues = state.Population.Select(p => p.Fitness);
      var orderedFitness = fitnessValues.OrderBy(x => x, comparer).ToArray();
      Fitness best = orderedFitness[0];
      Fitness worst = orderedFitness[^1];
      // ToDo: average only works for single objective
      Fitness median = orderedFitness[orderedFitness.Length / 2];
      return (best, median, worst);
    });
  }
  public static IEnumerable<(SingleFitness best, SingleFitness mean, SingleFitness worst)> GetSingleObjectiveStatisticsStream<TGenotype>(this ExecutionStream<PopulationState<TGenotype>> stream) {
    return stream.Select(state => {
      if (state.Population.Length == 0) throw new InvalidOperationException("Population must not be empty.");
      var objective = state.Objective.WithSingleObjective();
      var comparer = objective.TotalOrderComparer;
      var fitnessValues = state.Population.Select(p => p.Fitness);
      var orderedFitness = fitnessValues.OrderBy(x => x, comparer).ToArray();
      Fitness best = orderedFitness[0];
      Fitness worst = orderedFitness[^1];
      double mean = orderedFitness.Select(x => x.SingleFitness!.Value.Value).Average();
      return (best.SingleFitness!.Value, new SingleFitness(mean), worst.SingleFitness!.Value);
    });
  }
}
