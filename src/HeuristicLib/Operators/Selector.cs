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
  public ProportionalSelector(IRandomNumberGenerator random) {
    Random = random;
  }
  
  public IRandomNumberGenerator Random { get; }

  public TSolution[] Select(TSolution[] population, ObjectiveValue[] objectives, int count) {
    var selected = new TSolution[count];
    for (int j = 0; j < count; j++) {
      double totalQuality = objectives.Sum(o => o.Value);
      double randomValue = Random.Random() * totalQuality;
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
}

public record ProportionalSelectorParameters(IRandomNumberGenerator Random) : SelectorParameters;

public class ProportionalSelectorTemplate<TSolution> 
  : SelectorTemplateBase<ProportionalSelector<TSolution>, TSolution, ObjectiveValue, ProportionalSelectorParameters> {
  public override ProportionalSelector<TSolution> Parameterize(ProportionalSelectorParameters parameters) {
    return new ProportionalSelector<TSolution>(parameters.Random);
  }
}


public class RandomSelector<TSolution, TObjective> : SelectorBase<TSolution, TObjective> {
  public RandomSelector(IRandomNumberGenerator random) {
    Random = random;
  }
  
  public IRandomNumberGenerator Random { get; }

  public override TSolution[] Select(TSolution[] population, TObjective[] objectives, int count) {
    var selected = new TSolution[count];
    for (int i = 0; i < count; i++) {
      int index = Random.Integer(population.Length);
      selected[i] = population[index];
    }
    return selected;
  }
}

public record RandomSelectorParameters(IRandomNumberGenerator Random) : SelectorParameters;

public class RandomSelectorTemplate<TSolution, TObjective> 
  : SelectorTemplateBase<RandomSelector<TSolution, TObjective>, TSolution, TObjective, RandomSelectorParameters> {
  public override RandomSelector<TSolution, TObjective> Parameterize(RandomSelectorParameters parameters) {
    return new RandomSelector<TSolution, TObjective>(parameters.Random);
  }
}


public class TournamentSelector<TSolution> : SelectorBase<TSolution, ObjectiveValue> {
  public TournamentSelector(int tournamentSize, IRandomNumberGenerator random) {
    TournamentSize = tournamentSize;
    Random = random;
  }
  
  public int TournamentSize { get; }
  public IRandomNumberGenerator Random { get; }

  public override TSolution[] Select(TSolution[] population, ObjectiveValue[] objectives, int count) {
    var selected = new TSolution[count];
    var populationIndexMap = population
      .Select((solution, index) => (solution, index))
      .ToDictionary(x => x.solution, x => x.index);

    for (int i = 0; i < count; i++) {
      var tournamentParticipants = new List<TSolution>();
      for (int j = 0; j < TournamentSize; j++) {
        int index = Random.Integer(population.Length);
        tournamentParticipants.Add(population[index]);
      }
      var bestParticipant = tournamentParticipants.OrderBy(participant => objectives[populationIndexMap[participant]]).First();
      selected[i] = bestParticipant;
    }
    return selected;
  }
}

public record TournamentSelectorParameters(int TournamentSize, IRandomNumberGenerator Random) : SelectorParameters;

public class TournamentSelectorTemplate<TSolution> 
  : SelectorTemplateBase<TournamentSelector<TSolution>, TSolution, ObjectiveValue, TournamentSelectorParameters> {
  public override TournamentSelector<TSolution> Parameterize(TournamentSelectorParameters parameters) {
    return new TournamentSelector<TSolution>(parameters.TournamentSize, parameters.Random);
  }
}
