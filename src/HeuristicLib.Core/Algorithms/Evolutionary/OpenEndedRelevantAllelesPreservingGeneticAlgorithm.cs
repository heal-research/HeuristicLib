using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Operators.Replacers;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms.Evolutionary;

public record OpenEndedRelevantAllelesPreservingGeneticAlgorithm<TGenotype, TSearchSpace, TProblem>
  : IterativeAlgorithm<TGenotype, TSearchSpace, TProblem, PopulationState<TGenotype>>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  private double Strictness { get; } = 1.0;
  public required int PopulationSize { get; init; }
  public required ICreator<TGenotype, TSearchSpace, TProblem> Creator { get; init; }
  public required ICrossover<TGenotype, TSearchSpace, TProblem> Crossover { get; init; }
  public required IMutator<TGenotype, TSearchSpace, TProblem> Mutator { get; init; }
  public required ISelector<TGenotype, TSearchSpace, TProblem> Selector { get; init; }
  public int Elites { get; init; } = 1;

  public required int MaxEffort { get; init; }

  public override OpenEndedRelevantAllelesPreservingGeneticAlgorithmInstance<TGenotype, TSearchSpace, TProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry)
  {
    var interceptorInstance = Interceptor is not null ? instanceRegistry.Resolve(Interceptor) : null;
    var evaluatorInstance = instanceRegistry.Resolve(Evaluator);
    var creatorInstance = instanceRegistry.Resolve(Creator);
    var crossoverInstance = instanceRegistry.Resolve(Crossover);
    var mutatorInstance = instanceRegistry.Resolve(Mutator);
    var selectorInstance = instanceRegistry.Resolve(Selector);

    return new OpenEndedRelevantAllelesPreservingGeneticAlgorithmInstance<TGenotype, TSearchSpace, TProblem>(
      interceptorInstance,
      evaluatorInstance,
      PopulationSize,
      creatorInstance,
      crossoverInstance,
      mutatorInstance,
      selectorInstance,
      Elites,
      MaxEffort,
      Strictness
    );
  }
}

