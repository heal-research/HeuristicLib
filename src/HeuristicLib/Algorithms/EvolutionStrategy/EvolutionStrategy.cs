using HEAL.HeuristicLib.Operators.Creator;
using HEAL.HeuristicLib.Operators.Crossover;
using HEAL.HeuristicLib.Operators.Mutator;
using HEAL.HeuristicLib.Operators.Mutator.RealVectors;
using HEAL.HeuristicLib.Operators.Prototypes;
using HEAL.HeuristicLib.Operators.Replacer;
using HEAL.HeuristicLib.Operators.Selector;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Algorithms.EvolutionStrategy;

public record EvolutionStrategyIterationResult<TGenotype>(Population<TGenotype> Population, double MutationStrength) : PopulationIterationResult<TGenotype>(Population) {
  public double MutationStrength { get; } = MutationStrength;
}

public static class EvolutionStrategy {
  public static EvolutionStrategy<TGenotype, TSearchSpace, TProblem>.Builder GetBuilder<TGenotype, TSearchSpace, TProblem>(
    ICreator<TGenotype, TSearchSpace, TProblem> creator,
    IMutator<TGenotype, TSearchSpace, TProblem> mutator)
    where TSearchSpace : class, ISearchSpace<TGenotype> where TProblem : class, IProblem<TGenotype, TSearchSpace> where TGenotype : class => new() {
    Mutator = mutator,
    InitialMutationStrength = (mutator as IVariableStrengthMutator<TGenotype, TSearchSpace, TProblem>)?.MutationStrength ?? 0,
    Creator = creator
  };
}

public class EvolutionStrategy<TGenotype, TSearchSpace, TProblem>
  : IterativeAlgorithm<TGenotype, TSearchSpace, TProblem, EvolutionStrategyIterationResult<TGenotype>>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TGenotype : class {
  public required int PopulationSize { get; init; }
  public required int NumberOfChildren { get; init; }
  public required EvolutionStrategyType Strategy { get; init; }
  public required IMutator<TGenotype, TSearchSpace, TProblem> Mutator { get; init; }
  public required ICrossover<TGenotype, TSearchSpace, TProblem>? Crossover { get; init; }
  public double InitialMutationStrength { get; init; } = 1.0;
  public required ISelector<TGenotype, TSearchSpace, TProblem> Selector { get; init; }

  public override EvolutionStrategyIterationResult<TGenotype> ExecuteStep(TProblem problem, TSearchSpace searchSpace, EvolutionStrategyIterationResult<TGenotype>? previousIterationResult, IRandomNumberGenerator random) {
    if (previousIterationResult == null) {
      return new EvolutionStrategyIterationResult<TGenotype>(
        CreateInitialPopulation(problem, searchSpace, random, PopulationSize), InitialMutationStrength);
    }

    IReadOnlyList<TGenotype> parents;
    IReadOnlyList<ObjectiveVector> parentQualities;

    if (Crossover == null) {
      var parentISolutions = Selector.Select(previousIterationResult.Population.Solutions, problem.Objective, NumberOfChildren, random, problem.SearchSpace, problem);
      parents = parentISolutions.Select(x => x.Genotype).ToArray();
      parentQualities = parentISolutions.Select(x => x.ObjectiveVector).ToArray();
    } else {
      var parentISolutions = Selector.Select(previousIterationResult.Population.Solutions, problem.Objective, NumberOfChildren * 2, random, problem.SearchSpace, problem);
      parents = Crossover!.Cross(parentISolutions.ToGenotypePairs(), random, searchSpace, problem);
      parentQualities = parentISolutions.Where((_, i) => i % 2 == 0).Select(x => x.ObjectiveVector).ToArray();
    }

    var children = Mutator.Mutate(parents, random, searchSpace, problem);
    var fitnesses = Evaluator.Evaluate(children, random, searchSpace, problem);

    double newMutationStrength = previousIterationResult.MutationStrength;
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
    var newPopulation = replacer.Replace(previousIterationResult.Population.Solutions, population.Solutions, problem.Objective, random);
    return new EvolutionStrategyIterationResult<TGenotype>(Population.From(newPopulation), newMutationStrength);
  }

  public class Builder : PopulationBasedAlgorithmBuilder<
                           TGenotype, TSearchSpace, TProblem, EvolutionStrategyIterationResult<TGenotype>,
                           EvolutionStrategy<TGenotype, TSearchSpace, TProblem>>,
                         IMutatorPrototype<TGenotype, TSearchSpace, TProblem>,
                         IOptionalCrossoverPrototype<TGenotype, TSearchSpace, TProblem> {
    public int NoChildren { get; set; } = 100;
    public EvolutionStrategyType Strategy { get; set; } = EvolutionStrategyType.Plus;
    public required IMutator<TGenotype, TSearchSpace, TProblem> Mutator { get; set; }
    public required double InitialMutationStrength { get; set; }
    public ICrossover<TGenotype, TSearchSpace, TProblem>? Crossover { get; set; }

    public override EvolutionStrategy<TGenotype, TSearchSpace, TProblem> BuildAlgorithm() {
      return new EvolutionStrategy<TGenotype, TSearchSpace, TProblem> {
        PopulationSize = PopulationSize,
        Strategy = Strategy,
        Creator = Creator,
        Mutator = Mutator,
        Crossover = Crossover,
        Selector = Selector,
        Evaluator = Evaluator,
        AlgorithmRandom = SystemRandomNumberGenerator.Default(RandomSeed),
        Terminator = Terminator,
        InitialMutationStrength = InitialMutationStrength,
        Interceptor = Interceptor,
        NumberOfChildren = NoChildren
      };
    }
  }
}
