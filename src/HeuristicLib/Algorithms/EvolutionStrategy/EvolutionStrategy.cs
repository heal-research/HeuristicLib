using HEAL.HeuristicLib.Encodings.RealVector.Mutators;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Creator;
using HEAL.HeuristicLib.Operators.Crossover;
using HEAL.HeuristicLib.Operators.Evaluator;
using HEAL.HeuristicLib.Operators.Interceptor;
using HEAL.HeuristicLib.Operators.Mutator;
using HEAL.HeuristicLib.Operators.Prototypes;
using HEAL.HeuristicLib.Operators.Replacer;
using HEAL.HeuristicLib.Operators.Selector;
using HEAL.HeuristicLib.Operators.Terminator;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Algorithms.EvolutionStrategy;

public record EvolutionStrategyIterationResult<TGenotype>(Population<TGenotype> Population, double MutationStrength) : PopulationIterationResult<TGenotype>(Population) {
  public double MutationStrength { get; } = MutationStrength;
}

public static class EvolutionStrategy {
  public static EvolutionStrategy<TGenotype, TEncoding, TProblem>.Prototype CreatePrototype<TGenotype, TEncoding, TProblem>(int populationSize,
                                                                                                                            int noChildren,
                                                                                                                            EvolutionStrategyType strategy,
                                                                                                                            ICreator<TGenotype, TEncoding, TProblem> creator,
                                                                                                                            IMutator<TGenotype, TEncoding, TProblem> mutator,
                                                                                                                            double initialMutationStrength,
                                                                                                                            ISelector<TGenotype, TEncoding, TProblem> selector,
                                                                                                                            IEvaluator<TGenotype, TEncoding, TProblem> evaluator,
                                                                                                                            int? randomSeed,
                                                                                                                            ITerminator<TGenotype, EvolutionStrategyIterationResult<TGenotype>, TEncoding, TProblem> terminator,
                                                                                                                            ICrossover<TGenotype, TEncoding, TProblem>? crossover = null,
                                                                                                                            IInterceptor<TGenotype, EvolutionStrategyIterationResult<TGenotype>, TEncoding, TProblem>? interceptor = null)
    where TEncoding : class, IEncoding<TGenotype> where TProblem : class, IProblem<TGenotype, TEncoding>
    => new(populationSize, noChildren, strategy, creator, mutator, initialMutationStrength, selector, evaluator, randomSeed, terminator, crossover, interceptor);
}

