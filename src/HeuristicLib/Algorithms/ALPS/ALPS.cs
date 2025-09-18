using System.Diagnostics;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Algorithms.ALPS;

public record ALPSIterationResult<TGenotype> : IIterationResult<TGenotype> {
  public required IReadOnlyList<Population<AgedGenotype<TGenotype>>> Population { get; init; }
}

public record ALPSResult<TGenotype> : IAlgorithmResult<TGenotype> {
  public required IReadOnlyList<Population<AgedGenotype<TGenotype>>> Population { get; init; }
}

public readonly record struct AgedGenotype<TGenotype>(TGenotype InnerGenotype, int Age);

public class AgedEncoding<TGenotype, TEncoding> : IEncoding<AgedGenotype<TGenotype>>
  where TEncoding : class, IEncoding<TGenotype> {
  public TEncoding InnerEncoding { get; }

  public AgedEncoding(TEncoding innerEncoding) {
    InnerEncoding = innerEncoding;
  }

  public bool Contains(AgedGenotype<TGenotype> genotype) {
    return InnerEncoding.Contains(genotype.InnerGenotype);
  }
}

public class AgedProblem<TGenotype, TEncoding, TProblem> : IProblem<AgedGenotype<TGenotype>, AgedEncoding<TGenotype, TEncoding>>
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : class, IProblem<TGenotype, TEncoding> {
  public TProblem InnerProblem { get; }

  public AgedProblem(TProblem innerProblem) {
    InnerProblem = innerProblem;
    SearchSpace = new AgedEncoding<TGenotype, TEncoding>(innerProblem.SearchSpace);
  }

  public Objective Objective => InnerProblem.Objective;

  public AgedEncoding<TGenotype, TEncoding> SearchSpace { get; }

  public IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<AgedGenotype<TGenotype>> solutions) {
    var genotypes = new TGenotype[solutions.Count];
    for (int i = 0; i < solutions.Count; i++) {
      genotypes[i] = solutions[i].InnerGenotype;
    }

    return InnerProblem.Evaluate(genotypes);
  }
}

public class AgedGenotypeCreator<TGenotype, TEncoding, TProblem> : ICreator<AgedGenotype<TGenotype>, AgedEncoding<TGenotype, TEncoding>, AgedProblem<TGenotype, TEncoding, TProblem>>
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : class, IProblem<TGenotype, TEncoding> {
  private readonly ICreator<TGenotype, TEncoding, TProblem> internalCreator;

  public AgedGenotypeCreator(ICreator<TGenotype, TEncoding, TProblem> internalCreator) {
    this.internalCreator = internalCreator;
  }

  public IReadOnlyList<AgedGenotype<TGenotype>> Create(int count, IRandomNumberGenerator random, AgedEncoding<TGenotype, TEncoding> encoding, AgedProblem<TGenotype, TEncoding, TProblem> problem) {
    var offspring = new AgedGenotype<TGenotype>[count];
    var genotypes = internalCreator.Create(count, random, encoding.InnerEncoding, problem.InnerProblem);
    for (int i = 0; i < count; i++) {
      offspring[i] = new AgedGenotype<TGenotype>(genotypes[i], 0);
    }

    return offspring;
  }
}

public class AgedSelector<TGenotype, TEncoding, TProblem> : ISelector<AgedGenotype<TGenotype>, AgedEncoding<TGenotype, TEncoding>, AgedProblem<TGenotype, TEncoding, TProblem>>
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : class, IProblem<TGenotype, TEncoding> {
  private readonly ISelector<TGenotype, TEncoding, TProblem> internalSelector;

  public AgedSelector(ISelector<TGenotype, TEncoding, TProblem> internalSelector) {
    this.internalSelector = internalSelector;
  }

  public IReadOnlyList<Solution<AgedGenotype<TGenotype>>> Select(IReadOnlyList<Solution<AgedGenotype<TGenotype>>> population, Objective objective, int count, IRandomNumberGenerator random, AgedEncoding<TGenotype, TEncoding> encoding, AgedProblem<TGenotype, TEncoding, TProblem> problem) {
    var innerPopulation = new Solution<TGenotype>[population.Count];
    for (int i = 0; i < population.Count; i++) {
      innerPopulation[i] = new Solution<TGenotype>(population[i].Genotype.InnerGenotype, population[i].ObjectiveVector);
    }

    var selected = internalSelector.Select(innerPopulation, objective, count, random, encoding.InnerEncoding, problem.InnerProblem);
    var result = new Solution<AgedGenotype<TGenotype>>[selected.Count];
    for (int i = 0; i < selected.Count; i++) {
      // Find the original solution to get the age
      var originalSolution = population.Single(s => Equals(s.Genotype.InnerGenotype, selected[i].Genotype));
      result[i] = originalSolution;
    }

    return result;
  }
}

