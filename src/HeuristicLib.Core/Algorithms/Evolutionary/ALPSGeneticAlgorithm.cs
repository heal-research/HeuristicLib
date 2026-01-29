using System.Diagnostics;
using HEAL.HeuristicLib.Collections;
using HEAL.HeuristicLib.OperatorExtensions.MeasuredOperators;
using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Operators.Crossovers;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Operators.Interceptors;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Operators.Replacers;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Operators.Terminators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms.Evolutionary;

public record AgedGenotype<TGenotype>(TGenotype InnerGenotype, int Age);

public record AlpsIterationState<TGenotype> : AlgorithmState
{
  public required IReadOnlyList<Population<AgedGenotype<TGenotype>>> Population { get; init; }
}

public class AgedSearchSpace<TGenotype, TSearchSpace>(TSearchSpace innerSearchSpace) : ISearchSpace<AgedGenotype<TGenotype>>
  where TSearchSpace : class, ISearchSpace<TGenotype>
{
  public TSearchSpace InnerSearchSpace { get; } = innerSearchSpace;

  public bool Contains(AgedGenotype<TGenotype> genotype) => InnerSearchSpace.Contains(genotype.InnerGenotype);
}

public class AgedProblem<TGenotype, TSearchSpace, TProblem>(TProblem innerProblem) : IProblem<AgedGenotype<TGenotype>, AgedSearchSpace<TGenotype, TSearchSpace>>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  public TProblem InnerProblem { get; } = innerProblem;

  public Objective Objective => InnerProblem.Objective;
  public ObjectiveVector Evaluate(AgedGenotype<TGenotype> solution, IRandomNumberGenerator random) => InnerProblem.Evaluate(solution.InnerGenotype, random);

  public AgedSearchSpace<TGenotype, TSearchSpace> SearchSpace { get; } = new(innerProblem.SearchSpace);

  //public IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<AgedGenotype<TGenotype>> ISolutions) {
  //  var genotypes = new TGenotype[ISolutions.Count];
  //  for (int i = 0; i < ISolutions.Count; i++) {
  //    genotypes[i] = ISolutions[i].InnerGenotype;
  //  }

  //  return InnerProblem.Evaluate(genotypes);
  //}
}

