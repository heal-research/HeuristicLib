using HEAL.HeuristicLib.Collections;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Operators.Replacers;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms.Evolutionary;


public record AlpsState<TGenotype> : AlgorithmState
{
  public required IReadOnlyList<Population<TGenotype>> Population { get; init; }
  public required IReadOnlyList<IReadOnlyList<int>> Ages { get; init; }
}

public class AlpsGeneticAlgorithm<TGenotype, TSearchSpace, TProblem>
  : IterativeAlgorithm<TGenotype, TSearchSpace, TProblem, AlpsState<TGenotype>>
  where TGenotype : class
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  public required int PopulationSize { get; init; }
  public required ICreator<TGenotype, TSearchSpace, TProblem> Creator { get; init; }
  public required ICrossover<TGenotype, TSearchSpace, TProblem> Crossover { get; init; }
  public required IMutator<TGenotype, TSearchSpace, TProblem> Mutator { get; init; }
  public required double MutationRate { get; init; }
  public required ISelector<TGenotype, TSearchSpace, TProblem> Selector { get; init; }

  public int Elites { get => internalReplacer.Elites; init => internalReplacer = new ElitismReplacer<TGenotype>(value); }
  // public IReplacer<TGenotype, TSearchSpace, TProblem> Replacer { get; }

  // ToDo: this is not yet correctly set.
  private readonly MultiMutator<TGenotype, TSearchSpace, TProblem> internalMutator = new([]); 
  private readonly ElitismReplacer<TGenotype> internalReplacer = new(1);

  public override AlpsGeneticAlgorithmInstance<TGenotype, TSearchSpace, TProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry)
  {
    var creatorInstance = instanceRegistry.GetOrCreate(Creator);
    var evaluatorInstance = instanceRegistry.GetOrCreate(Evaluator);
    
    return new AlpsGeneticAlgorithmInstance<TGenotype, TSearchSpace, TProblem>(
      Interceptor,
      evaluatorInstance,
      PopulationSize,
      creatorInstance,
      Crossover,
      internalMutator,
      Selector,
      internalReplacer
    );
  }

  //internalMutator = new MultiMutator<TGenotype, TSearchSpace, TProblem>([mutator, new NoChangeMutator<TGenotype>()], [mutationRate, 1 - mutationRate]);
  //  internalReplacer = new ElitismReplacer<TGenotype>(elites);
 
}

