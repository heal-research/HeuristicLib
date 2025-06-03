using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators;


public abstract record class Selector<TGenotype, TSearchSpace, TProblem> : Operator<TGenotype, TSearchSpace, TProblem, ISelectorExecution<TGenotype>> 
  where TSearchSpace : ISearchSpace<TGenotype>
  where TProblem : IOptimizable<TGenotype, TSearchSpace>
{
  [return: NotNullIfNotNull(nameof(problemAgnosticOperator))]
  public static implicit operator Selector<TGenotype, TSearchSpace, TProblem>?(Selector<TGenotype, TSearchSpace>? problemAgnosticOperator) {
    if (problemAgnosticOperator is null) return null;
    return new ProblemSpecificSelector<TGenotype, TSearchSpace, TProblem>(problemAgnosticOperator);
  }
  
  [return: NotNullIfNotNull(nameof(simpleSelector))]
  public static implicit operator Selector<TGenotype, TSearchSpace, TProblem>?(Selector? simpleSelector) {
    if (simpleSelector is null) return null;
    return new SimpleProblemSpecificSelector<TGenotype, TSearchSpace, TProblem>(simpleSelector);
  }
}

public abstract record class Selector<TGenotype, TSearchSpace> : Operator<TGenotype, TSearchSpace, ISelectorExecution<TGenotype>> 
  where TSearchSpace : ISearchSpace<TGenotype>
{
  [return: NotNullIfNotNull(nameof(simpleSelector))]
  public static implicit operator Selector<TGenotype, TSearchSpace>?(Selector? simpleSelector) {
    if (simpleSelector is null) return null;
    return new SimpleSelector<TGenotype, TSearchSpace>(simpleSelector);
  }
}

public abstract record class Selector : Operator<ISelectorExecution>
{
}

public interface ISelectorExecution<TGenotype> {
  IReadOnlyList<Solution<TGenotype>> Select(IReadOnlyList<Solution<TGenotype>> population, Objective objective, int count, IRandomNumberGenerator random);
}

public interface ISelectorExecution {
  IReadOnlyList<Solution<T>> Select<T>(IReadOnlyList<Solution<T>> population, Objective objective, int count, IRandomNumberGenerator random);
}

public abstract class SelectorExecution<TGenotype, TSearchSpace, TProblem, TSelector> : OperatorExecution<TGenotype, TSearchSpace, TProblem, TSelector>, ISelectorExecution<TGenotype>
  where TSearchSpace : ISearchSpace<TGenotype>
  where TProblem : IOptimizable<TGenotype, TSearchSpace> {
  protected SelectorExecution(TSelector parameters, TSearchSpace searchSpace, TProblem problem) : base(parameters, searchSpace, problem) {}

  public abstract IReadOnlyList<Solution<TGenotype>> Select(IReadOnlyList<Solution<TGenotype>> population, Objective objective, int count, IRandomNumberGenerator random);
}

public abstract class SelectorExecution<TGenotype, TSearchSpace, TSelector> : OperatorExecution<TGenotype, TSearchSpace, TSelector>, ISelectorExecution<TGenotype> 
  where TSearchSpace : ISearchSpace<TGenotype>
{
  protected SelectorExecution(TSelector parameters, TSearchSpace searchSpace) : base(parameters, searchSpace) { }
  public abstract IReadOnlyList<Solution<TGenotype>> Select(IReadOnlyList<Solution<TGenotype>> population, Objective objective, int count, IRandomNumberGenerator random);
}

public abstract class SelectorExecution<TSelector> : OperatorExecution< TSelector>, ISelectorExecution 
{
  protected SelectorExecution(TSelector parameters) : base(parameters) { }

  public abstract IReadOnlyList<Solution<T>> Select<T>(IReadOnlyList<Solution<T>> population, Objective objective, int count, IRandomNumberGenerator random);
}

public sealed record class ProblemSpecificSelector<TGenotype, TSearchSpace, TProblem> : Selector<TGenotype, TSearchSpace, TProblem> 
  where TSearchSpace : ISearchSpace<TGenotype>
  where TProblem : IOptimizable<TGenotype, TSearchSpace>
{
  public Selector<TGenotype, TSearchSpace> ProblemAgnosticSelector { get; }

  public ProblemSpecificSelector(Selector<TGenotype, TSearchSpace> problemAgnosticSelector) {
    ProblemAgnosticSelector = problemAgnosticSelector;
  }

  public override ISelectorExecution<TGenotype> CreateExecution(TSearchSpace searchSpace, TProblem problem) {
    return ProblemAgnosticSelector.CreateExecution(searchSpace);
  }
}

