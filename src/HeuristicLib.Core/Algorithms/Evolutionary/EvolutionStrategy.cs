using HEAL.HeuristicLib.Execution;
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
  : IterativeAlgorithm<TGenotype, TSearchSpace, TProblem, EvolutionStrategyState<TGenotype>, EvolutionStrategy<TGenotype, TSearchSpace, TProblem>.ExecutionState>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  public new sealed class ExecutionState
    : IterativeAlgorithm<TGenotype, TSearchSpace, TProblem, EvolutionStrategyState<TGenotype>, ExecutionState>.ExecutionState
  {
    public required ICreatorInstance<TGenotype, TSearchSpace, TProblem> Creator { get; init; }
    public required IMutatorInstance<TGenotype, TSearchSpace, TProblem> Mutator { get; init; }
    public required ISelectorInstance<TGenotype, TSearchSpace, TProblem> Selector { get; init; }
    public ICrossoverInstance<TGenotype, TSearchSpace, TProblem>? Crossover { get; init; }
    public IVariableStrengthMutator<TGenotype, TSearchSpace, TProblem>? VariableStrengthMutator { get; init; }
  }

  public required int PopulationSize { get; init; }
  public required int NumberOfChildren { get; init; }
  public required EvolutionStrategyType Strategy { get; init; }
  public required ICreator<TGenotype, TSearchSpace, TProblem> Creator { get; init; }
  public required IMutator<TGenotype, TSearchSpace, TProblem> Mutator { get; init; }
  public required ICrossover<TGenotype, TSearchSpace, TProblem>? Crossover { get; init; }
  public double InitialMutationStrength { get; init; } = 1.0;
  public required ISelector<TGenotype, TSearchSpace, TProblem> Selector { get; init; }

  protected override ExecutionState CreateInitialExecutionState(IExecutionInstanceResolver resolver)
  {
    return new ExecutionState {
      Evaluator = resolver.Resolve(Evaluator),
      Interceptor = Interceptor is not null ? resolver.Resolve(Interceptor) : null,
      Creator = resolver.Resolve(Creator),
      Mutator = resolver.Resolve(Mutator),
      Selector = resolver.Resolve(Selector),
      Crossover = Crossover is not null ? resolver.Resolve(Crossover) : null,
      VariableStrengthMutator = Mutator as IVariableStrengthMutator<TGenotype, TSearchSpace, TProblem>
    };
  }

  protected override EvolutionStrategyState<TGenotype> ExecuteStep(
    EvolutionStrategyState<TGenotype>? previousState,
    ExecutionState executionState,
    TProblem problem,
    IRandomNumberGenerator random)
  {
    if (previousState is null) {
      var initialPopulation = executionState.Creator.Create(PopulationSize, random, problem.SearchSpace, problem);
      var objectives = executionState.Evaluator.Evaluate(initialPopulation, random, problem.SearchSpace, problem);
      return new EvolutionStrategyState<TGenotype> {
        Population = Population.From(initialPopulation, objectives),
        MutationStrength = InitialMutationStrength
      };
    }

    IReadOnlyList<TGenotype> parents;
    IReadOnlyList<ObjectiveVector> parentQualities;

    if (executionState.Crossover is null) {
      var parentSolutions = executionState.Selector.Select(
        previousState.Population.Solutions,
        problem.Objective,
        NumberOfChildren,
        random,
        problem.SearchSpace,
        problem);
      parents = parentSolutions.Select(x => x.Genotype).ToArray();
      parentQualities = parentSolutions.Select(x => x.ObjectiveVector).ToArray();
    } else {
      var parentSolutions = executionState.Selector.Select(
        previousState.Population.Solutions,
        problem.Objective,
        NumberOfChildren * 2,
        random,
        problem.SearchSpace,
        problem);
      parents = executionState.Crossover.Cross(parentSolutions.ToParents(problem.Objective), random, problem.SearchSpace, problem);
      parentQualities = parentSolutions.Where((_, i) => i % 2 == 0).Select(x => x.ObjectiveVector).ToArray();
    }

    var children = executionState.Mutator.Mutate(parents, random, problem.SearchSpace, problem);
    var fitnesses = executionState.Evaluator.Evaluate(children, random, problem.SearchSpace, problem);

    var newMutationStrength = previousState.MutationStrength;
    if (executionState.VariableStrengthMutator is not null) {
      var successes = parentQualities.Zip(fitnesses)
        .Count(t => t.Second.CompareTo(t.First, problem.Objective) == DominanceRelation.Dominates);
      var successRate = successes / (double)PopulationSize;
      newMutationStrength *= successRate switch {
        > 0.2 => 1.5,
        < 0.2 => 1 / 1.5,
        _ => 1
      };
      executionState.VariableStrengthMutator.MutationStrength = newMutationStrength;
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
