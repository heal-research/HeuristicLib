using HEAL.HeuristicLib.OperatorPrototypes;
using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Operators.Crossovers;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Operators.Replacers;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Algorithms.NSGA2;

public class Nsga2<TGenotype, TSearchSpace, TProblem> :
  IterativeAlgorithm<TGenotype, TSearchSpace, TProblem, PopulationIterationResult<TGenotype>>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TGenotype : class {
  public required int PopulationSize { get; init; }
  public required ICrossover<TGenotype, TSearchSpace, TProblem> Crossover { get; init; }
  public required IMutator<TGenotype, TSearchSpace, TProblem> Mutator { get; init; }
  public required ISelector<TGenotype, TSearchSpace, TProblem> Selector { get; init; }
  public required IReplacer<TGenotype, TSearchSpace, TProblem> Replacer { get; init; }

  public override PopulationIterationResult<TGenotype> ExecuteStep(TProblem problem, TSearchSpace searchSpace, PopulationIterationResult<TGenotype>? previousIterationResult, IRandomNumberGenerator random) {
    if (previousIterationResult == null)
      return new PopulationIterationResult<TGenotype>(CreateInitialPopulation(problem, searchSpace, random, PopulationSize));

    var offspringCount = Replacer.GetOffspringCount(PopulationSize);
    var parents = Selector.Select(previousIterationResult.Population.Solutions, problem.Objective, offspringCount * 2, random, searchSpace, problem).ToGenotypePairs();
    var children = Crossover.Cross(parents, random, searchSpace, problem);
    var mutants = Mutator.Mutate(children, random, searchSpace, problem);
    var newPop = Population.From(mutants, Evaluator.Evaluate(mutants, random, searchSpace, problem));
    var nextPop = Replacer.Replace(previousIterationResult.Population.Solutions, newPop.Solutions, problem.Objective, random, searchSpace, problem);

    return new PopulationIterationResult<TGenotype>(Population.From(nextPop));
  }

  public class Builder : PopulationBasedAlgorithmBuilder<TGenotype, TSearchSpace, TProblem, PopulationIterationResult<TGenotype>, Nsga2<TGenotype, TSearchSpace, TProblem>>,
                         IMutatorPrototype<TGenotype, TSearchSpace, TProblem>, ICrossoverPrototype<TGenotype, TSearchSpace, TProblem> {
    public double MutationRate { get; set; } = 0.05;
    public bool DominateOnEquals { get; set; } = false;
    public required IMutator<TGenotype, TSearchSpace, TProblem> Mutator { get; set; }
    public required ICrossover<TGenotype, TSearchSpace, TProblem> Crossover { get; set; }

    public override Nsga2<TGenotype, TSearchSpace, TProblem> BuildAlgorithm() => new() {
      Terminator = Terminator,
      Interceptor = Interceptor,
      PopulationSize = PopulationSize,
      Creator = Creator,
      Crossover = Crossover,
      Mutator = Mutator.WithRate(MutationRate),
      Selector = Selector,
      Evaluator = Evaluator,
      AlgorithmRandom = SystemRandomNumberGenerator.Default(RandomSeed),
      Replacer = new ParetoCrowdingReplacer<TGenotype>(DominateOnEquals)
    };
  }
}

public static class Nsga2 {
  public static Nsga2<TGenotype, TSearchSpace, TProblem>.Builder GetBuilder<TGenotype, TSearchSpace, TProblem>(
    ICreator<TGenotype, TSearchSpace, TProblem> creator,
    ICrossover<TGenotype, TSearchSpace, TProblem> crossover,
    IMutator<TGenotype, TSearchSpace, TProblem> mutator, bool dominateOnEquals = true)
    where TSearchSpace : class, ISearchSpace<TGenotype> where TProblem : class, IProblem<TGenotype, TSearchSpace> where TGenotype : class => new() {
    Mutator = mutator,
    Crossover = crossover,
    Creator = creator,
    Selector = new ParetoCrowdingTournamentSelector<TGenotype>(dominateOnEquals)
  };
}
