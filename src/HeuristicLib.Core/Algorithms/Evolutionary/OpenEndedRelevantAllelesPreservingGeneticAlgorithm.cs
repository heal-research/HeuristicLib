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

public class OpenEndedRelevantAllelesPreservingGeneticAlgorithm<TGenotype, TSearchSpace, TProblem>
  : IterativeAlgorithm<TGenotype, TSearchSpace, TProblem, PopulationState<TGenotype>>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TGenotype : class
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
    var creatorInstance = instanceRegistry.GetOrAdd(Creator, () => Creator.CreateExecutionInstance(instanceRegistry));
    var evaluatorInstance = instanceRegistry.GetOrAdd(Evaluator, () => Evaluator.CreateExecutionInstance(instanceRegistry));
    
    return new OpenEndedRelevantAllelesPreservingGeneticAlgorithmInstance<TGenotype, TSearchSpace, TProblem>(
      Interceptor,
      evaluatorInstance,
      PopulationSize,
      creatorInstance,
      Crossover,
      Mutator,
      Selector,
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
  where TGenotype : class
{
  protected readonly int PopulationSize;
  protected readonly ICreatorInstance<TGenotype, TSearchSpace, TProblem> Creator;
  protected readonly ICrossover<TGenotype, TSearchSpace, TProblem> Crossover;
  protected readonly IMutator<TGenotype, TSearchSpace, TProblem> Mutator;
  protected readonly ISelector<TGenotype, TSearchSpace, TProblem> Selector;
  protected readonly int Elites;
  protected readonly int MaxEffort;
  protected readonly double Strictness = 1.0;


  public OpenEndedRelevantAllelesPreservingGeneticAlgorithmInstance(IInterceptor<TGenotype, PopulationState<TGenotype>, TSearchSpace, TProblem>? interceptor, IEvaluatorInstance<TGenotype, TSearchSpace, TProblem> evaluator, int populationSize, ICreatorInstance<TGenotype, TSearchSpace, TProblem> creator, ICrossover<TGenotype, TSearchSpace, TProblem> crossover, IMutator<TGenotype, TSearchSpace, TProblem> mutator, ISelector<TGenotype, TSearchSpace, TProblem> selector, int elites, int maxEffort, double strictness) 
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
      //CurrentIteration = 0
    };
  }

  public override PopulationState<TGenotype> ExecuteStep(PopulationState<TGenotype>? previousState, TProblem problem, IRandomNumberGenerator random)
  {
    if (previousState is null) {
      return CreateInitialPopulation(problem, random);
    }

    var oldPopulation = previousState.Population.Solutions;

    IReadOnlyList<ISolution<TGenotype>> newPop;
    if (oldPopulation.Count <= 0) {
      newPop = CreateInitialPopulation(problem, random).Population.Solutions;
    } else {
      var selected = Selector.Select(oldPopulation, problem.Objective, MaxEffort * 2, random, problem.SearchSpace, problem);
      var population = Crossover.Cross(selected.ToGenotypePairs(), random, problem.SearchSpace, problem);
      population = Mutator.Mutate(population, random, problem.SearchSpace, problem);
      var fitnesses = Evaluator.Evaluate(population, random, problem.SearchSpace, problem);

      //Offspring Selection
      newPop = Population.From(population, fitnesses)
                         .Solutions
                         .Zip(selected.ToSolutionPairs())
                         .Where(f =>
                           f.Item1.ObjectiveVector.Dominates(Combine(f.Item2, problem.Objective, Strictness), problem.Objective))
                         .Select(f => f.Item1)
                         .ToArray();
    }

    var r = new ElitismReplacer<TGenotype>(Elites, Elites + newPop.Count);
    var newPopulation = r.Replace(oldPopulation, newPop, problem.Objective, random);

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

  //AlgorithmBuilder<TG, TS, TP, PopulationState<TG>, GeneticAlgorithm<TG, TS, TP>, GeneticAlgorithmBuildSpec<TG, TS, TP>>
  public record Builder : AlgorithmBuilder<
    TGenotype,
    TSearchSpace,
    TProblem,
    PopulationState<TGenotype>,
    OpenEndedRelevantAllelesPreservingGeneticAlgorithm<TGenotype, TSearchSpace, TProblem>,
    OERAPGABuildSpec<TGenotype, TSearchSpace, TProblem>>
  {
    public double MutationRate { get; set; } = 0.05;
    public int Elites { get; set; } = 1;
    public required int MaxEffort { get; set; }
    public required ICreator<TGenotype, TSearchSpace, TProblem> Creator { get; set; }
    public required ICrossover<TGenotype, TSearchSpace, TProblem> Crossover { get; set; }
    public required IMutator<TGenotype, TSearchSpace, TProblem> Mutator { get; set; }
    public int PopulationSize { get; set; } = 100;
    public ISelector<TGenotype, TSearchSpace, TProblem> Selector { get; set; } = new TournamentSelector<TGenotype>(2);

    public override OERAPGABuildSpec<TGenotype, TSearchSpace, TProblem> CreateBuildSpec() => new(
      Evaluator, Terminator, Interceptor, PopulationSize, Selector, Creator, Crossover, Mutator, MutationRate, Elites, MaxEffort
    );

    public override OpenEndedRelevantAllelesPreservingGeneticAlgorithm<TGenotype, TSearchSpace, TProblem> BuildFromSpec(OERAPGABuildSpec<TGenotype, TSearchSpace, TProblem> spec) =>
      new OpenEndedRelevantAllelesPreservingGeneticAlgorithm<TGenotype, TSearchSpace, TProblem>() {
        // ToDo: how to prevent accidentally reading from the builder instead of the spec here?
        PopulationSize = spec.PopulationSize,
        Creator = spec.Creator,
        Crossover = spec.Crossover,
        Selector = spec.Selector,
        Evaluator = spec.Evaluator,
        //Replacer = new ElitismReplacer<TGenotype>(spec.Elites),
        //Terminator = spec.Terminator,
        Interceptor = spec.Interceptor,
        Mutator = spec.Mutator.WithRate(spec.MutationRate),
        MaxEffort = spec.MaxEffort
      };
  }
}

