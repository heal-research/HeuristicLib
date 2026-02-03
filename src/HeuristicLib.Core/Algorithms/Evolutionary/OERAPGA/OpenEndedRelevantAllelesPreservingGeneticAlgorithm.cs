using HEAL.HeuristicLib.Operators.Crossovers;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Operators.Prototypes;
using HEAL.HeuristicLib.Operators.Replacers;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Algorithms.Evolutionary.OERAPGA;

public class OpenEndedRelevantAllelesPreservingGeneticAlgorithm<TGenotype, TSearchSpace, TProblem>
  : IterativeAlgorithm<TGenotype, TSearchSpace, TProblem, PopulationAlgorithmState<TGenotype>>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TGenotype : class
{
  private double Strictness { get; } = 1.0;
  public required int PopulationSize { get; init; }
  public required ICrossover<TGenotype, TSearchSpace, TProblem> Crossover { get; init; }
  public required IMutator<TGenotype, TSearchSpace, TProblem> Mutator { get; init; }
  public required ISelector<TGenotype, TSearchSpace, TProblem> Selector { get; init; }
  public int Elites { get; init; } = 1;

  public required int MaxEffort { get; init; }

  public override PopulationAlgorithmState<TGenotype> ExecuteStep(TProblem problem, TSearchSpace searchSpace, PopulationAlgorithmState<TGenotype>? previousIterationResult, IRandomNumberGenerator random)
  {
    if (previousIterationResult == null) {
      return new PopulationAlgorithmState<TGenotype>(CreateInitialPopulation(problem, searchSpace, random, PopulationSize));
    }

    var oldPopulation = previousIterationResult.Population.Solutions;

    IReadOnlyList<ISolution<TGenotype>> newPop;
    if (oldPopulation.Count <= 0) {
      newPop = CreateInitialPopulation(problem, searchSpace, random, PopulationSize).Solutions;
    } else {
      var selected = Selector.Select(oldPopulation, problem.Objective, MaxEffort * 2, random, searchSpace, problem);
      var population = Crossover.Cross(selected.ToGenotypePairs(), random, searchSpace, problem);
      population = Mutator.Mutate(population, random, searchSpace, problem);
      var fitnesses = Evaluator.Evaluate(population, random, searchSpace, problem);

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

    return new PopulationAlgorithmState<TGenotype>(newPopulation);
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

  public class Builder : PopulationBasedAlgorithmBuilder<
      TGenotype,
      TSearchSpace,
      TProblem,
      PopulationAlgorithmState<TGenotype>,
      OpenEndedRelevantAllelesPreservingGeneticAlgorithm<TGenotype, TSearchSpace, TProblem>>,
    IMutatorPrototype<TGenotype, TSearchSpace, TProblem>,
    ICrossoverPrototype<TGenotype, TSearchSpace, TProblem>
  {
    public double MutationRate { get; set; } = 0.05;
    public int Elites { get; set; } = 1;
    public required int MaxEffort { get; set; }
    public required ICrossover<TGenotype, TSearchSpace, TProblem> Crossover { get; set; }
    public required IMutator<TGenotype, TSearchSpace, TProblem> Mutator { get; set; }

    public OpenEndedRelevantAllelesPreservingGeneticAlgorithm<TGenotype, TSearchSpace, TProblem> Create()
    {
      return new OpenEndedRelevantAllelesPreservingGeneticAlgorithm<TGenotype, TSearchSpace, TProblem> {
        AlgorithmRandom = SystemRandomNumberGenerator.Default(RandomSeed),
        PopulationSize = PopulationSize,
        Creator = Creator,
        Crossover = Crossover,
        Selector = Selector,
        Evaluator = Evaluator,
        Elites = Elites,
        Terminator = Terminator,
        Interceptor = Interceptor,
        Mutator = Mutator.WithRate(MutationRate),
        MaxEffort = MaxEffort
      };
    }

    public override OpenEndedRelevantAllelesPreservingGeneticAlgorithm<TGenotype, TSearchSpace, TProblem> BuildAlgorithm() => Create();
  }
}
