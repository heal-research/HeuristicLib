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
  public static Phenotype<TGenotype, Fitness>? GetBest<TGenotype>(this ExecutionStream<PopulationState<TGenotype, Fitness, Goal>> stream) {
    var goal = stream.Select(state => state.Goal).Single();
    var comparer = Fitness.CreateSingleObjectiveComparer(goal);
    
    var bestPhenotype = stream
      .SelectMany(state => state.Population)
      .MinBy(individual => individual.Fitness, comparer);

    return bestPhenotype is null ? null
      : new Phenotype<TGenotype, Fitness>(bestPhenotype.Genotype, bestPhenotype.Fitness);
  }
  public static Phenotype<TGenotype, TFitness>? GetBest<TGenotype, TFitness, TGoal>(this ExecutionStream<PopulationState<TGenotype, TFitness, TGoal>> stream, IComparer<TFitness> comparer) {
    return stream
      .SelectMany(state => state.Population)
      .MinBy(individual => individual.Fitness, comparer);
  }

  public static IEnumerable<(Fitness best, Fitness average, Fitness worst)> GetSingleObjectiveStatisticsStream<TGenotype>(this ExecutionStream<PopulationState<TGenotype, Fitness, Goal>> stream) {
    return stream.Select(state => {
      if (state.Population.Length == 0) throw new InvalidOperationException("Population must not be empty.");
      var comparer = Fitness.CreateSingleObjectiveComparer(state.Goal);
      var fitnessValues = state.Population.Select(p => p.Fitness).ToArray();
      return (fitnessValues.MinBy(x => x, comparer), new Fitness(fitnessValues.Select(x => x.Value).Average()), fitnessValues.MaxBy(x => x, comparer));
    });
  }
}
