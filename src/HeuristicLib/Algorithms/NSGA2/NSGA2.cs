using HEAL.HeuristicLib.Operators.Creator;
using HEAL.HeuristicLib.Operators.Crossover;
using HEAL.HeuristicLib.Operators.Evaluator;
using HEAL.HeuristicLib.Operators.Mutator;
using HEAL.HeuristicLib.Operators.Prototypes;
using HEAL.HeuristicLib.Operators.Replacer;
using HEAL.HeuristicLib.Operators.Selector;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Algorithms.NSGA2;

public class Nsga2<TGenotype, TEncoding, TProblem> :
  IterativeAlgorithm<TGenotype, TEncoding, TProblem, Nsga2IterationResult<TGenotype>>
  where TProblem : class, IProblem<TGenotype, TEncoding>
  where TEncoding : class, IEncoding<TGenotype> {
  public required int PopulationSize { get; init; }
  public required ICreator<TGenotype, TEncoding, TProblem> Creator { get; init; }
  public required ICrossover<TGenotype, TEncoding, TProblem> Crossover { get; init; }
  public required IMutator<TGenotype, TEncoding, TProblem> Mutator { get; init; }
  public required ISelector<TGenotype, TEncoding, TProblem> Selector { get; init; }
  public required IEvaluator<TGenotype, TEncoding, TProblem> Evaluator { get; init; }
  public required IReplacer<TGenotype, TEncoding, TProblem> Replacer { get; init; }

  public override Nsga2IterationResult<TGenotype> ExecuteStep(TProblem problem, TEncoding searchSpace, Nsga2IterationResult<TGenotype>? previousIterationResult, IRandomNumberGenerator random) {
    if (previousIterationResult == null) {
      var genotypes = Creator.Create(PopulationSize, random, searchSpace, problem);
      var objectiveValues = Evaluator.Evaluate(genotypes, random, searchSpace, problem);
      return new Nsga2IterationResult<TGenotype>(Population.From(genotypes, objectiveValues));
    }

    var offspringCount = Replacer.GetOffspringCount(PopulationSize);
    var parents = Selector.Select(previousIterationResult.Population.Solutions, problem.Objective, offspringCount * 2, random, searchSpace, problem).ToGenotypePairs();
    var children = Crossover.Cross(parents, random, searchSpace, problem);
    var mutants = Mutator.Mutate(children, random, searchSpace, problem);
    var newPop = Population.From(mutants, Evaluator.Evaluate(mutants, random, searchSpace, problem));
    var nextPop = Replacer.Replace(previousIterationResult.Population.Solutions, newPop.Solutions, problem.Objective, random, searchSpace, problem);

    return new Nsga2IterationResult<TGenotype>(Population.From(nextPop));
  }

  public class Builder : PopulationBasedAlgorithmBuilder<TGenotype, TEncoding, TProblem, Nsga2IterationResult<TGenotype>, Nsga2<TGenotype, TEncoding, TProblem>>,
                         IMutatorPrototype<TGenotype, TEncoding, TProblem>, ICrossoverPrototype<TGenotype, TEncoding, TProblem> {
    public double MutationRate { get; set; } = 0.05;
    public bool DominateOnEquals { get; set; } = false;
    public required IMutator<TGenotype, TEncoding, TProblem> Mutator { get; set; }
    public required ICrossover<TGenotype, TEncoding, TProblem> Crossover { get; set; }

    public override Nsga2<TGenotype, TEncoding, TProblem> BuildAlgorithm() => new() {
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
  public static Nsga2<TGenotype, TEncoding, TProblem>.Builder CreateBuilder<TGenotype, TEncoding, TProblem>(
    ICreator<TGenotype, TEncoding, TProblem> creator,
    ICrossover<TGenotype, TEncoding, TProblem> crossover,
    IMutator<TGenotype, TEncoding, TProblem> mutator, bool dominateOnEquals = true)
    where TEncoding : class, IEncoding<TGenotype> where TProblem : class, IProblem<TGenotype, TEncoding>
    => new() {
      Mutator = mutator,
      Crossover = crossover,
      Creator = creator,
      Selector = new ParetoCrowdingTournamentSelector<TGenotype>(dominateOnEquals)
    };
}