public class AgedReplacer<TGenotype, TEncoding, TProblem> : IReplacer<AgedGenotype<TGenotype>, AgedEncoding<TGenotype, TEncoding>, AgedProblem<TGenotype, TEncoding, TProblem>>
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : class, IProblem<TGenotype, TEncoding> {
  private readonly IReplacer<TGenotype, TEncoding, TProblem> innerReplacer;

  public AgedReplacer(IReplacer<TGenotype, TEncoding, TProblem> innerReplacer) {
    this.innerReplacer = innerReplacer;
  }

  public IReadOnlyList<Solution<AgedGenotype<TGenotype>>> Replace(IReadOnlyList<Solution<AgedGenotype<TGenotype>>> previousPopulation, IReadOnlyList<Solution<AgedGenotype<TGenotype>>> offspringPopulation, Objective objective, IRandomNumberGenerator random, AgedEncoding<TGenotype, TEncoding> encoding, AgedProblem<TGenotype, TEncoding, TProblem> problem) {
    var innerPreviousPopulation = new Solution<TGenotype>[previousPopulation.Count];
    for (int i = 0; i < previousPopulation.Count; i++) {
      innerPreviousPopulation[i] = new Solution<TGenotype>(previousPopulation[i].Genotype.InnerGenotype, previousPopulation[i].ObjectiveVector);
    }

    var innerOffspringPopulation = new Solution<TGenotype>[offspringPopulation.Count];
    for (int i = 0; i < offspringPopulation.Count; i++) {
      innerOffspringPopulation[i] = new Solution<TGenotype>(offspringPopulation[i].Genotype.InnerGenotype, offspringPopulation[i].ObjectiveVector);
    }

    var replaced = innerReplacer.Replace(innerPreviousPopulation, innerOffspringPopulation, objective, random, encoding.InnerEncoding, problem.InnerProblem);
    var result = new Solution<AgedGenotype<TGenotype>>[replaced.Count];
    for (int i = 0; i < replaced.Count; i++) {
      // Find the original solution to get the age
      var originalSolution = previousPopulation.Concat(offspringPopulation).Single(s => Equals(s.Genotype.InnerGenotype, replaced[i].Genotype));
      result[i] = originalSolution;
    }

    return result;
  }

  public int GetOffspringCount(int populationSize) {
    return innerReplacer.GetOffspringCount(populationSize);
  }
}

public class AgedCrossover<TGenotype, TEncoding, TProblem> : ICrossover<AgedGenotype<TGenotype>, AgedEncoding<TGenotype, TEncoding>, AgedProblem<TGenotype, TEncoding, TProblem>>
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : class, IProblem<TGenotype, TEncoding> {
  private readonly ICrossover<TGenotype, TEncoding, TProblem> internalCrossover;

  public AgedCrossover(ICrossover<TGenotype, TEncoding, TProblem> internalCrossover) {
    this.internalCrossover = internalCrossover;
  }

  public IReadOnlyList<AgedGenotype<TGenotype>> Cross(IReadOnlyList<(AgedGenotype<TGenotype>, AgedGenotype<TGenotype>)> parents, IRandomNumberGenerator random, AgedEncoding<TGenotype, TEncoding> encoding, AgedProblem<TGenotype, TEncoding, TProblem> problem) {
    var innerParents = new (TGenotype, TGenotype)[parents.Count];
    for (int i = 0; i < parents.Count; i++) {
      innerParents[i] = (parents[i].Item1.InnerGenotype, parents[i].Item2.InnerGenotype);
    }

    var offspring = internalCrossover.Cross(innerParents, random, encoding.InnerEncoding, problem.InnerProblem);
    var result = new AgedGenotype<TGenotype>[offspring.Count];
    for (int i = 0; i < offspring.Count; i++) {
      int newAge = Math.Max(parents[i].Item1.Age, parents[i].Item2.Age) + 1;
      result[i] = new AgedGenotype<TGenotype>(offspring[i], newAge);
    }

    return result;
  }
}

