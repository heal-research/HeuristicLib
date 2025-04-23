using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators;

public interface IReplacer : IOperator {
  IReadOnlyList<EvaluatedIndividual<TGenotype>> Replace<TGenotype/*, TPhenotype*/>(IReadOnlyList<EvaluatedIndividual<TGenotype>> previousPopulation/*, TPhenotype*/, IReadOnlyList<EvaluatedIndividual<TGenotype>> offspringPopulation/*, TPhenotype*/, Objective objective, IRandomNumberGenerator random);
  int GetOffspringCount(int populationSize);
}

public abstract class ReplacerBase : IReplacer {
  public abstract IReadOnlyList<EvaluatedIndividual<TGenotype>> Replace<TGenotype/*, TPhenotype*/>(IReadOnlyList<EvaluatedIndividual<TGenotype>> previousPopulation/*, TPhenotype*/, IReadOnlyList<EvaluatedIndividual<TGenotype>> offspringPopulation/*, TPhenotype*/, Objective objective, IRandomNumberGenerator random);
  public abstract int GetOffspringCount(int populationSize);
}

public class PlusSelectionReplacer : ReplacerBase {
  public override IReadOnlyList<EvaluatedIndividual<TGenotype>> Replace<TGenotype/*, TPhenotype*/>(IReadOnlyList<EvaluatedIndividual<TGenotype>> previousPopulation/*, TPhenotype*/, IReadOnlyList<EvaluatedIndividual<TGenotype>> offspringPopulation/*, TPhenotype*/, Objective objective, IRandomNumberGenerator random) {
    var combinedPopulation = previousPopulation.Concat(offspringPopulation).ToList();
    return combinedPopulation
      .OrderBy(p => p.Fitness, objective.TotalOrderComparer)
      .Take(previousPopulation.Count) // if algorithm population differs from previousPopulation.Length, it is not detected
      .ToArray();
  }
  
  public override int GetOffspringCount(int populationSize) {
    return populationSize;
  }
}

public class ElitismReplacer : ReplacerBase {
  public int Elites { get; }

  public ElitismReplacer(int elites) {
    Elites = elites;
  }

  public override IReadOnlyList<EvaluatedIndividual<TGenotype>> Replace<TGenotype/*, TPhenotype*/>(IReadOnlyList<EvaluatedIndividual<TGenotype>> previousPopulation/*, TPhenotype*/, IReadOnlyList<EvaluatedIndividual<TGenotype>> offspringPopulation/*, TPhenotype*/, Objective objective, IRandomNumberGenerator random) {
    var elitesPopulation = previousPopulation
      .OrderBy(p => p.Fitness, objective.TotalOrderComparer)
      .Take(Elites);
    
    return elitesPopulation
      .Concat(offspringPopulation) // requires that offspring population size is correct
      .ToArray();
  }
  
  public override int GetOffspringCount(int populationSize) => populationSize - Elites;
}