public class AlpsGeneticAlgorithmInstance<TGenotype, TSearchSpace, TProblem>
  : IterativeAlgorithmInstance<TGenotype, TSearchSpace, TProblem, AlpsState<TGenotype>>
  where TGenotype : class
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  private readonly int PopulationSize;
  private readonly ICreatorInstance<TGenotype, TSearchSpace, TProblem> agedCreator;
  private readonly ICrossover<TGenotype, TSearchSpace, TProblem> agedCrossover;
  private readonly IMutator<TGenotype, TSearchSpace, TProblem> agedMutator;
  private readonly ISelector<TGenotype, TSearchSpace, TProblem> agedSelector;
  private readonly IReplacer<TGenotype, TSearchSpace, TProblem> agedReplacer;

  public AlpsGeneticAlgorithmInstance(IInterceptor<TGenotype, AlpsState<TGenotype>, TSearchSpace, TProblem>? interceptor, IEvaluatorInstance<TGenotype, TSearchSpace, TProblem> evaluator, int populationSize, ICreatorInstance<TGenotype, TSearchSpace, TProblem> agedCreator, ICrossover<TGenotype, TSearchSpace, TProblem> agedCrossover, IMutator<TGenotype, TSearchSpace, TProblem> agedMutator, ISelector<TGenotype, TSearchSpace, TProblem> agedSelector, IReplacer<TGenotype, TSearchSpace, TProblem> agedReplacer) 
    : base(interceptor, evaluator)
  {
    PopulationSize = populationSize;
    this.agedCreator = agedCreator;
    this.agedCrossover = agedCrossover;
    this.agedMutator = agedMutator;
    this.agedSelector = agedSelector;
    this.agedReplacer = agedReplacer;
  }

  public override AlpsState<TGenotype> ExecuteStep(AlpsState<TGenotype>? previousState, TProblem problem, IRandomNumberGenerator random)
  {
    var agedProblem = problem;
    var agedSearchSpace = problem.SearchSpace;

    if (previousState is null) {
      //var iteration = previousState?.CurrentIteration + 1 ?? 0;

      var initialLayerPopulation = agedCreator.Create(PopulationSize, random, agedSearchSpace, agedProblem);
      
      var initialFitnesses = Evaluator.Evaluate(initialLayerPopulation, random, agedSearchSpace, agedProblem);

      return new AlpsState<TGenotype> {
        Population = [Population.From(initialLayerPopulation, initialFitnesses)],
        Ages = [Enumerable.Repeat(0, PopulationSize).ToArray()],
        //CurrentIteration = 0
      };
    }
    
    var offspringCount = agedReplacer.GetOffspringCount(PopulationSize);

    // ToDo: implement actual ALPS logic with layers
    
    var oldPopulation = previousState.Population[0].ToArray();

    var parents = agedSelector.Select(oldPopulation, problem.Objective, offspringCount * 2, random, agedSearchSpace, agedProblem);

    var parentPairs = new IParents<TGenotype>[offspringCount];
    var offspringAges = new int[offspringCount];
    for (int i = 0, j = 0; i < offspringCount; i++, j += 2) {
      parentPairs[i] = new Parents<TGenotype>(parents[j].Genotype, parents[j + 1].Genotype);
      offspringAges[i] = Math.Max(previousState.Ages[0][j], previousState.Ages[0][j + 1]) + 1;
    }

    var population = agedCrossover.Cross(parentPairs, random, agedSearchSpace, agedProblem);

    population = agedMutator.Mutate(population, random, agedSearchSpace, agedProblem);

    var fitnesses = Evaluator.Evaluate(population, random, agedSearchSpace, agedProblem);

    var evaluatedPopulation = Population.From(population, fitnesses);

    var newPopulation = agedReplacer.Replace(oldPopulation, evaluatedPopulation.ToList(), problem.Objective, random, agedSearchSpace, agedProblem);

    var result = new AlpsState<TGenotype> {
      Population = [new Population<TGenotype>(new ImmutableList<ISolution<TGenotype>>(newPopulation))],
      Ages = [offspringAges], // ToDo: ERROR here, since the replacer might shuffled the population and keeps some of the old solutions, so we need to track the ages through the replacer as well
      //CurrentIteration = previousGenerationState.CurrentIteration + 1
    };

    return result;
  }

  // #region Aged Types
  //
  // // ToDo: probably remove all the Aged adapters and instead hold solutions and ages separated and only use a separate age-calculation scheme
  // public class AgedCreator(ICreator<TGenotype, TSearchSpace, TProblem> internalCreator) : ICreator<AgedGenotype<TGenotype>, AgedSearchSpace<TGenotype, TSearchSpace>, AgedProblem<TGenotype, TSearchSpace, TProblem>>
  // {
  //   public IReadOnlyList<AgedGenotype<TGenotype>> Create(int count, IRandomNumberGenerator random, AgedSearchSpace<TGenotype, TSearchSpace> searchSpace, AgedProblem<TGenotype, TSearchSpace, TProblem> problem)
  //   {
  //     var offspring = new AgedGenotype<TGenotype>[count];
  //     var genotypes = internalCreator.Create(count, random, searchSpace.InnerSearchSpace, problem.InnerProblem);
  //     for (var i = 0; i < count; i++) {
  //       offspring[i] = new AgedGenotype<TGenotype>(genotypes[i], 0);
  //     }
  //
  //     return offspring;
  //   }
  // }
  //
  // public class AgedMutator(IMutator<TGenotype, TSearchSpace, TProblem> internalMutator) : Mutator<AgedGenotype<TGenotype>, AgedSearchSpace<TGenotype, TSearchSpace>, AgedProblem<TGenotype, TSearchSpace, TProblem>>
  // {
  //   public override IReadOnlyList<AgedGenotype<TGenotype>> Mutate(IReadOnlyList<AgedGenotype<TGenotype>> population, IRandomNumberGenerator random, AgedSearchSpace<TGenotype, TSearchSpace> searchSpace, AgedProblem<TGenotype, TSearchSpace, TProblem> problem)
  //   {
  //     var innerPopulation = new TGenotype[population.Count];
  //     for (var i = 0; i < population.Count; i++) {
  //       innerPopulation[i] = population[i].InnerGenotype;
  //     }
  //
  //     var mutated = internalMutator.Mutate(innerPopulation, random, searchSpace.InnerSearchSpace, problem.InnerProblem);
  //     var result = new AgedGenotype<TGenotype>[mutated.Count];
  //     for (var i = 0; i < mutated.Count; i++) {
  //       // Find the original Solution to get the age
  //       var originalISolution = population.Single(s => Equals(s.InnerGenotype, mutated[i]));
  //       result[i] = originalISolution;
  //     }
  //
  //     return result;
  //   }
  // }
  //
  // private class AgedCrossover(ICrossover<TGenotype, TSearchSpace, TProblem> internalCrossover)
  //   : Crossover<AgedGenotype<TGenotype>, AgedSearchSpace<TGenotype, TSearchSpace>, AgedProblem<TGenotype, TSearchSpace, TProblem>>
  // {
  //   public override IReadOnlyList<AgedGenotype<TGenotype>> Cross(IReadOnlyList<IParents<AgedGenotype<TGenotype>>> parents, IRandomNumberGenerator random, AgedSearchSpace<TGenotype, TSearchSpace> searchSpace, AgedProblem<TGenotype, TSearchSpace, TProblem> problem)
  //   {
  //     var innerParents = new IParents<TGenotype>[parents.Count];
  //     for (var i = 0; i < parents.Count; i++) {
  //       innerParents[i] = new Parents<TGenotype>(parents[i].Item1.InnerGenotype, parents[i].Item2.InnerGenotype);
  //     }
  //
  //     var offspring = internalCrossover.Cross(innerParents, random, searchSpace.InnerSearchSpace, problem.InnerProblem);
  //     var result = new AgedGenotype<TGenotype>[offspring.Count];
  //     for (var i = 0; i < offspring.Count; i++) {
  //       var newAge = Math.Max(parents[i].Item1.Age, parents[i].Item2.Age) + 1;
  //       result[i] = new AgedGenotype<TGenotype>(offspring[i], newAge);
  //     }
  //
  //     return result;
  //   }
  // }
  //
  // public class AgedSelector(ISelector<TGenotype, TSearchSpace, TProblem> internalSelector) : Selector<AgedGenotype<TGenotype>, AgedSearchSpace<TGenotype, TSearchSpace>, AgedProblem<TGenotype, TSearchSpace, TProblem>>
  // {
  //   public override IReadOnlyList<ISolution<AgedGenotype<TGenotype>>> Select(IReadOnlyList<ISolution<AgedGenotype<TGenotype>>> population, Objective objective, int count, IRandomNumberGenerator random, AgedSearchSpace<TGenotype, TSearchSpace> searchSpace, AgedProblem<TGenotype, TSearchSpace, TProblem> problem)
  //   {
  //     var innerPopulation = new ISolution<TGenotype>[population.Count];
  //     for (var i = 0; i < population.Count; i++) {
  //       innerPopulation[i] = new Solution<TGenotype>(population[i].Genotype.InnerGenotype, population[i].ObjectiveVector);
  //     }
  //
  //     var selected = internalSelector.Select(innerPopulation, objective, count, random, searchSpace.InnerSearchSpace, problem.InnerProblem);
  //     var result = new ISolution<AgedGenotype<TGenotype>>[selected.Count];
  //     for (var i = 0; i < selected.Count; i++) {
  //       // Find the original Solution to get the age
  //       var originalISolution = population.Single(s => Equals(s.Genotype.InnerGenotype, selected[i].Genotype));
  //       result[i] = originalISolution;
  //     }
  //
  //     return result;
  //   }
  // }
  //
  //
  // public class AgedReplacer(IReplacer<TGenotype, TSearchSpace, TProblem> innerReplacer) : Replacer<AgedGenotype<TGenotype>, AgedSearchSpace<TGenotype, TSearchSpace>, AgedProblem<TGenotype, TSearchSpace, TProblem>>
  // {
  //   public  override IReadOnlyList<ISolution<AgedGenotype<TGenotype>>> Replace(IReadOnlyList<ISolution<AgedGenotype<TGenotype>>> previousPopulation, IReadOnlyList<ISolution<AgedGenotype<TGenotype>>> offspringPopulation, Objective objective, IRandomNumberGenerator random, AgedSearchSpace<TGenotype, TSearchSpace> searchSpace, AgedProblem<TGenotype, TSearchSpace, TProblem> problem)
  //   {
  //     var innerPreviousPopulation = new ISolution<TGenotype>[previousPopulation.Count];
  //     for (var i = 0; i < previousPopulation.Count; i++) {
  //       innerPreviousPopulation[i] = new Solution<TGenotype>(previousPopulation[i].Genotype.InnerGenotype, previousPopulation[i].ObjectiveVector);
  //     }
  //
  //     var innerOffspringPopulation = new ISolution<TGenotype>[offspringPopulation.Count];
  //     for (var i = 0; i < offspringPopulation.Count; i++) {
  //       innerOffspringPopulation[i] = new Solution<TGenotype>(offspringPopulation[i].Genotype.InnerGenotype, offspringPopulation[i].ObjectiveVector);
  //     }
  //
  //     var replaced = innerReplacer.Replace(innerPreviousPopulation, innerOffspringPopulation, objective, random, searchSpace.InnerSearchSpace, problem.InnerProblem);
  //     var result = new ISolution<AgedGenotype<TGenotype>>[replaced.Count];
  //     for (var i = 0; i < replaced.Count; i++) {
  //       // Find the original Solution to get the age
  //       var originalISolution = previousPopulation.Concat(offspringPopulation).Single(s => Equals(s.Genotype.InnerGenotype, replaced[i].Genotype));
  //       result[i] = originalISolution;
  //     }
  //
  //     return result;
  //   }
  //
  //   public override int GetOffspringCount(int populationSize) => innerReplacer.GetOffspringCount(populationSize);
  // }
  //
  // #endregion
}

