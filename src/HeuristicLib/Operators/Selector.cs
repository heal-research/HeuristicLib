namespace HEAL.HeuristicLib.Operators;

using Algorithms;
using System.Linq;


public interface ISelector<TSolution, in TObjective> : IOperator {
  TSolution[] Select(TSolution[] population, TObjective[] objectives, int count);
}

public interface ISelectorTemplate<out TSelector, TSolution, TObjective, in TParams> 
  : IOperatorTemplate<TSelector, TParams>
  where TSelector : ISelector<TSolution, TObjective>
  where TParams : SelectorParameters {
}

public record SelectorParameters();

public abstract class SelectorBase<TSolution, TObjective> 
  : ISelector<TSolution, TObjective> {
  public abstract TSolution[] Select(TSolution[] population, TObjective[] objectives, int count);
}

public abstract class SelectorTemplateBase<TSelector, TSolution, TObjective, TParams> 
  : ISelectorTemplate<TSelector, TSolution, TObjective, TParams>
  where TSelector : ISelector<TSolution, TObjective>
  where TParams : SelectorParameters {
  public abstract TSelector Parameterize(TParams parameters);
}

public class ProportionalSelector<TSolution> : ISelector<TSolution, ObjectiveValue> {
  public ProportionalSelector(RandomSource randomSource) {
    RandomSource = randomSource;
  }
  public RandomSource RandomSource { get; }

  public TSolution[] Select(TSolution[] population, ObjectiveValue[] objectives, int count) {
    var rng = RandomSource.CreateRandomNumberGenerator();
    var selected = new TSolution[count];
    for (int j = 0; j < count; j++) {
      double totalQuality = objectives.Sum(o => o.Value);
      double randomValue = rng.Random() * totalQuality;
      double cumulativeQuality = 0.0;

      for (int i = 0; i < population.Length(); i++) {
        cumulativeQuality += objectives[i].Value;
        if (cumulativeQuality >= randomValue) {
          selected[j] = population[i];
          break;
        }
      }
    }
    return selected;
  }
  
  public record Parameters(RandomSource RandomSource) : SelectorParameters;

  public class Template : SelectorTemplateBase<ProportionalSelector<TSolution>, TSolution, ObjectiveValue, Parameters> {
    public override ProportionalSelector<TSolution> Parameterize(Parameters parameters) {
      return new ProportionalSelector<TSolution>(parameters.RandomSource);
    }
  }
}




public class RandomSelector<TSolution, TObjective> : SelectorBase<TSolution, TObjective> {
  public RandomSelector(RandomSource randomSource) {
    RandomSource = randomSource;
  }
  public RandomSource RandomSource { get; }

  public override TSolution[] Select(TSolution[] population, TObjective[] objectives, int count) {
    var rng = RandomSource.CreateRandomNumberGenerator();
    var selected = new TSolution[count];
    for (int i = 0; i < count; i++) {
      int index = rng.Integer(population.Length);
      selected[i] = population[index];
    }
    return selected;
  }
  
  public record Parameters(RandomSource RandomSource) : SelectorParameters;

  public class Template : SelectorTemplateBase<RandomSelector<TSolution, TObjective>, TSolution, TObjective, Parameters> {
    public override RandomSelector<TSolution, TObjective> Parameterize(Parameters parameters) {
      return new RandomSelector<TSolution, TObjective>(parameters.RandomSource);
    }
  }
}

public class TournamentSelector<TSolution> : SelectorBase<TSolution, ObjectiveValue> {
  public TournamentSelector(int tournamentSize, RandomSource randomSource) {
    TournamentSize = tournamentSize;
    RandomSource = randomSource;
  }
  
  public int TournamentSize { get; }
  public RandomSource RandomSource { get; }

  public override TSolution[] Select(TSolution[] population, ObjectiveValue[] objectives, int count) {
    var rng = RandomSource.CreateRandomNumberGenerator();
    var selected = new TSolution[count];
    var populationIndexMap = population
      .Select((solution, index) => (solution, index))
      .ToDictionary(x => x.solution, x => x.index);

    for (int i = 0; i < count; i++) {
      var tournamentParticipants = new List<TSolution>();
      for (int j = 0; j < TournamentSize; j++) {
        int index = rng.Integer(population.Length);
        tournamentParticipants.Add(population[index]);
      }
      var bestParticipant = tournamentParticipants.OrderBy(participant => objectives[populationIndexMap[participant]]).First();
      selected[i] = bestParticipant;
    }
    return selected;
  }
  
  public record Parameters(int TournamentSize, RandomSource RandomSource) : SelectorParameters;

  public class Template : SelectorTemplateBase<TournamentSelector<TSolution>, TSolution, ObjectiveValue, Parameters> {
    public override TournamentSelector<TSolution> Parameterize(Parameters parameters) {
      return new TournamentSelector<TSolution>(parameters.TournamentSize, parameters.RandomSource);
    }
  }
}