internal sealed record class SimpleProblemSpecificSelector<TGenotype, TSearchSpace, TProblem> : Selector<TGenotype, TSearchSpace, TProblem> 
  where TSearchSpace : ISearchSpace<TGenotype>
  where TProblem : IOptimizable<TGenotype, TSearchSpace>
{
  public Selector SimpleSelector { get; }

  public SimpleProblemSpecificSelector(Selector simpleSelector) {
    SimpleSelector = simpleSelector;
  }

  public override ISelectorExecution<TGenotype> CreateExecution(TSearchSpace searchSpace, TProblem problem) {
    var simpleExecution = SimpleSelector.CreateExecution();
    return new SimpleSelectorExecution<TGenotype>(simpleExecution);
  }
}

internal sealed record class SimpleSelector<TGenotype, TSearchSpace> : Selector<TGenotype, TSearchSpace> 
  where TSearchSpace : ISearchSpace<TGenotype>
{
  public Selector ProblemAgnosticSelector { get; }

  public SimpleSelector(Selector problemAgnosticSelector) {
    ProblemAgnosticSelector = problemAgnosticSelector;
  }

  public override ISelectorExecution<TGenotype> CreateExecution(TSearchSpace searchSpace) {
    var simpleExecution = ProblemAgnosticSelector.CreateExecution();
    return new SimpleSelectorExecution<TGenotype>(simpleExecution);
  }
}


internal class SimpleSelectorExecution<TGenotype> : ISelectorExecution<TGenotype> {
  public ISelectorExecution SimpleSelectionExecution { get; }

  public SimpleSelectorExecution(ISelectorExecution simpleSelectionExecution) {
    SimpleSelectionExecution = simpleSelectionExecution;
  }

  public IReadOnlyList<Solution<TGenotype>> Select(IReadOnlyList<Solution<TGenotype>> population, Objective objective, int count, IRandomNumberGenerator random)
  {
    return SimpleSelectionExecution.Select(population, objective, count, random);
  }
}


//
// public abstract class SelectorBase : ISelector {
//   public abstract IReadOnlyList<EvaluatedIndividual<TGenotype>> Select<TGenotype/*, TPhenotype*/>(IReadOnlyList<EvaluatedIndividual<TGenotype>> population/*, TPhenotype*/, Objective objective, int count, IRandomNumberGenerator random);
// }


// public abstract record class FitnessAssignment {
// }
//
// public abstract record class FitnessAssignment<TGenotype, TSearchSpace> {
// }

public interface IFitnessAssignment {
  double AssignFitness<TGenotype>(Solution<TGenotype> solution, Objective objective, IReadOnlyList<Solution<TGenotype>> population);
}

public interface IFitnessAssignment<TGenotype> {
  double AssignFitness(Solution<TGenotype> solution, Objective objective, IReadOnlyList<Solution<TGenotype>> population);
}


// public abstract record class FitnessBasedSelector<TGenotype, TSearchSpace> : Selector<TGenotype, TSearchSpace>
//   where TSearchSpace : ISearchSpace<TGenotype>
// {
//   public IFitnessAssignment<TGenotype> FitnessAssignment { get; }
//   
//   protected FitnessBasedSelector(IFitnessAssignment<TGenotype> fitnessAssignment) {
//     FitnessAssignment = fitnessAssignment;
//   }
// }

public abstract record class FitnessBasedSelector : Selector
{
  public IFitnessAssignment FitnessAssignment { get; }
  
  protected FitnessBasedSelector(IFitnessAssignment fitnessAssignment) {
    FitnessAssignment = fitnessAssignment;
  }
}

public abstract class FitnessBasedSelectorExecution<TSelector> : SelectorExecution<TSelector>
  where TSelector : FitnessBasedSelector {
  
  protected FitnessBasedSelectorExecution(TSelector parameters) : base(parameters) {
  }

  public sealed override IReadOnlyList<Solution<TGenotype>> Select<TGenotype>(IReadOnlyList<Solution<TGenotype>> population, Objective objective, int count, IRandomNumberGenerator random) {
    var fitnesses = population
      .Select(solution => Parameters.FitnessAssignment.AssignFitness(solution, objective, population))
      .ToList();

    return Select(population, fitnesses, objective, count, random);
  }

  protected abstract IReadOnlyList<Solution<TGenotype>> Select<TGenotype>(IReadOnlyList<Solution<TGenotype>> population, IReadOnlyList<double> fitnesses, Objective objective, int count, IRandomNumberGenerator random);
}






public record class ProportionalSelector : FitnessBasedSelector
{ // ToDo: Probability-based selection base class (fitness -> probability, rank -> probability, etc.)
  public bool Windowing { get; }

  public ProportionalSelector(bool windowing = true) : base(default(IFitnessAssignment)!) { /*ToDo: set assignment only if necessary*/
    Windowing = windowing;
  }

