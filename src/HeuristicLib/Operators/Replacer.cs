using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators;

public abstract record class Replacer : Operator<IReplacerInstance> {
}


public interface IReplacerInstance {
  IReadOnlyList<EvaluatedIndividual<TGenotype>> Replace<TGenotype/*, TPhenotype*/>(IReadOnlyList<EvaluatedIndividual<TGenotype>> previousPopulation/*, TPhenotype*/, IReadOnlyList<EvaluatedIndividual<TGenotype>> offspringPopulation/*, TPhenotype*/, Objective objective, IRandomNumberGenerator random);
  int GetOffspringCount(int populationSize);
}

public abstract class ReplacerInstance<TReplacer> : OperatorInstance<TReplacer>, IReplacerInstance
  where TReplacer : Replacer
{
  protected ReplacerInstance(TReplacer parameters) : base(parameters) { }
  
  public abstract IReadOnlyList<EvaluatedIndividual<TGenotype>> Replace<TGenotype/*, TPhenotype*/>(IReadOnlyList<EvaluatedIndividual<TGenotype>> previousPopulation/*, TPhenotype*/, IReadOnlyList<EvaluatedIndividual<TGenotype>> offspringPopulation/*, TPhenotype*/, Objective objective, IRandomNumberGenerator random);
  public abstract int GetOffspringCount(int populationSize);
  
}


//
// public abstract class ReplacerBase : IReplacer {
//   public abstract IReadOnlyList<EvaluatedIndividual<TGenotype>> Replace<TGenotype/*, TPhenotype*/>(IReadOnlyList<EvaluatedIndividual<TGenotype>> previousPopulation/*, TPhenotype*/, IReadOnlyList<EvaluatedIndividual<TGenotype>> offspringPopulation/*, TPhenotype*/, Objective objective, IRandomNumberGenerator random);
//   public abstract int GetOffspringCount(int populationSize);
// }

public record class PlusSelectionReplacer : Replacer {
  public override PlusSelectionReplacerInstance CreateInstance() {
    return new PlusSelectionReplacerInstance(this);
  }
}

public class PlusSelectionReplacerInstance : ReplacerInstance<PlusSelectionReplacer> {
  public PlusSelectionReplacerInstance(PlusSelectionReplacer parameters) : base(parameters) {}
  
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


public record class ElitismReplacer : Replacer {
  public int Elites { get; }

  public ElitismReplacer(int elites) {
    Elites = elites;
  }

  public override ElitismReplacerInstance CreateInstance() {
    return new ElitismReplacerInstance(this);
  }
}

public class ElitismReplacerInstance : ReplacerInstance<ElitismReplacer> {
  // public ElitismReplacer Parameters { get; }

  public ElitismReplacerInstance(ElitismReplacer parameters) : base (parameters) {
    // Parameters = parameters;
  }

  public override IReadOnlyList<EvaluatedIndividual<TGenotype>> Replace<TGenotype/*, TPhenotype*/>(IReadOnlyList<EvaluatedIndividual<TGenotype>> previousPopulation/*, TPhenotype*/, IReadOnlyList<EvaluatedIndividual<TGenotype>> offspringPopulation/*, TPhenotype*/, Objective objective, IRandomNumberGenerator random) {
    var elitesPopulation = previousPopulation
      .OrderBy(p => p.Fitness, objective.TotalOrderComparer)
      .Take(Parameters.Elites);
    
    return elitesPopulation
      .Concat(offspringPopulation) // requires that offspring population size is correct
      .ToArray();
  }
  
  public override int GetOffspringCount(int populationSize) => populationSize - Parameters.Elites;
}
