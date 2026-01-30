using HEAL.HeuristicLib.Operators.Crossover;
using HEAL.HeuristicLib.Operators.Mutator;
using HEAL.HeuristicLib.Operators.Prototypes;
using HEAL.HeuristicLib.Operators.Replacer;
using HEAL.HeuristicLib.Operators.Selector;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Algorithms.OERAPGA;

public class OpenEndedRelevantAllelesPreservingGeneticAlgorithm<TGenotype, TEncoding, TProblem>
  : IterativeAlgorithm<TGenotype, TEncoding, TProblem, PopulationIterationResult<TGenotype>>
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : class, IProblem<TGenotype, TEncoding>
  where TGenotype : class {
  private double Strictness { get; init; } = 1.0;
  public required int PopulationSize { get; init; }
  public required ICrossover<TGenotype, TEncoding, TProblem> Crossover { get; init; }
  public required IMutator<TGenotype, TEncoding, TProblem> Mutator { get; init; }
  public required ISelector<TGenotype, TEncoding, TProblem> Selector { get; init; }
  public int Elites { get; init; } = 1;

  public required int MaxEffort { get; init; }

  public override PopulationIterationResult<TGenotype> ExecuteStep(TProblem problem, TEncoding searchSpace, PopulationIterationResult<TGenotype>? previousIterationResult, IRandomNumberGenerator random) {
    if (previousIterationResult == null)
      return new PopulationIterationResult<TGenotype>(CreateInitialPopulation(problem, searchSpace, random, PopulationSize));

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
    return new PopulationIterationResult<TGenotype>(newPopulation);
  }

  private static ObjectiveVector Combine((ISolution<TGenotype>, ISolution<TGenotype>) parents, Objective problemObjective, double strictness = 1.0) {
    var o1 = parents.Item1.ObjectiveVector;
    var o2 = parents.Item2.ObjectiveVector;
    if (o2.Dominates(o1, problemObjective)) (o1, o2) = (o2, o1);
    return strictness switch {
      >= 1.0 => o1,
      <= 0.0 => o2,
      _ => new ObjectiveVector(o1.Zip(o2).Select(pair => (pair.Item1 * strictness) + (pair.Item2 * (1.0 - strictness))).ToArray())
    };
  }

  public class Builder : PopulationBasedAlgorithmBuilder<
                           TGenotype,
                           TEncoding,
                           TProblem,
                           PopulationIterationResult<TGenotype>,
                           OpenEndedRelevantAllelesPreservingGeneticAlgorithm<TGenotype, TEncoding, TProblem>>,
                         IMutatorPrototype<TGenotype, TEncoding, TProblem>,
                         ICrossoverPrototype<TGenotype, TEncoding, TProblem> {
    public double MutationRate { get; set; } = 0.05;
    public int Elites { get; set; } = 1;
    public required IMutator<TGenotype, TEncoding, TProblem> Mutator { get; set; }
    public required ICrossover<TGenotype, TEncoding, TProblem> Crossover { get; set; }
    public required int MaxEffort { get; set; }

    public OpenEndedRelevantAllelesPreservingGeneticAlgorithm<TGenotype, TEncoding, TProblem> Create() {
      return new OpenEndedRelevantAllelesPreservingGeneticAlgorithm<TGenotype, TEncoding, TProblem> {
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

    public override OpenEndedRelevantAllelesPreservingGeneticAlgorithm<TGenotype, TEncoding, TProblem> BuildAlgorithm() => Create();
  }
}
