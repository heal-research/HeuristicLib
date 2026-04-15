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

public record EvolutionStrategy<TGenotype, TSearchSpace, TProblem>
  : IterativeAlgorithm<TGenotype, TSearchSpace, TProblem, PopulationState<TGenotype>, EvolutionStrategy<TGenotype, TSearchSpace, TProblem>.State>
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

  protected override State CreateInitialState(ExecutionInstanceRegistry registry) => new(registry, this);

  protected override PopulationState<TGenotype> ExecuteStep(State state, PopulationState<TGenotype>? previousState, TProblem problem, IRandomNumberGenerator random)
  {
    if (previousState is null) {
      var initialPopulation = state.Creator.Create(PopulationSize, random, problem.SearchSpace, problem);
      var objectives = state.Evaluator.Evaluate(initialPopulation, random, problem.SearchSpace, problem);
      return new PopulationState<TGenotype> { Population = Population.From(initialPopulation, objectives) };
    }

    IReadOnlyList<TGenotype> parents;
    IReadOnlyList<ObjectiveVector> parentQualities;

    if (state.Crossover == null) {
      var parentSolutions = state.Selector.Select(previousState.Population.Solutions, problem.Objective, NumberOfChildren, random, problem.SearchSpace, problem);
      parents = parentSolutions.Select(x => x.Genotype).ToArray();
      parentQualities = parentSolutions.Select(x => x.ObjectiveVector).ToArray();
    } else {
      var parentSolutions = state.Selector.Select(previousState.Population.Solutions, problem.Objective, NumberOfChildren * 2, random, problem.SearchSpace, problem);
      parents = state.Crossover.Cross(parentSolutions.ToParents(problem.Objective), random, problem.SearchSpace, problem);
      parentQualities = parentSolutions.Where((_, i) => i % 2 == 0).Select(x => x.ObjectiveVector).ToArray();
    }

    var children = state.Mutator.Mutate(parents, random, problem.SearchSpace, problem);
    var fitnesses = state.Evaluator.Evaluate(children, random, problem.SearchSpace, problem);

    var newMutationStrength = state.MutationStrength;
    if (Mutator is IVariableStrengthMutator<TGenotype, TSearchSpace, TProblem> vm) {
      // adapt Mutation Strength based on 1/5th rule
      var successes = parentQualities.Zip(fitnesses).Count(t => t.Second.CompareTo(t.First, problem.Objective) == DominanceRelation.Dominates);
      var successRate = successes / (double)PopulationSize;
      newMutationStrength *= successRate switch {
        > 0.2 => 1.5,
        < 0.2 => 1 / 1.5,
        _ => 1
      };
      state.MutationStrength = newMutationStrength;
    }

    var population = Population.From(children, fitnesses);

    var newPopulation = state.Replacer.Replace(previousState.Population.Solutions, population.Solutions, problem.Objective, NumberOfChildren, random, problem.SearchSpace, problem);
    return new PopulationState<TGenotype> { Population = Population.From(newPopulation) };
  }

  public class State : IterativeAlgorithmState
  {
    public readonly ICreatorInstance<TGenotype, TSearchSpace, TProblem> Creator;
    public readonly IMutatorInstance<TGenotype, TSearchSpace, TProblem> Mutator;
    public readonly ICrossoverInstance<TGenotype, TSearchSpace, TProblem>? Crossover;
    public readonly ISelectorInstance<TGenotype, TSearchSpace, TProblem> Selector;
    public readonly IReplacerInstance<TGenotype, TSearchSpace, TProblem> Replacer;
    public double MutationStrength { get; set; }

    public State(ExecutionInstanceRegistry instanceRegistry, EvolutionStrategy<TGenotype, TSearchSpace, TProblem> algorithm) : base(instanceRegistry, algorithm)
    {
      Creator = instanceRegistry.Resolve(algorithm.Creator);
      Mutator = instanceRegistry.Resolve(algorithm.Mutator);
      Crossover = instanceRegistry.ResolveOptional(algorithm.Crossover);
      Selector = instanceRegistry.Resolve(algorithm.Selector);
      IReplacer<TGenotype, TSearchSpace, TProblem> replacer = algorithm.Strategy switch {
        EvolutionStrategyType.Comma => new ElitismReplacer<TGenotype>(0),
        EvolutionStrategyType.Plus => new PlusSelectionReplacer<TGenotype>(),
        _ => throw new InvalidOperationException($"Unknown strategy {algorithm.Strategy}")
      };
      Replacer = instanceRegistry.Resolve(replacer);
      MutationStrength = algorithm.InitialMutationStrength;
    }
  }
}

public static class EvolutionStrategy
{
  public static EvolutionStrategyBuilder<TGenotype, TSearchSpace, TProblem> GetBuilder<TGenotype, TSearchSpace, TProblem>(
    ICreator<TGenotype, TSearchSpace, TProblem> creator,
    IMutator<TGenotype, TSearchSpace, TProblem> mutator)
    where TSearchSpace : class, ISearchSpace<TGenotype> where TProblem : class, IProblem<TGenotype, TSearchSpace>
  {
    return new EvolutionStrategyBuilder<TGenotype, TSearchSpace, TProblem> {
      Mutator = mutator,
      InitialMutationStrength = (mutator as IVariableStrengthMutator<TGenotype, TSearchSpace, TProblem>)?.MutationStrength ?? 0,
      Creator = creator
    };
  }
}