public class AgedMutator<TGenotype, TEncoding, TProblem> : IMutator<AgedGenotype<TGenotype>, AgedEncoding<TGenotype, TEncoding>, AgedProblem<TGenotype, TEncoding, TProblem>>
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : class, IProblem<TGenotype, TEncoding> {
  private readonly IMutator<TGenotype, TEncoding, TProblem> internalMutator;

  public AgedMutator(IMutator<TGenotype, TEncoding, TProblem> internalMutator) {
    this.internalMutator = internalMutator;
  }

  public IReadOnlyList<AgedGenotype<TGenotype>> Mutate(IReadOnlyList<AgedGenotype<TGenotype>> population, IRandomNumberGenerator random, AgedEncoding<TGenotype, TEncoding> encoding, AgedProblem<TGenotype, TEncoding, TProblem> problem) {
    var innerPopulation = new TGenotype[population.Count];
    for (int i = 0; i < population.Count; i++) {
      innerPopulation[i] = population[i].InnerGenotype;
    }

    var mutated = internalMutator.Mutate(innerPopulation, random, encoding.InnerEncoding, problem.InnerProblem);
    var result = new AgedGenotype<TGenotype>[mutated.Count];
    for (int i = 0; i < mutated.Count; i++) {
      // Find the original solution to get the age
      var originalSolution = population.Single(s => Equals(s.InnerGenotype, mutated[i]));
      result[i] = originalSolution;
    }

    return result;
  }
}