public sealed record OERAPGABuildSpec<TG, TS, TP>
  : AlgorithmBuildSpec<TG, TS, TP, PopulationState<TG>>,
    ISpecWithCreator<TG, TS, TP>,
    ISpecWithSelector<TG, TS, TP>,
    ISpecWithCrossover<TG, TS, TP>,
    ISpecWithMutator<TG, TS, TP>
  where TG : class
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
  public int PopulationSize { get; set; }
  public ISelector<TG, TS, TP> Selector { get; set; }
  public ICreator<TG, TS, TP> Creator { get; set; }
  public ICrossover<TG, TS, TP> Crossover { get; set; }
  public IMutator<TG, TS, TP> Mutator { get; set; }
  public double MutationRate { get; set; }
  public int Elites { get; set; }
  public int MaxEffort { get; set; }

  public OERAPGABuildSpec(IEvaluator<TG, TS, TP> Evaluator,
                          ITerminator<TG, PopulationState<TG>, TS, TP> Terminator,
                          IInterceptor<TG, PopulationState<TG>, TS, TP>? Interceptor,
                          int PopulationSize,
                          ISelector<TG, TS, TP> Selector,
                          ICreator<TG, TS, TP> Creator,
                          ICrossover<TG, TS, TP> Crossover,
                          IMutator<TG, TS, TP> Mutator,
                          double MutationRate,
                          int Elites,
                          int MaxEffort)
    : base(Evaluator, Terminator, Interceptor)
  {
    this.PopulationSize = PopulationSize;
    this.Selector = Selector;
    this.Creator = Creator;
    this.Crossover = Crossover;
    this.Mutator = Mutator;
    this.MutationRate = MutationRate;
    this.Elites = Elites;
    this.MaxEffort = MaxEffort;
  }
}

// public static class OpenEndedRelevantAllelesPreservingGeneticAlgorithm
// {
//   public static OpenEndedRelevantAllelesPreservingGeneticAlgorithm<TGenotype, TSearchSpace, TProblem>.Builder GetBuilder<TGenotype, TSearchSpace, TProblem>(
//     ICreator<TGenotype, TSearchSpace, TProblem> creator,
//     ICrossover<TGenotype, TSearchSpace, TProblem> crossover,
//     IMutator<TGenotype, TSearchSpace, TProblem> mutator)
//     where TSearchSpace : class, ISearchSpace<TGenotype> where TProblem : class, IProblem<TGenotype, TSearchSpace> where TGenotype : class => new() {
//     Mutator = mutator,
//     Crossover = crossover,
//     Creator = creator,
//     MaxEffort = 200
//   };
// }