public class EvolutionStrategy<TGenotype, TEncoding, TProblem>(
  int populationSize,
  EvolutionStrategyType strategy,
  ICreator<TGenotype, TEncoding, TProblem> creator,
  IMutator<TGenotype, TEncoding, TProblem> mutator,
  double initialMutationStrength,
  ISelector<TGenotype, TEncoding, TProblem> selector,
  IEvaluator<TGenotype, TEncoding, TProblem> evaluator,
  int? randomSeed,
  ITerminator<TGenotype, EvolutionStrategyIterationResult<TGenotype>, TEncoding, TProblem> terminator,
  ICrossover<TGenotype, TEncoding, TProblem>? crossover = null,
  IInterceptor<TGenotype, EvolutionStrategyIterationResult<TGenotype>, TEncoding, TProblem>? interceptor = null)
  : IterativeAlgorithm<TGenotype, TEncoding, TProblem, EvolutionStrategyIterationResult<TGenotype>>(terminator, randomSeed, interceptor)
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : class, IProblem<TGenotype, TEncoding> {
  public int PopulationSize { get; } = populationSize;
  public EvolutionStrategyType Strategy { get; } = strategy;
  public ICreator<TGenotype, TEncoding, TProblem> Creator { get; } = creator;
  public IMutator<TGenotype, TEncoding, TProblem> Mutator { get; } = mutator;
  public ICrossover<TGenotype, TEncoding, TProblem>? Crossover { get; } = crossover;
  public double InitialMutationStrength { get; } = initialMutationStrength;
  public ISelector<TGenotype, TEncoding, TProblem> Selector { get; } = selector;
  public IEvaluator<TGenotype, TEncoding, TProblem> Evaluator { get; } = evaluator;

  public override EvolutionStrategyIterationResult<TGenotype> ExecuteStep(TProblem problem, TEncoding searchSpace, EvolutionStrategyIterationResult<TGenotype>? previousIterationResult, IRandomNumberGenerator random) {
    if (previousIterationResult == null) {
      var pop = Creator.Create(PopulationSize, random, searchSpace, problem);
      var fitnesses1 = Evaluator.Evaluate(pop, random, searchSpace, problem);
      return new EvolutionStrategyIterationResult<TGenotype>(Population.From(pop, fitnesses1), InitialMutationStrength);
    }

    IReadOnlyList<TGenotype> parents;
    IReadOnlyList<ObjectiveVector> parentQualities;

    if (Crossover == null) {
      var parentISolutions = Selector.Select(previousIterationResult.Population.Solutions, problem.Objective, PopulationSize, random, problem.SearchSpace, problem);
      parents = parentISolutions.Select(x => x.Genotype).ToArray();
      parentQualities = parentISolutions.Select(x => x.ObjectiveVector).ToArray();
    } else {
      var parentISolutions = Selector.Select(previousIterationResult.Population.Solutions, problem.Objective, PopulationSize * 2, random, problem.SearchSpace, problem);
      parents = Crossover!.Cross(parentISolutions.ToGenotypePairs(), random, searchSpace, problem);
      parentQualities = parentISolutions.Where((_, i) => i % 2 == 0).Select(x => x.ObjectiveVector).ToArray();
    }

    var children = Mutator.Mutate(parents, random, searchSpace, problem);
    var fitnesses = Evaluator.Evaluate(children, random, searchSpace, problem);

    double newMutationStrength = previousIterationResult.MutationStrength;
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

  public class Prototype : PopulationBasedAlgorithmPrototype<TGenotype, TEncoding, TProblem, EvolutionStrategyIterationResult<TGenotype>>,
                           IMutatorPrototype<TGenotype, TEncoding, TProblem>,
                           IOptionalCrossoverPrototype<TGenotype, TEncoding, TProblem> {
    public Prototype(
      int populationSize,
      int noChildren,
      EvolutionStrategyType strategy,
      ICreator<TGenotype, TEncoding, TProblem> creator,
      IMutator<TGenotype, TEncoding, TProblem> mutator,
      double initialInitialMutationStrength,
      ISelector<TGenotype, TEncoding, TProblem> selector,
      IEvaluator<TGenotype, TEncoding, TProblem> evaluator,
      int? randomSeed,
      ITerminator<TGenotype, EvolutionStrategyIterationResult<TGenotype>, TEncoding, TProblem> terminator,
      ICrossover<TGenotype, TEncoding, TProblem>? crossover = null,
      IInterceptor<TGenotype, EvolutionStrategyIterationResult<TGenotype>, TEncoding, TProblem>? interceptor = null) : base(
      populationSize,
      creator,
      selector, evaluator, randomSeed, terminator, interceptor) {
      NoChildren = noChildren;
      Strategy = strategy;
      Mutator = mutator;
      InitialMutationStrength = initialInitialMutationStrength;
      Crossover = crossover;
    }

    public int NoChildren { get; set; }
    public EvolutionStrategyType Strategy { get; set; }
    public IMutator<TGenotype, TEncoding, TProblem> Mutator { get; set; }
    public double InitialMutationStrength { get; set; }
    public ICrossover<TGenotype, TEncoding, TProblem>? Crossover { get; set; }

    public EvolutionStrategy<TGenotype, TEncoding, TProblem> CreateAlgorithm() => new(PopulationSize, Strategy, Creator, Mutator, InitialMutationStrength, Selector, Evaluator, RandomSeed, Terminator, Crossover, Interceptor);
    protected override IIterativeAlgorithm<TGenotype, TEncoding, TProblem, EvolutionStrategyIterationResult<TGenotype>> BuildAlgorithm() => CreateAlgorithm();
  }
}