public class AlpsGeneticAlgorithm<TGenotype, TSearchSpace, TProblem>
  : IterativeAlgorithm<TGenotype, TSearchSpace, TProblem, AlpsIterationState<TGenotype>>
  where TGenotype : class
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  public int PopulationSize { get; }
  public ICreator<TGenotype, TSearchSpace, TProblem> Creator { get; }
  public ICrossover<TGenotype, TSearchSpace, TProblem> Crossover { get; }
  public IMutator<TGenotype, TSearchSpace, TProblem> Mutator { get; }
  public double MutationRate { get; }
  public ISelector<TGenotype, TSearchSpace, TProblem> Selector { get; }
  public int Elites { get; }
  // public IReplacer<TGenotype, TSearchSpace, TProblem> Replacer { get; }

  private readonly AgedCreator agedCreator;
  private readonly AgedCrossover agedCrossover;
  private readonly AgedMutator agedMutator;
  private readonly AgedSelector agedSelector;
  private readonly AgedReplacer agedReplacer;

  private readonly MultiMutator<TGenotype, TSearchSpace, TProblem> internalMutator;
  private readonly IReplacer<TGenotype, TSearchSpace, TProblem> internalReplacer;

  public OperatorMetric CreatorMetric { get; protected set; } = OperatorMetric.Zero;
  public OperatorMetric CrossoverMetric { get; protected set; } = OperatorMetric.Zero;
  public OperatorMetric MutationMetric { get; protected set; } = OperatorMetric.Zero;
  public OperatorMetric SelectionMetric { get; protected set; } = OperatorMetric.Zero;
  public OperatorMetric ReplacementMetric { get; protected set; } = OperatorMetric.Zero;

  public AlpsGeneticAlgorithm(
    int populationSize,
    ICreator<TGenotype, TSearchSpace, TProblem> creator,
    ICrossover<TGenotype, TSearchSpace, TProblem> crossover,
    IMutator<TGenotype, TSearchSpace, TProblem> mutator, double mutationRate,
    ISelector<TGenotype, TSearchSpace, TProblem> selector,
    IEvaluator<TGenotype, TSearchSpace, TProblem> evaluator,
    // IReplacer<TGenotype, TSearchSpace, TProblem> replacer,
    int elites,
    ITerminator<TGenotype, AlpsIterationState<TGenotype>, TSearchSpace, TProblem> terminator,
    IInterceptor<TGenotype, AlpsIterationState<TGenotype>, TSearchSpace, TProblem>? interceptor = null
  )
  {
    PopulationSize = populationSize;
    Creator = creator;
    Crossover = crossover;
    Mutator = mutator;
    MutationRate = mutationRate;
    Selector = selector;
    Evaluator = evaluator;
    //Replacer = replacer;
    Elites = elites;

    internalMutator = new MultiMutator<TGenotype, TSearchSpace, TProblem>([mutator, new NoChangeMutator<TGenotype>()], [mutationRate, 1 - mutationRate]);
    internalReplacer = new ElitismReplacer<TGenotype>(elites);

    agedCreator = new AgedCreator(Creator);
    agedCrossover = new AgedCrossover(Crossover);
    agedMutator = new AgedMutator(internalMutator);
    agedSelector = new AgedSelector(Selector);
    agedReplacer = new AgedReplacer(internalReplacer);
  }

  public override AlpsIterationState<TGenotype> ExecuteStep(TProblem problem, AlpsIterationState<TGenotype>? previousState, IRandomNumberGenerator random)
  {
    var agedProblem = new AgedProblem<TGenotype, TSearchSpace, TProblem>(problem);
    var agedSearchSpace = new AgedSearchSpace<TGenotype, TSearchSpace>(problem.SearchSpace);
    //var iteration = previousState?.CurrentIteration + 1 ?? 0;
    return previousState switch {
      null => ExecuteInitialization(agedProblem, agedSearchSpace, random),
      _ => ExecuteGeneration(agedProblem, agedSearchSpace, previousState, random)
    };
  }

  protected virtual AlpsIterationState<TGenotype> ExecuteInitialization(AgedProblem<TGenotype, TSearchSpace, TProblem> problem, AgedSearchSpace<TGenotype, TSearchSpace> searchSpace, IRandomNumberGenerator iterationRandom)
  {
    var startCreating = Stopwatch.GetTimestamp();
    var initialLayerPopulation = agedCreator.Create(PopulationSize, iterationRandom, searchSpace, problem);
    var endCreating = Stopwatch.GetTimestamp();
    CreatorMetric += new OperatorMetric(PopulationSize, Stopwatch.GetElapsedTime(startCreating, endCreating));

    var startEvaluating = Stopwatch.GetTimestamp();
    var fitnesses = Evaluator.Evaluate(initialLayerPopulation.Select(x => x.InnerGenotype).ToArray(), iterationRandom, searchSpace.InnerSearchSpace, problem.InnerProblem);
    var endEvaluating = Stopwatch.GetTimestamp();

    var result = new AlpsIterationState<TGenotype> {
      Population = [Population.From(initialLayerPopulation, fitnesses)],
      //CurrentIteration = 0
    };

    return result;
  }

  protected virtual AlpsIterationState<TGenotype> ExecuteGeneration(AgedProblem<TGenotype, TSearchSpace, TProblem> problem, AgedSearchSpace<TGenotype, TSearchSpace> searchSpace, AlpsIterationState<TGenotype> previousGenerationState, IRandomNumberGenerator iterationRandom)
  {
    var offspringCount = internalReplacer.GetOffspringCount(PopulationSize);

    var oldPopulation = previousGenerationState.Population[0].ToArray();

    var startSelection = Stopwatch.GetTimestamp();
    var parents = agedSelector.Select(oldPopulation, problem.Objective, offspringCount * 2, iterationRandom, searchSpace, problem);
    var endSelection = Stopwatch.GetTimestamp();
    SelectionMetric += new OperatorMetric(parents.Count, Stopwatch.GetElapsedTime(startSelection, endSelection));

    var parentPairs = new IParents<AgedGenotype<TGenotype>>[offspringCount];
    for (int i = 0, j = 0; i < offspringCount; i++, j += 2) {
      parentPairs[i] = new Parents<AgedGenotype<TGenotype>>(parents[j].Genotype, parents[j + 1].Genotype);
    }

    var startCrossover = Stopwatch.GetTimestamp();
    var crossoverCount = 0;
    var population = agedCrossover.Cross(parentPairs, iterationRandom, searchSpace, problem);
    var endCrossover = Stopwatch.GetTimestamp();
    CrossoverMetric += new OperatorMetric(crossoverCount, Stopwatch.GetElapsedTime(startCrossover, endCrossover));

    var startMutation = Stopwatch.GetTimestamp();
    var mutationCount = 0;
    population = agedMutator.Mutate(population, iterationRandom, searchSpace, problem);
    var endMutation = Stopwatch.GetTimestamp();
    MutationMetric += new OperatorMetric(mutationCount, Stopwatch.GetElapsedTime(startMutation, endMutation));

    var fitnesses = Evaluator.Evaluate(population.Select(x => x.InnerGenotype).ToArray(), iterationRandom, searchSpace.InnerSearchSpace, problem.InnerProblem);

    var evaluatedPopulation = Population.From(population, fitnesses);

    var startReplacement = Stopwatch.GetTimestamp();
    var newPopulation = agedReplacer.Replace(oldPopulation, evaluatedPopulation.ToList(), problem.Objective, iterationRandom, searchSpace, problem);
    var endReplacement = Stopwatch.GetTimestamp();
    ReplacementMetric += new OperatorMetric(1, Stopwatch.GetElapsedTime(startReplacement, endReplacement));

    var result = new AlpsIterationState<TGenotype> {
      Population = [new Population<AgedGenotype<TGenotype>>(new ImmutableList<ISolution<AgedGenotype<TGenotype>>>(newPopulation))],
      //CurrentIteration = previousGenerationState.CurrentIteration + 1
    };

    return result;
  }

  #region Aged Types

  public class AgedCreator(ICreator<TGenotype, TSearchSpace, TProblem> internalCreator) : ICreator<AgedGenotype<TGenotype>, AgedSearchSpace<TGenotype, TSearchSpace>, AgedProblem<TGenotype, TSearchSpace, TProblem>>
  {
    public IReadOnlyList<AgedGenotype<TGenotype>> Create(int count, IRandomNumberGenerator random, AgedSearchSpace<TGenotype, TSearchSpace> searchSpace, AgedProblem<TGenotype, TSearchSpace, TProblem> problem)
    {
      var offspring = new AgedGenotype<TGenotype>[count];
      var genotypes = internalCreator.Create(count, random, searchSpace.InnerSearchSpace, problem.InnerProblem);
      for (var i = 0; i < count; i++) {
        offspring[i] = new AgedGenotype<TGenotype>(genotypes[i], 0);
      }

      return offspring;
    }
  }

  public class AgedMutator(IMutator<TGenotype, TSearchSpace, TProblem> internalMutator) : IMutator<AgedGenotype<TGenotype>, AgedSearchSpace<TGenotype, TSearchSpace>, AgedProblem<TGenotype, TSearchSpace, TProblem>>
  {
    public IReadOnlyList<AgedGenotype<TGenotype>> Mutate(IReadOnlyList<AgedGenotype<TGenotype>> population, IRandomNumberGenerator random, AgedSearchSpace<TGenotype, TSearchSpace> searchSpace, AgedProblem<TGenotype, TSearchSpace, TProblem> problem)
    {
      var innerPopulation = new TGenotype[population.Count];
      for (var i = 0; i < population.Count; i++) {
        innerPopulation[i] = population[i].InnerGenotype;
      }

      var mutated = internalMutator.Mutate(innerPopulation, random, searchSpace.InnerSearchSpace, problem.InnerProblem);
      var result = new AgedGenotype<TGenotype>[mutated.Count];
      for (var i = 0; i < mutated.Count; i++) {
        // Find the original Solution to get the age
        var originalISolution = population.Single(s => Equals(s.InnerGenotype, mutated[i]));
        result[i] = originalISolution;
      }

      return result;
    }
  }

  private class AgedCrossover(ICrossover<TGenotype, TSearchSpace, TProblem> internalCrossover)
    : ICrossover<AgedGenotype<TGenotype>, AgedSearchSpace<TGenotype, TSearchSpace>, AgedProblem<TGenotype, TSearchSpace, TProblem>>
  {
    public IReadOnlyList<AgedGenotype<TGenotype>> Cross(IReadOnlyList<IParents<AgedGenotype<TGenotype>>> parents, IRandomNumberGenerator random, AgedSearchSpace<TGenotype, TSearchSpace> searchSpace, AgedProblem<TGenotype, TSearchSpace, TProblem> problem)
    {
      var innerParents = new IParents<TGenotype>[parents.Count];
      for (var i = 0; i < parents.Count; i++) {
        innerParents[i] = new Parents<TGenotype>(parents[i].Item1.InnerGenotype, parents[i].Item2.InnerGenotype);
      }

      var offspring = internalCrossover.Cross(innerParents, random, searchSpace.InnerSearchSpace, problem.InnerProblem);
      var result = new AgedGenotype<TGenotype>[offspring.Count];
      for (var i = 0; i < offspring.Count; i++) {
        var newAge = Math.Max(parents[i].Item1.Age, parents[i].Item2.Age) + 1;
        result[i] = new AgedGenotype<TGenotype>(offspring[i], newAge);
      }

      return result;
    }
  }

  public class AgedSelector(ISelector<TGenotype, TSearchSpace, TProblem> internalSelector) : ISelector<AgedGenotype<TGenotype>, AgedSearchSpace<TGenotype, TSearchSpace>, AgedProblem<TGenotype, TSearchSpace, TProblem>>
  {
    public IReadOnlyList<ISolution<AgedGenotype<TGenotype>>> Select(IReadOnlyList<ISolution<AgedGenotype<TGenotype>>> population, Objective objective, int count, IRandomNumberGenerator random, AgedSearchSpace<TGenotype, TSearchSpace> searchSpace, AgedProblem<TGenotype, TSearchSpace, TProblem> problem)
    {
      var innerPopulation = new ISolution<TGenotype>[population.Count];
      for (var i = 0; i < population.Count; i++) {
        innerPopulation[i] = new Solution<TGenotype>(population[i].Genotype.InnerGenotype, population[i].ObjectiveVector);
      }

      var selected = internalSelector.Select(innerPopulation, objective, count, random, searchSpace.InnerSearchSpace, problem.InnerProblem);
      var result = new ISolution<AgedGenotype<TGenotype>>[selected.Count];
      for (var i = 0; i < selected.Count; i++) {
        // Find the original Solution to get the age
        var originalISolution = population.Single(s => Equals(s.Genotype.InnerGenotype, selected[i].Genotype));
        result[i] = originalISolution;
      }

      return result;
    }
  }


  public class AgedReplacer(IReplacer<TGenotype, TSearchSpace, TProblem> innerReplacer) : IReplacer<AgedGenotype<TGenotype>, AgedSearchSpace<TGenotype, TSearchSpace>, AgedProblem<TGenotype, TSearchSpace, TProblem>>
  {
    public IReadOnlyList<ISolution<AgedGenotype<TGenotype>>> Replace(IReadOnlyList<ISolution<AgedGenotype<TGenotype>>> previousPopulation, IReadOnlyList<ISolution<AgedGenotype<TGenotype>>> offspringPopulation, Objective objective, IRandomNumberGenerator random, AgedSearchSpace<TGenotype, TSearchSpace> searchSpace, AgedProblem<TGenotype, TSearchSpace, TProblem> problem)
    {
      var innerPreviousPopulation = new ISolution<TGenotype>[previousPopulation.Count];
      for (var i = 0; i < previousPopulation.Count; i++) {
        innerPreviousPopulation[i] = new Solution<TGenotype>(previousPopulation[i].Genotype.InnerGenotype, previousPopulation[i].ObjectiveVector);
      }

      var innerOffspringPopulation = new ISolution<TGenotype>[offspringPopulation.Count];
      for (var i = 0; i < offspringPopulation.Count; i++) {
        innerOffspringPopulation[i] = new Solution<TGenotype>(offspringPopulation[i].Genotype.InnerGenotype, offspringPopulation[i].ObjectiveVector);
      }

      var replaced = innerReplacer.Replace(innerPreviousPopulation, innerOffspringPopulation, objective, random, searchSpace.InnerSearchSpace, problem.InnerProblem);
      var result = new ISolution<AgedGenotype<TGenotype>>[replaced.Count];
      for (var i = 0; i < replaced.Count; i++) {
        // Find the original Solution to get the age
        var originalISolution = previousPopulation.Concat(offspringPopulation).Single(s => Equals(s.Genotype.InnerGenotype, replaced[i].Genotype));
        result[i] = originalISolution;
      }

      return result;
    }

    public int GetOffspringCount(int populationSize) => innerReplacer.GetOffspringCount(populationSize);
  }

  #endregion
}

