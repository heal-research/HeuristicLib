using System;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Algorithms.GeneticAlgorithm;

public class GeneticAlgorithm<TGenotype, TEncoding, TProblem>(
  int populationSize,
  ICreator<TGenotype, TEncoding, TProblem> creator,
  ICrossover<TGenotype, TEncoding, TProblem> crossover,
  IMutator<TGenotype, TEncoding, TProblem> mutator,
  double mutationRate,
  ISelector<TGenotype, TEncoding, TProblem> selector,
  int elites,
  int? randomSeed,
  ITerminator<TGenotype, PopulationIterationResult<TGenotype>, TEncoding, TProblem> terminator,
  IInterceptor<TGenotype, PopulationIterationResult<TGenotype>, TEncoding, TProblem>? interceptor = null)
  : IterativeAlgorithm<TGenotype, TEncoding, TProblem, PopulationResult<TGenotype>, PopulationIterationResult<TGenotype>>(terminator, randomSeed, interceptor)
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : class, IProblem<TGenotype, TEncoding> {
  public int PopulationSize { get; } = populationSize;
  public ICreator<TGenotype, TEncoding, TProblem> Creator { get; } = creator;
  public ICrossover<TGenotype, TEncoding, TProblem> Crossover { get; } = crossover;
  public IMutator<TGenotype, TEncoding, TProblem> Mutator { get; } = mutator;
  public ISelector<TGenotype, TEncoding, TProblem> Selector { get; } = selector;
  private readonly MultiMutator<TGenotype, TEncoding, TProblem> internalMutator = new([mutator, new NoChangeMutator<TGenotype>()], [mutationRate, 1 - mutationRate]);
  private readonly ElitismReplacer<TGenotype> internalReplacer = new(elites);

  public override PopulationIterationResult<TGenotype> ExecuteStep(TProblem problem, TEncoding searchSpace, PopulationIterationResult<TGenotype>? previousIterationResult, IRandomNumberGenerator random) {
    return previousIterationResult switch {
      null => ExecuteInitialization(problem, searchSpace, random),
      _ => ExecuteGeneration(problem, searchSpace, previousIterationResult, random)
    };
  }

  protected virtual PopulationIterationResult<TGenotype> ExecuteInitialization(TProblem problem, TEncoding searchSpace, IRandomNumberGenerator random) {
    var population = Creator.Create(PopulationSize, random, searchSpace, problem);
    var objectives = problem.Evaluate(population);
    return new PopulationIterationResult<TGenotype>(Population.From(population, objectives));
  }

  protected virtual PopulationIterationResult<TGenotype> ExecuteGeneration(TProblem problem, TEncoding searchSpace, PopulationIterationResult<TGenotype> previousGenerationResult, IRandomNumberGenerator random) {
    var offspringCount = internalReplacer.GetOffspringCount(PopulationSize);
    var oldPopulation = previousGenerationResult.Population.Solutions;

    var parents = Selector.Select(oldPopulation, problem.Objective, offspringCount * 2, random, searchSpace, problem).ToGenotypePairs();
    var population = Crossover.Cross(parents, random, searchSpace, problem);
    population = internalMutator.Mutate(population, random, searchSpace, problem);
    var fitnesses = problem.Evaluate(population);
    var newPopulation = internalReplacer.Replace(oldPopulation, Population.From(population, fitnesses).Solutions, problem.Objective, random);

    return new PopulationIterationResult<TGenotype>(Population.From(newPopulation));
  }

  protected override PopulationResult<TGenotype> FinalizeResult(PopulationIterationResult<TGenotype> iterationResult, TProblem problem) => new(iterationResult.Population);
}

public class GeneticAlgorithm<TGenotype, TEncoding>(int populationSize, ICreator<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>> creator, ICrossover<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>> crossover, IMutator<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>> mutator, double mutationRate, ISelector<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>> selector, int elites, int randomSeed, ITerminator<TGenotype, PopulationIterationResult<TGenotype>, TEncoding, IProblem<TGenotype, TEncoding>> terminator, IInterceptor<TGenotype, PopulationIterationResult<TGenotype>, TEncoding, IProblem<TGenotype, TEncoding>>? interceptor = null)
  : GeneticAlgorithm<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>>(populationSize, creator, crossover, mutator, mutationRate, selector, elites, randomSeed, terminator, interceptor)
  where TEncoding : class, IEncoding<TGenotype>;

public class GeneticAlgorithm<TGenotype>(int populationSize, ICreator<TGenotype, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>> creator, ICrossover<TGenotype, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>> crossover, IMutator<TGenotype, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>> mutator, double mutationRate, ISelector<TGenotype, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>> selector, int elites, int randomSeed, ITerminator<TGenotype, PopulationIterationResult<TGenotype>, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>> terminator, IInterceptor<TGenotype, PopulationIterationResult<TGenotype>, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>>? interceptor = null)
  : GeneticAlgorithm<TGenotype, IEncoding<TGenotype>>(populationSize, creator, crossover, mutator, mutationRate, selector, elites, randomSeed, terminator, interceptor);
