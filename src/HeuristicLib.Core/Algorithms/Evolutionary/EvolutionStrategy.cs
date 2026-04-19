using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Mutators.RealVectorMutators;
using HEAL.HeuristicLib.Operators.Replacers;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms.Evolutionary;

public enum EvolutionStrategyType
{
  Comma,
  Plus
}

public record EvolutionStrategyState<TGenotype> : PopulationState<TGenotype>
{
  public required double MutationStrength { get; init; }
}

public record EvolutionStrategy<TGenotype, TSearchSpace, TProblem>
  : IterativeAlgorithm<TGenotype, TSearchSpace, TProblem, EvolutionStrategyState<TGenotype>>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  public required int PopulationSize { get; init; }
  public required int NumberOfChildren { get; init; }
  public required EvolutionStrategyType Strategy { get; init; }
  public required ICreator<TGenotype, TSearchSpace, TProblem> Creator { get; init; }
  public required IMutator<TGenotype, TSearchSpace, TProblem> Mutator { get; init; }
  public required ICrossover<TGenotype, TSearchSpace, TProblem>? Crossover { get; init; }
  public double InitialMutationStrength { get; init; } = 1.0;
  public required ISelector<TGenotype, TSearchSpace, TProblem> Selector { get; init; }

  protected override EvolutionStrategyState<TGenotype> ExecuteStep(
    EvolutionStrategyState<TGenotype>? previousState,
    IOperatorExecutor executor,
    TProblem problem,
    IRandomNumberGenerator random)
  {
    if (previousState is null) {
      var initialPopulation = executor.Create(Creator, PopulationSize, random, problem.SearchSpace, problem);
      var objectives = executor.Evaluate(Evaluator, initialPopulation, random, problem.SearchSpace, problem);
      return new EvolutionStrategyState<TGenotype> {
        Population = Population.From(initialPopulation, objectives),
        MutationStrength = InitialMutationStrength
      };
    }

    IReadOnlyList<TGenotype> parents;
    IReadOnlyList<ObjectiveVector> parentQualities;

    if (Crossover is null) {
      var parentSolutions = executor.Select(
        Selector,
        previousState.Population.Solutions,
        problem.Objective,
        NumberOfChildren,
        random,
        problem.SearchSpace,
        problem);
      parents = parentSolutions.Select(x => x.Genotype).ToArray();
      parentQualities = parentSolutions.Select(x => x.ObjectiveVector).ToArray();
    } else {
      var parentSolutions = executor.Select(
        Selector,
        previousState.Population.Solutions,
        problem.Objective,
        NumberOfChildren * 2,
        random,
        problem.SearchSpace,
        problem);
      parents = executor.Cross(Crossover, parentSolutions.ToParents(problem.Objective), random, problem.SearchSpace, problem);
      parentQualities = parentSolutions.Where((_, i) => i % 2 == 0).Select(x => x.ObjectiveVector).ToArray();
    }

    var children = executor.Mutate(Mutator, parents, random, problem.SearchSpace, problem);
    var fitnesses = executor.Evaluate(Evaluator, children, random, problem.SearchSpace, problem);

    var newMutationStrength = previousState.MutationStrength;
    if (Mutator is IVariableStrengthMutator<TGenotype, TSearchSpace, TProblem> variableStrengthMutator) {
      var successes = parentQualities.Zip(fitnesses)
        .Count(t => t.Second.CompareTo(t.First, problem.Objective) == DominanceRelation.Dominates);
      var successRate = successes / (double)PopulationSize;
      newMutationStrength *= successRate switch {
        > 0.2 => 1.5,
        < 0.2 => 1 / 1.5,
        _ => 1
      };
      variableStrengthMutator.MutationStrength = newMutationStrength;
    }

    var population = Population.From(children, fitnesses);
    var newPopulation = Strategy switch {
      EvolutionStrategyType.Comma => ElitismReplacer<TGenotype>.Replace(previousState.Population.Solutions, population.Solutions, problem.Objective, NumberOfChildren, 0),
      EvolutionStrategyType.Plus => PlusSelectionReplacer<TGenotype>.Replace(previousState.Population.Solutions, population.Solutions, problem.Objective, NumberOfChildren),
      _ => throw new InvalidOperationException($"Unknown strategy {Strategy}")
    };

    return new EvolutionStrategyState<TGenotype> {
      Population = Population.From(newPopulation),
      MutationStrength = newMutationStrength
    };
  }
}

public static class EvolutionStrategy
{
  public static EvolutionStrategyBuilder<TGenotype, TSearchSpace, TProblem> GetBuilder<TGenotype, TSearchSpace, TProblem>(
    ICreator<TGenotype, TSearchSpace, TProblem> creator,
    IMutator<TGenotype, TSearchSpace, TProblem> mutator)
    where TSearchSpace : class, ISearchSpace<TGenotype> where TProblem : class, IProblem<TGenotype, TSearchSpace>
  {
    return new() {
      Mutator = mutator,
      InitialMutationStrength = (mutator as IVariableStrengthMutator<TGenotype, TSearchSpace, TProblem>)?.MutationStrength ?? 0,
      Creator = creator
    };
  }
}