public class AlpsGeneticAlgorithm<TGenotype, TSearchSpace>(
  int populationSize,
  ICreator<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>> creator,
  ICrossover<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>> crossover,
  IMutator<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>> mutator,
  double mutationRate,
  ISelector<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>> selector,
  IEvaluator<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>> evaluator,
  int elites,
  ITerminator<TGenotype, AlpsIterationState<TGenotype>, TSearchSpace, IProblem<TGenotype, TSearchSpace>> terminator,
  IInterceptor<TGenotype, AlpsIterationState<TGenotype>, TSearchSpace, IProblem<TGenotype, TSearchSpace>>? interceptor = null)
  : AlpsGeneticAlgorithm<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>>(populationSize, creator, crossover, mutator, mutationRate, selector, evaluator, elites, terminator, interceptor)
  where TSearchSpace : class, ISearchSpace<TGenotype> where TGenotype : class;

public class AlpsGeneticAlgorithm<TGenotype>(
  int populationSize,
  ICreator<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>> creator,
  ICrossover<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>> crossover,
  IMutator<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>> mutator,
  double mutationRate,
  ISelector<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>> selector,
  IEvaluator<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>> evaluator,
  int elites,
  ITerminator<TGenotype, AlpsIterationState<TGenotype>, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>> terminator,
  IInterceptor<TGenotype, AlpsIterationState<TGenotype>, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>? interceptor = null)
  : AlpsGeneticAlgorithm<TGenotype, ISearchSpace<TGenotype>>(populationSize, creator, crossover, mutator, mutationRate, selector, evaluator, elites, terminator, interceptor) where TGenotype : class;
