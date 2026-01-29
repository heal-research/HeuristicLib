using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Operators.Crossovers;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Operators.Mutators.RealVectorMutators;
using HEAL.HeuristicLib.Operators.Replacers;
using HEAL.HeuristicLib.Operators.Selectors;
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

public class EvolutionStrategy<TGenotype, TSearchSpace, TProblem>
  : IterativeAlgorithm<TGenotype, TSearchSpace, TProblem, EvolutionStrategyState<TGenotype>>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TGenotype : class
{
  public required int PopulationSize { get; init; }
  public required int NumberOfChildren { get; init; }
  public required EvolutionStrategyType Strategy { get; init; }
  public required ICreator<TGenotype, TSearchSpace, TProblem> Creator { get; init; }
  public required IMutator<TGenotype, TSearchSpace, TProblem> Mutator { get; init; }
  public required ICrossover<TGenotype, TSearchSpace, TProblem>? Crossover { get; init; }
  public double InitialMutationStrength { get; init; } = 1.0;
  public required ISelector<TGenotype, TSearchSpace, TProblem> Selector { get; init; }

  public override EvolutionStrategyState<TGenotype> ExecuteStep(TProblem problem, EvolutionStrategyState<TGenotype>? previousState, IRandomNumberGenerator random)
  {
    if (previousState == null) {
      var initialPopulation = Creator.Create(PopulationSize, random, problem.SearchSpace, problem);
      var objectives = Evaluator.Evaluate(initialPopulation, random, problem.SearchSpace, problem);
      return new EvolutionStrategyState<TGenotype> {
        Population = Population.From(initialPopulation, objectives),
        //CurrentIteration = 0,
        // ToDo: Actually, mutation strength is not used in initial population creation.
        MutationStrength = InitialMutationStrength
      };
    }

    IReadOnlyList<TGenotype> parents;
    IReadOnlyList<ObjectiveVector> parentQualities;

    if (Crossover == null) {
      var parentSolutions = Selector.Select(previousState.Population.Solutions, problem.Objective, NumberOfChildren, random, problem.SearchSpace, problem);
      parents = parentSolutions.Select(x => x.Genotype).ToArray();
      parentQualities = parentSolutions.Select(x => x.ObjectiveVector).ToArray();
    } else {
      var parentSolutions = Selector.Select(previousState.Population.Solutions, problem.Objective, NumberOfChildren * 2, random, problem.SearchSpace, problem);
      parents = Crossover!.Cross(parentSolutions.ToGenotypePairs(), random, problem.SearchSpace, problem);
      parentQualities = parentSolutions.Where((_, i) => i % 2 == 0).Select(x => x.ObjectiveVector).ToArray();
    }

    var children = Mutator.Mutate(parents, random, problem.SearchSpace, problem);
    var fitnesses = Evaluator.Evaluate(children, random, problem.SearchSpace, problem);

    var newMutationStrength = previousState.MutationStrength;
    if (Mutator is IVariableStrengthMutator<TGenotype, TSearchSpace, TProblem> vm) {
      //adapt Mutation Strength based on 1/5th rule
      var successes = parentQualities.Zip(fitnesses).Count(t => t.Item2.CompareTo(t.Item1, problem.Objective) == DominanceRelation.Dominates);
      var successRate = successes / (double)PopulationSize;
      newMutationStrength *= successRate switch {
        > 0.2 => 1.5,
        < 0.2 => 1 / 1.5,
        _ => 1
      };
      vm.MutationStrength = newMutationStrength;
    }

    var population = Population.From(children, fitnesses);
    Replacer<TGenotype> replacer = Strategy switch {
      EvolutionStrategyType.Comma => new ElitismReplacer<TGenotype>(0),
      EvolutionStrategyType.Plus => new PlusSelectionReplacer<TGenotype>(),
      _ => throw new InvalidOperationException($"Unknown strategy {Strategy}")
    };
    var newPopulation = replacer.Replace(previousState.Population.Solutions, population.Solutions, problem.Objective, random);
    return new EvolutionStrategyState<TGenotype> {
      Population = Population.From(newPopulation),
      //CurrentIteration = previousState.CurrentIteration + 1,
      MutationStrength = newMutationStrength
    };
  }
}

public static class EvolutionStrategy
{
  public static EvolutionStrategyBuilder<TGenotype, TSearchSpace, TProblem> GetBuilder<TGenotype, TSearchSpace, TProblem>(
    ICreator<TGenotype, TSearchSpace, TProblem> creator,
    IMutator<TGenotype, TSearchSpace, TProblem> mutator)
    where TSearchSpace : class, ISearchSpace<TGenotype> where TProblem : class, IProblem<TGenotype, TSearchSpace> where TGenotype : class => new() {
    Mutator = mutator,
    InitialMutationStrength = (mutator as IVariableStrengthMutator<TGenotype, TSearchSpace, TProblem>)?.MutationStrength ?? 0,
    Creator = creator
  };
}
