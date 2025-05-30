using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators;

public abstract record class Replacer<TGenotype, TSearchSpace, TProblem> : Operator<TGenotype, TSearchSpace, TProblem, IReplacerInstance<TGenotype>> 
  where TSearchSpace : ISearchSpace<TGenotype>
  where TProblem : IOptimizable<TGenotype, TSearchSpace>
{
  [return: NotNullIfNotNull(nameof(problemAgnosticOperator))]
  public static implicit operator Replacer<TGenotype, TSearchSpace, TProblem>?(Replacer<TGenotype, TSearchSpace>? problemAgnosticOperator) {
    if (problemAgnosticOperator is null) return null;
    return new ProblemSpecificReplacer<TGenotype, TSearchSpace, TProblem>(problemAgnosticOperator);
  }
}

public abstract record class Replacer<TGenotype, TSearchSpace> : Operator<TGenotype, TSearchSpace, IReplacerInstance<TGenotype>> 
  where TSearchSpace : ISearchSpace<TGenotype>
{
}


public interface IReplacerInstance<TGenotype> {
  IReadOnlyList<Solution<TGenotype>> Replace(IReadOnlyList<Solution<TGenotype>> previousPopulation, IReadOnlyList<Solution<TGenotype>> offspringPopulation, Objective objective, IRandomNumberGenerator random);
  int GetOffspringCount(int populationSize);
}

public abstract class ReplacerExecution<TGenotype, TSearchSpace, TProblem, TReplacer> : OperatorExecution<TGenotype, TSearchSpace, TProblem, TReplacer>, IReplacerInstance<TGenotype>
  where TSearchSpace : ISearchSpace<TGenotype>
  where TProblem : IOptimizable<TGenotype, TSearchSpace>
{
  protected ReplacerExecution(TReplacer parameters, TSearchSpace searchSpace, TProblem problem) : base(parameters, searchSpace, problem) { }
  
  public abstract IReadOnlyList<Solution<TGenotype>> Replace(IReadOnlyList<Solution<TGenotype>> previousPopulation, IReadOnlyList<Solution<TGenotype>> offspringPopulation, Objective objective, IRandomNumberGenerator random);
  public abstract int GetOffspringCount(int populationSize);
}

public abstract class ReplacerExecution<TGenotype, TSearchSpace, TReplacer> : OperatorExecution<TGenotype, TSearchSpace, TReplacer>, IReplacerInstance<TGenotype>
  where TSearchSpace : ISearchSpace<TGenotype>
{
  protected ReplacerExecution(TReplacer parameters, TSearchSpace searchSpace) : base(parameters, searchSpace) { }
  
  public abstract IReadOnlyList<Solution<TGenotype>> Replace(IReadOnlyList<Solution<TGenotype>> previousPopulation, IReadOnlyList<Solution<TGenotype>> offspringPopulation, Objective objective, IRandomNumberGenerator random);
  public abstract int GetOffspringCount(int populationSize);
}

public sealed record class ProblemSpecificReplacer<TGenotype, TSearchSpace, TProblem> : Replacer<TGenotype, TSearchSpace, TProblem>
  where TSearchSpace : ISearchSpace<TGenotype>
  where TProblem : IOptimizable<TGenotype, TSearchSpace>
{
  public Replacer<TGenotype, TSearchSpace> ProblemAgnosticReplacer { get; }

  public ProblemSpecificReplacer(Replacer<TGenotype, TSearchSpace> problemAgnosticReplacer) {
    ProblemAgnosticReplacer = problemAgnosticReplacer;
  }

  public override IReplacerInstance<TGenotype> CreateExecution(TSearchSpace searchSpace, TProblem problem) {
    return ProblemAgnosticReplacer.CreateExecution(searchSpace);
  }
}


//
// public abstract class ReplacerBase : IReplacer {
//   public abstract IReadOnlyList<EvaluatedIndividual<TGenotype>> Replace<TGenotype/*, TPhenotype*/>(IReadOnlyList<EvaluatedIndividual<TGenotype>> previousPopulation/*, TPhenotype*/, IReadOnlyList<EvaluatedIndividual<TGenotype>> offspringPopulation/*, TPhenotype*/, Objective objective, IRandomNumberGenerator random);
//   public abstract int GetOffspringCount(int populationSize);
// }

public record class PlusSelectionReplacer<TGenotype, TSearchSpace> : Replacer<TGenotype, TSearchSpace>
  where TSearchSpace : ISearchSpace<TGenotype>

{
  public override PlusSelectionReplacerExecution<TGenotype, TSearchSpace> CreateExecution(TSearchSpace searchSpace) {
    return new PlusSelectionReplacerExecution<TGenotype, TSearchSpace>(this, searchSpace);
  }
}

public class PlusSelectionReplacerExecution<TGenotype, TSearchSpace> : ReplacerExecution<TGenotype, TSearchSpace, PlusSelectionReplacer<TGenotype, TSearchSpace>> 
  where TSearchSpace : ISearchSpace<TGenotype>
{
  public PlusSelectionReplacerExecution(PlusSelectionReplacer<TGenotype, TSearchSpace> parameters, TSearchSpace searchSpace) : base(parameters, searchSpace) {}
  
  public override IReadOnlyList<Solution<TGenotype>> Replace(IReadOnlyList<Solution<TGenotype>> previousPopulation, IReadOnlyList<Solution<TGenotype>> offspringPopulation, Objective objective, IRandomNumberGenerator random) {
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


public record class ElitismReplacer<TGenotype, TSearchSpace> : Replacer<TGenotype, TSearchSpace>
  where TSearchSpace : ISearchSpace<TGenotype>
{
  public int Elites { get; }

  public ElitismReplacer(int elites) {
    Elites = elites;
  }

  public override ElitismReplacerExecution<TGenotype, TSearchSpace> CreateExecution(TSearchSpace searchSpace) {
    return new ElitismReplacerExecution<TGenotype, TSearchSpace>(this, searchSpace);
  }
}

public class ElitismReplacerExecution<TGenotype, TSearchSpace> : ReplacerExecution<TGenotype, TSearchSpace, ElitismReplacer<TGenotype, TSearchSpace>>
  where TSearchSpace : ISearchSpace<TGenotype>
{
  // public ElitismReplacer Parameters { get; }

  public ElitismReplacerExecution(ElitismReplacer<TGenotype, TSearchSpace> parameters, TSearchSpace searchSpace) : base (parameters, searchSpace) {
    // Parameters = parameters;
  }

  public override IReadOnlyList<Solution<TGenotype>> Replace(IReadOnlyList<Solution<TGenotype>> previousPopulation, IReadOnlyList<Solution<TGenotype>> offspringPopulation, Objective objective, IRandomNumberGenerator random) {
    var elitesPopulation = previousPopulation
      .OrderBy(p => p.Fitness, objective.TotalOrderComparer)
      .Take(Parameters.Elites);
    
    return elitesPopulation
      .Concat(offspringPopulation) // requires that offspring population size is correct
      .ToArray();
  }
  
  public override int GetOffspringCount(int populationSize) => populationSize - Parameters.Elites;
}
