using HEAL.HeuristicLib.Encodings.RealVector.Mutators;
using HEAL.HeuristicLib.Operators.Creator;
using HEAL.HeuristicLib.Operators.Crossover;
using HEAL.HeuristicLib.Operators.Evaluator;
using HEAL.HeuristicLib.Operators.Mutator;
using HEAL.HeuristicLib.Operators.Prototypes;
using HEAL.HeuristicLib.Operators.Replacer;
using HEAL.HeuristicLib.Operators.Selector;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Algorithms.EvolutionStrategy;

public record EvolutionStrategyIterationResult<TGenotype>(Population<TGenotype> Population, double MutationStrength) : PopulationIterationResult<TGenotype>(Population) {
  public double MutationStrength { get; } = MutationStrength;
}

public static class EvolutionStrategy {
  public static EvolutionStrategy<TGenotype, TEncoding, TProblem>.Builder GetBuilder<TGenotype, TEncoding, TProblem>(
    ICreator<TGenotype, TEncoding, TProblem> creator,
    IMutator<TGenotype, TEncoding, TProblem> mutator)
    where TEncoding : class, IEncoding<TGenotype> where TProblem : class, IProblem<TGenotype, TEncoding> where TGenotype : class => new() {
    Mutator = mutator,
    InitialMutationStrength = (mutator as IVariableStrengthMutator<TGenotype, TEncoding, TProblem>)?.MutationStrength ?? 0,
    Creator = creator
  };
}

public class EvolutionStrategy<TGenotype, TEncoding, TProblem>
  : IterativeAlgorithm<TGenotype, TEncoding, TProblem, EvolutionStrategyIterationResult<TGenotype>>
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : class, IProblem<TGenotype, TEncoding>
  where TGenotype : class {
  public required int PopulationSize { get; init; }
  public required int NumberOfChildren { get; init; }
  public required EvolutionStrategyType Strategy { get; init; }
  public required IMutator<TGenotype, TEncoding, TProblem> Mutator { get; init; }
  public required ICrossover<TGenotype, TEncoding, TProblem>? Crossover { get; init; }
  public double InitialMutationStrength { get; init; } = 1.0;
  public required ISelector<TGenotype, TEncoding, TProblem> Selector { get; init; }

  public override EvolutionStrategyIterationResult<TGenotype> ExecuteStep(TProblem problem, TEncoding searchSpace, EvolutionStrategyIterationResult<TGenotype>? previousIterationResult, IRandomNumberGenerator random) {
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

    var newMutationStrength = previousIterationResult.MutationStrength;
    if (Mutator is IVariableStrengthMutator<TGenotype, TEncoding, TProblem> vm) {
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
                           TGenotype, TEncoding, TProblem, EvolutionStrategyIterationResult<TGenotype>,
                           EvolutionStrategy<TGenotype, TEncoding, TProblem>>,
                         IMutatorPrototype<TGenotype, TEncoding, TProblem>,
                         IOptionalCrossoverPrototype<TGenotype, TEncoding, TProblem> {
    public int NoChildren { get; set; } = 100;
    public EvolutionStrategyType Strategy { get; set; } = EvolutionStrategyType.Plus;
    public required IMutator<TGenotype, TEncoding, TProblem> Mutator { get; set; }
    public required double InitialMutationStrength { get; set; }
    public ICrossover<TGenotype, TEncoding, TProblem>? Crossover { get; set; }

    public override EvolutionStrategy<TGenotype, TEncoding, TProblem> BuildAlgorithm() {
      return new EvolutionStrategy<TGenotype, TEncoding, TProblem> {
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