// public class AlpsGeneticAlgorithm<TGenotype, TSearchSpace>(
//   int populationSize,
//   ICreator<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>> creator,
//   ICrossover<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>> crossover,
//   IMutator<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>> mutator,
//   double mutationRate,
//   ISelector<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>> selector,
//   IEvaluator<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>> evaluator,
//   int elites,
//   ITerminator<TGenotype, AlpsState<TGenotype>, TSearchSpace, IProblem<TGenotype, TSearchSpace>> terminator,
//   IInterceptor<TGenotype, AlpsState<TGenotype>, TSearchSpace, IProblem<TGenotype, TSearchSpace>>? interceptor = null)
//   : AlpsGeneticAlgorithm<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>>(populationSize, creator, crossover, mutator, mutationRate, selector, evaluator, elites, terminator, interceptor)
//   where TSearchSpace : class, ISearchSpace<TGenotype> where TGenotype : class;
//
// public class AlpsGeneticAlgorithm<TGenotype>(
//   int populationSize,
//   ICreator<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>> creator,
//   ICrossover<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>> crossover,
//   IMutator<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>> mutator,
//   double mutationRate,
//   ISelector<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>> selector,
//   IEvaluator<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>> evaluator,
//   int elites,
//   ITerminator<TGenotype, AlpsState<TGenotype>, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>> terminator,
//   IInterceptor<TGenotype, AlpsState<TGenotype>, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>? interceptor = null)
//   : AlpsGeneticAlgorithm<TGenotype, ISearchSpace<TGenotype>>(populationSize, creator, crossover, mutator, mutationRate, selector, evaluator, elites, terminator, interceptor) where TGenotype : class;
