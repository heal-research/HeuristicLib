using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Algorithms.EvolutionStrategy;

public record EvolutionStrategyIterationResult<TGenotype>(Population<TGenotype> Population, double MutationStrength) : PopulationIterationResult<TGenotype>(Population);

public class EvolutionStrategy<TGenotype, TEncoding, TProblem> : IterativeAlgorithm<TGenotype, TEncoding, TProblem, PopulationResult<TGenotype>, EvolutionStrategyIterationResult<TGenotype>> where TEncoding : class, IEncoding<TGenotype> where TProblem : class, IProblem<TGenotype, TEncoding> {
  public EvolutionStrategy(
    int populationSize,
    int children,
    EvolutionStrategyType strategy,
    ICreator<TGenotype, TEncoding, TProblem> creator,
    IMutator<TGenotype, TEncoding, TProblem> mutator,
    double initialMutationStrength,
    ISelector<TGenotype, TEncoding, TProblem> selector,
    int? randomSeed, ITerminator<TGenotype, EvolutionStrategyIterationResult<TGenotype>, TEncoding, TProblem> terminator,
    IInterceptor<TGenotype, EvolutionStrategyIterationResult<TGenotype>, TEncoding, TProblem>? interceptor) : base(terminator, randomSeed, interceptor) {
    PopulationSize = populationSize;
    Children = children;
    Strategy = strategy;
    Creator = creator;
    Mutator = mutator;
    InitialMutationStrength = initialMutationStrength;
    Selector = selector;
    Interceptor = interceptor;
  }

  public int PopulationSize { get; }
  public int Children { get; }
  public EvolutionStrategyType Strategy { get; }
  public ICreator<TGenotype, TEncoding, TProblem> Creator { get; }
  //public ICrossover<TGenotype, TGenotypeSearchSpace>? Crossover { get; }
  public IMutator<TGenotype, TEncoding, TProblem> Mutator { get; }
  public double InitialMutationStrength { get; }
  public ISelector<TGenotype, TEncoding, TProblem> Selector { get; }
  public IInterceptor<TGenotype, EvolutionStrategyIterationResult<TGenotype>, TEncoding, TProblem>? Interceptor { get; }

  public override EvolutionStrategyIterationResult<TGenotype> ExecuteStep(TProblem problem, TEncoding? searchSpace = default, EvolutionStrategyIterationResult<TGenotype>? previousIterationResult = default, IRandomNumberGenerator? random = null) {
    random ??= AlgorithmRandom;
    searchSpace ??= problem.SearchSpace;

    if (previousIterationResult == null) {
      var pop = Creator.Create(PopulationSize, random, searchSpace, problem);
      var objs = problem.Evaluate(pop);
      return new EvolutionStrategyIterationResult<TGenotype>(Population.From(pop, objs), InitialMutationStrength);
    }

    var oldPopulation = previousIterationResult.Population.Solutions;

    var parents = Selector.Select(oldPopulation, problem.Objective, PopulationSize, random, problem.SearchSpace, problem).ToArray();
    var children = Mutator.Mutate(parents.Select(x => x.Genotype).ToArray(), random, searchSpace, problem);

    // ToDo: optional crossover
    // var startDecoding = Stopwatch.GetTimestamp();
    // var phenotypePopulation = genotypePopulation.Select(genotype => optimizable.Decoder.Decode(genotype)).ToArray();
    // var endDecoding = Stopwatch.GetTimestamp();

    var fitnesses = problem.Evaluate(children);
    var successes = parents.Zip(fitnesses).Count(t => t.Item2.CompareTo(t.Item1.ObjectiveVector, problem.Objective) == DominanceRelation.Dominates);
    var successRate = successes / (double)parents.Length;
    double newMutationStrength = successRate switch {
      > 0.2 => previousIterationResult.MutationStrength * 1.5,
      < 0.2 => previousIterationResult.MutationStrength / 1.5,
      _ => previousIterationResult.MutationStrength
    };

    var population = Population.From(children, fitnesses);

    // ToDo: to create execution/instance
    Replacer<TGenotype> replacer = Strategy switch {
      EvolutionStrategyType.Comma => new ElitismReplacer<TGenotype>(0),
      EvolutionStrategyType.Plus => new PlusSelectionReplacer<TGenotype>(),
      _ => throw new InvalidOperationException($"Unknown strategy {Strategy}")
    };
    var newPopulation = replacer.Replace(oldPopulation, population.Solutions, problem.Objective, random);
    return new EvolutionStrategyIterationResult<TGenotype>(Population.From(newPopulation), newMutationStrength);
  }

  protected override PopulationResult<TGenotype> FinalizeResult(EvolutionStrategyIterationResult<TGenotype> iterationResult, TProblem problem) {
    return new PopulationResult<TGenotype>(iterationResult.Population);
  }
}