public class GeneticAlgorithm<TGenotype, TEncoding, TProblem>
  : IterativeAlgorithm<TGenotype, TEncoding, TProblem, ALPSResult<TGenotype>, ALPSIterationResult<TGenotype>>
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : class, IProblem<TGenotype, TEncoding> {
  public int PopulationSize { get; }
  public ICreator<TGenotype, TEncoding, TProblem> Creator { get; }
  public ICrossover<TGenotype, TEncoding, TProblem> Crossover { get; }
  public IMutator<TGenotype, TEncoding, TProblem> Mutator { get; }
  public double MutationRate { get; }
  public ISelector<TGenotype, TEncoding, TProblem> Selector { get; }
  public int Elites { get; }
  // public IReplacer<TGenotype, TEncoding, TProblem> Replacer { get; }
  public int RandomSeed { get; }

  private readonly IRandomNumberGenerator algorithmRandom;

  private readonly AgedGenotypeCreator<TGenotype, TEncoding, TProblem> agedCreator;
  private readonly AgedCrossover<TGenotype, TEncoding, TProblem> agedCrossover;
  private readonly AgedMutator<TGenotype, TEncoding, TProblem> agedMutator;
  private readonly AgedSelector<TGenotype, TEncoding, TProblem> agedSelector;
  private readonly AgedReplacer<TGenotype, TEncoding, TProblem> agedReplacer;

  private readonly MultiMutator<TGenotype, TEncoding, TProblem> internalMutator;
  private readonly IReplacer<TGenotype, TEncoding, TProblem> internalReplacer;

  public OperatorMetric CreatorMetric { get; protected set; } = OperatorMetric.Zero;
  public OperatorMetric CrossoverMetric { get; protected set; } = OperatorMetric.Zero;
  public OperatorMetric MutationMetric { get; protected set; } = OperatorMetric.Zero;
  public OperatorMetric SelectionMetric { get; protected set; } = OperatorMetric.Zero;
  public OperatorMetric ReplacementMetric { get; protected set; } = OperatorMetric.Zero;

  public GeneticAlgorithm(
    int populationSize,
    ICreator<TGenotype, TEncoding, TProblem> creator,
    ICrossover<TGenotype, TEncoding, TProblem> crossover,
    IMutator<TGenotype, TEncoding, TProblem> mutator, double mutationRate,
    ISelector<TGenotype, TEncoding, TProblem> selector,
    // IReplacer<TGenotype, TEncoding, TProblem> replacer,
    int elites,
    int? randomSeed,
    ITerminator<TGenotype, ALPSIterationResult<TGenotype>, TEncoding, TProblem> terminator,
    IInterceptor<TGenotype, ALPSIterationResult<TGenotype>, TEncoding, TProblem>? interceptor = null
  ) : base(terminator, interceptor) {
    PopulationSize = populationSize;
    Creator = creator;
    Crossover = crossover;
    Mutator = mutator;
    MutationRate = mutationRate;
    Selector = selector;
    //Replacer = replacer;
    Elites = elites;
    RandomSeed = randomSeed ?? SystemRandomNumberGenerator.RandomSeed();

    algorithmRandom = new SystemRandomNumberGenerator(RandomSeed);

    internalMutator = new MultiMutator<TGenotype, TEncoding, TProblem>([mutator, new NoChangeMutator<TGenotype>()], [mutationRate, 1 - mutationRate]);
    internalReplacer = new ElitismReplacer<TGenotype>(elites);

    agedCreator = new AgedGenotypeCreator<TGenotype, TEncoding, TProblem>(Creator);
    agedCrossover = new AgedCrossover<TGenotype, TEncoding, TProblem>(Crossover);
    agedMutator = new AgedMutator<TGenotype, TEncoding, TProblem>(internalMutator);
    agedSelector = new AgedSelector<TGenotype, TEncoding, TProblem>(Selector);
    agedReplacer = new AgedReplacer<TGenotype, TEncoding, TProblem>(internalReplacer);
  }

  public override ALPSIterationResult<TGenotype> ExecuteStep(TProblem problem, TEncoding? searchSpace = null, ALPSIterationResult<TGenotype>? previousIterationResult = null, IRandomNumberGenerator? random = null) {
    if (searchSpace is ISubencodingComparable<TEncoding> s && !s.IsSubspaceOf(problem.SearchSpace))
      throw new ArgumentException("The provided search space is not a subspace of the problem's search space.");

    var activeSearchSpace = searchSpace ?? problem.SearchSpace;

    var agedProblem = new AgedProblem<TGenotype, TEncoding, TProblem>(problem);
    var agedEncoding = new AgedEncoding<TGenotype, TEncoding>(activeSearchSpace);

    var iterationRandom = (random ?? algorithmRandom).Fork(CurrentIteration);
    return previousIterationResult switch {
      null => ExecuteInitialization(agedProblem, agedEncoding, iterationRandom),
      _ => ExecuteGeneration(agedProblem, agedEncoding, previousIterationResult, iterationRandom)
    };
  }

  protected virtual ALPSIterationResult<TGenotype> ExecuteInitialization(AgedProblem<TGenotype, TEncoding, TProblem> problem, AgedEncoding<TGenotype, TEncoding> searchSpace, IRandomNumberGenerator iterationRandom) {
    var startCreating = Stopwatch.GetTimestamp();
    var initialLayerPopulation = agedCreator.Create(PopulationSize, iterationRandom, searchSpace, problem);
    var endCreating = Stopwatch.GetTimestamp();
    CreatorMetric += new OperatorMetric(PopulationSize, Stopwatch.GetElapsedTime(startCreating, endCreating));

    var startEvaluating = Stopwatch.GetTimestamp();
    var fitnesses = problem.Evaluate(initialLayerPopulation);
    var endEvaluating = Stopwatch.GetTimestamp();
    EvaluationsMetric += new OperatorMetric(PopulationSize, Stopwatch.GetElapsedTime(startEvaluating, endEvaluating));

    var result = new ALPSIterationResult<TGenotype>() { Population = [Population.From(initialLayerPopulation, fitnesses)] };

    return result;
  }

  protected virtual ALPSIterationResult<TGenotype> ExecuteGeneration(AgedProblem<TGenotype, TEncoding, TProblem> problem, AgedEncoding<TGenotype, TEncoding> searchSpace, ALPSIterationResult<TGenotype> previousGenerationResult, IRandomNumberGenerator iterationRandom) {
    int offspringCount = internalReplacer.GetOffspringCount(PopulationSize);

    var oldPopulation = previousGenerationResult.Population[0].ToArray();

    var startSelection = Stopwatch.GetTimestamp();
    var parents = agedSelector.Select(oldPopulation, problem.Objective, offspringCount * 2, iterationRandom, searchSpace, problem);
    var endSelection = Stopwatch.GetTimestamp();
    SelectionMetric += new OperatorMetric(parents.Count, Stopwatch.GetElapsedTime(startSelection, endSelection));

    var parentPairs = new ValueTuple<AgedGenotype<TGenotype>, AgedGenotype<TGenotype>>[offspringCount];
    for (int i = 0, j = 0; i < offspringCount; i++, j += 2) {
      parentPairs[i] = (parents[j].Genotype, parents[j + 1].Genotype);
    }

    var startCrossover = Stopwatch.GetTimestamp();
    int crossoverCount = 0;
    var population = agedCrossover.Cross(parentPairs, iterationRandom, searchSpace, problem);
    var endCrossover = Stopwatch.GetTimestamp();
    CrossoverMetric += new OperatorMetric(crossoverCount, Stopwatch.GetElapsedTime(startCrossover, endCrossover));

    var startMutation = Stopwatch.GetTimestamp();
    int mutationCount = 0;
    population = agedMutator.Mutate(population, iterationRandom, searchSpace, problem);
    var endMutation = Stopwatch.GetTimestamp();
    MutationMetric += new OperatorMetric(mutationCount, Stopwatch.GetElapsedTime(startMutation, endMutation));

    var startEvaluation = Stopwatch.GetTimestamp();
    var fitnesses = problem.Evaluate(population);
    var endEvaluation = Stopwatch.GetTimestamp();
    EvaluationsMetric += new OperatorMetric(fitnesses.Count, Stopwatch.GetElapsedTime(startEvaluation, endEvaluation));

    var evaluatedPopulation = Population.From(population, fitnesses);

    var startReplacement = Stopwatch.GetTimestamp();
    var newPopulation = agedReplacer.Replace(oldPopulation, evaluatedPopulation.ToList(), problem.Objective, iterationRandom, searchSpace, problem);
    var endReplacement = Stopwatch.GetTimestamp();
    ReplacementMetric += new OperatorMetric(1, Stopwatch.GetElapsedTime(startReplacement, endReplacement));

    var result = new ALPSIterationResult<TGenotype>() {
      Population = [new(new(newPopulation))],
    };

    return result;
  }

  protected override ALPSResult<TGenotype> FinalizeResult(ALPSIterationResult<TGenotype> iterationResult, TProblem problem) {
    return new ALPSResult<TGenotype>() { Population = iterationResult.Population };
  }
}