public class OpenEndedRelevantAllelesPreservingGeneticAlgorithmInstance<TGenotype, TSearchSpace, TProblem>
  : IterativeAlgorithmInstance<TGenotype, TSearchSpace, TProblem, PopulationState<TGenotype>>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  protected readonly int PopulationSize;
  protected readonly ICreatorInstance<TGenotype, TSearchSpace, TProblem> Creator;
  protected readonly ICrossoverInstance<TGenotype, TSearchSpace, TProblem> Crossover;
  protected readonly IMutatorInstance<TGenotype, TSearchSpace, TProblem> Mutator;
  protected readonly ISelectorInstance<TGenotype, TSearchSpace, TProblem> Selector;
  protected readonly int Elites;
  protected readonly int MaxEffort;
  protected readonly double Strictness;

  public OpenEndedRelevantAllelesPreservingGeneticAlgorithmInstance(IInterceptorInstance<TGenotype, TSearchSpace, TProblem, PopulationState<TGenotype>>? interceptor, IEvaluatorInstance<TGenotype, TSearchSpace, TProblem> evaluator, int populationSize, ICreatorInstance<TGenotype, TSearchSpace, TProblem> creator, ICrossoverInstance<TGenotype, TSearchSpace, TProblem> crossover, IMutatorInstance<TGenotype, TSearchSpace, TProblem> mutator, ISelectorInstance<TGenotype, TSearchSpace, TProblem> selector, int elites, int maxEffort, double strictness)
    : base(interceptor, evaluator)
  {
    PopulationSize = populationSize;
    Creator = creator;
    Crossover = crossover;
    Mutator = mutator;
    Selector = selector;
    Elites = elites;
    MaxEffort = maxEffort;
    Strictness = strictness;
  }

  private PopulationState<TGenotype> CreateInitialPopulation(TProblem problem, IRandomNumberGenerator random)
  {
    var initialSolutions = Creator.Create(PopulationSize, random, problem.SearchSpace, problem);
    var initialFitnesses = Evaluator.Evaluate(initialSolutions, random, problem.SearchSpace, problem);
    return new PopulationState<TGenotype> {
      Population = Population.From(initialSolutions, initialFitnesses),
      // CurrentIteration = 0
    };
  }

  public override PopulationState<TGenotype> ExecuteStep(PopulationState<TGenotype>? previousState, TProblem problem, IRandomNumberGenerator random)
  {
    if (previousState is null) {
      return CreateInitialPopulation(problem, random);
    }

    var oldPopulation = previousState.Population.Solutions;

    IReadOnlyList<ISolution<TGenotype>> newPop;
    if (oldPopulation.Length <= 0) {
      newPop = CreateInitialPopulation(problem, random).Population.Solutions;
    } else {
      var selected = Selector.Select(oldPopulation, problem.Objective, MaxEffort * 2, random, problem.SearchSpace, problem);
      var population = Crossover.Cross(selected.ToParents(problem.Objective), random, problem.SearchSpace, problem);
      population = Mutator.Mutate(population, random, problem.SearchSpace, problem);
      var fitnesses = Evaluator.Evaluate(population, random, problem.SearchSpace, problem);

      // Offspring Selection
      newPop = Population
               .From(population, fitnesses)
               .Solutions
               .Zip(selected.ToSolutionPairs())
               .Where(f =>
                 f.Item1.ObjectiveVector.Dominates(Combine(f.Item2, problem.Objective, Strictness), problem.Objective))
               .Select(f => f.Item1)
               .ToArray();
    }

    var r = new ElitismReplacer<TGenotype>(Elites);
    var targetPopsize = Elites + newPop.Count;
    var newPopulation = r.Replace(oldPopulation, newPop, problem.Objective, targetPopsize, random);

    return new PopulationState<TGenotype> { Population = Population.From(newPopulation) };
  }

  private static ObjectiveVector Combine((ISolution<TGenotype>, ISolution<TGenotype>) parents, Objective problemObjective, double strictness = 1.0)
  {
    var o1 = parents.Item1.ObjectiveVector;
    var o2 = parents.Item2.ObjectiveVector;
    if (o2.Dominates(o1, problemObjective)) {
      (o1, o2) = (o2, o1);
    }

    return strictness switch {
      >= 1.0 => o1,
      <= 0.0 => o2,
      _ => new ObjectiveVector(o1.Zip(o2).Select(pair => pair.Item1 * strictness + pair.Item2 * (1.0 - strictness)).ToArray())
    };
  }
}

// ReSharper disable once IdentifierTypo
public record OerapgaBuildBuilder<TG, TS, TP>
  : AlgorithmBuilder<TG, TS, TP, PopulationState<TG>, OpenEndedRelevantAllelesPreservingGeneticAlgorithm<TG, TS, TP>>,
    IBuilderWithCreator<TG, TS, TP>,
    IBuilderWithSelector<TG, TS, TP>,
    IBuilderWithCrossover<TG, TS, TP>,
    IBuilderWithMutator<TG, TS, TP>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
  public double MutationRate { get; set; } = 0.05;
  public int Elites { get; set; } = 1;
  public required int MaxEffort { get; set; }
  public required ICreator<TG, TS, TP> Creator { get; set; }
  public required ICrossover<TG, TS, TP> Crossover { get; set; }
  public required IMutator<TG, TS, TP> Mutator { get; set; }
  public int PopulationSize { get; set; } = 100;
  public ISelector<TG, TS, TP> Selector { get; set; } = new TournamentSelector<TG>(2);

  public override OpenEndedRelevantAllelesPreservingGeneticAlgorithm<TG, TS, TP> Build()
  {
    return new OpenEndedRelevantAllelesPreservingGeneticAlgorithm<TG, TS, TP>() {
      PopulationSize = PopulationSize,
      Creator = Creator,
      Crossover = Crossover,
      Selector = Selector,
      Evaluator = Evaluator,
      Interceptor = Interceptor,
      Mutator = Mutator.WithRate(MutationRate),
      MaxEffort = MaxEffort
    };
  }
}