  public override ProportionalSelectorExecution CreateExecution() {
    return new ProportionalSelectorExecution(this);
  }
}

public class ProportionalSelectorExecution : FitnessBasedSelectorExecution<ProportionalSelector>
{
  public ProportionalSelectorExecution(ProportionalSelector parameters) : base(parameters) {}

  protected override IReadOnlyList<Solution<TGenotype>> Select<TGenotype>(IReadOnlyList<Solution<TGenotype>> population, IReadOnlyList<double> fitnesses, Objective objective, int count, IRandomNumberGenerator random) {
    var singleObjective = objective.Directions.Length == 1 ? objective.Directions[0] : throw new InvalidOperationException("Proportional selection requires a single objective.");
    // prepare qualities
    double minQuality = double.MaxValue, maxQuality = double.MinValue;
    foreach (double val in fitnesses) {
      minQuality = Math.Min(minQuality, val);
      maxQuality = Math.Max(maxQuality, val);
    }
    
    var qualities = fitnesses.AsEnumerable();
    if (Math.Abs(minQuality - maxQuality) < double.Epsilon) {
      qualities = qualities.Select(_ => 1.0);
    } else {
      if (Parameters.Windowing) {
        if (singleObjective == ObjectiveDirection.Maximize) {
          qualities = qualities.Select(q => q - minQuality);
        } else {
          qualities = qualities.Select(q => maxQuality - q);
        }
      } else {
        if (minQuality < 0.0)
          throw new InvalidOperationException("Proportional selection without windowing does not work with quality values < 0.");
        if (singleObjective == ObjectiveDirection.Minimize) {
          double limit = Math.Min(maxQuality * 2, double.MaxValue);
          qualities = qualities.Select(q => limit - q);
        }
      }
    }

    var list = qualities.ToList();
    double qualitySum = list.Sum();
    var selected = new Solution<TGenotype/*, TPhenotype*/>[count];
    for (int i = 0; i < count; i++) {
      double selectedQuality = random.Random() * qualitySum;
      int index = 0;
      double currentQuality = list[index];
      while (currentQuality < selectedQuality) {
        index++;
        currentQuality += list[index];
      }
      selected[i] = population[index];
    }
    return selected;
  }
}

public record class RandomSelector : Selector 
{
  public override RandomSelectorExecution CreateExecution() {
    return new RandomSelectorExecution(this);
  }
}

public class RandomSelectorExecution : SelectorExecution<RandomSelector>
{
  public RandomSelectorExecution(RandomSelector parameters) : base(parameters) { }
  public override IReadOnlyList<Solution<TGenotype>> Select<TGenotype>(IReadOnlyList<Solution<TGenotype>> population, Objective objective, int count, IRandomNumberGenerator random) {
    var selected = new Solution<TGenotype>[count];
    for (int i = 0; i < count; i++) {
      int index = random.Integer(population.Count);
      selected[i] = population[index];
    }
    return selected;
  }
}

public record class TournamentSelector<TGenotype, TSearchSpace> : Selector<TGenotype, TSearchSpace> 
  where TSearchSpace : ISearchSpace<TGenotype>
{
  public int TournamentSize { get; }
  
  public TournamentSelector(int tournamentSize) {
    TournamentSize = tournamentSize;
  }
  
  public override TournamentSelectorExecution<TGenotype, TSearchSpace> CreateExecution(TSearchSpace searchSpace) {
    return new TournamentSelectorExecution<TGenotype, TSearchSpace>(this, searchSpace);
  }
}

public class TournamentSelectorExecution<TGenotype, TSearchSpace> : SelectorExecution<TGenotype, TSearchSpace, TournamentSelector<TGenotype, TSearchSpace>>
  where TSearchSpace : ISearchSpace<TGenotype>
{
  public TournamentSelectorExecution(TournamentSelector<TGenotype, TSearchSpace> parameters, TSearchSpace searchSpace) : base(parameters, searchSpace) { }
  public override IReadOnlyList<Solution<TGenotype>> Select(IReadOnlyList<Solution<TGenotype>> population, Objective objective, int count, IRandomNumberGenerator random) {
    var selected = new Solution<TGenotype/*, TPhenotype*/>[count];
    for (int i = 0; i < count; i++) {
      var tournamentParticipants = new List<Solution<TGenotype/*, TPhenotype*/>>();
      for (int j = 0; j < Parameters.TournamentSize; j++) {
        int index = random.Integer(population.Count);
        tournamentParticipants.Add(population[index]);
      }
      var bestParticipant = tournamentParticipants.OrderBy(participant => participant.ObjectiveVector, objective.TotalOrderComparer).First();
      selected[i] = bestParticipant;
    }
    return selected;
  }
}