public class GeneticAlgorithm<TGenotype, TEncoding>
  : GeneticAlgorithm<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>>
  where TEncoding : class, IEncoding<TGenotype> {
  public GeneticAlgorithm(int populationSize, ICreator<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>> creator, ICrossover<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>> crossover, IMutator<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>> mutator, double mutationRate, ISelector<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>> selector, int elites, int randomSeed, ITerminator<TGenotype, ALPSIterationResult<TGenotype>, TEncoding, IProblem<TGenotype, TEncoding>> terminator, IInterceptor<TGenotype, ALPSIterationResult<TGenotype>, TEncoding, IProblem<TGenotype, TEncoding>>? interceptor = null)
    : base(populationSize, creator, crossover, mutator, mutationRate, selector, elites, randomSeed, terminator, interceptor) { }
}

public class GeneticAlgorithm<TGenotype>
  : GeneticAlgorithm<TGenotype, IEncoding<TGenotype>> {
  public GeneticAlgorithm(int populationSize,
                          ICreator<TGenotype, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>> creator,
                          ICrossover<TGenotype, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>> crossover,
                          IMutator<TGenotype, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>> mutator,
                          double mutationRate,
                          ISelector<TGenotype, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>> selector,
                          int elites,
                          int randomSeed,
                          ITerminator<TGenotype, ALPSIterationResult<TGenotype>, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>> terminator,
                          IInterceptor<TGenotype, ALPSIterationResult<TGenotype>, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>>? interceptor = null)
    : base(populationSize, creator, crossover, mutator, mutationRate, selector, elites, randomSeed, terminator, interceptor) { }
}
