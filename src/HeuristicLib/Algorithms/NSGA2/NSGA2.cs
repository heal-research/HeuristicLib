using HEAL.HeuristicLib.Operators.Creator;
using HEAL.HeuristicLib.Operators.Crossover;
using HEAL.HeuristicLib.Operators.Evaluator;
using HEAL.HeuristicLib.Operators.Interceptor;
using HEAL.HeuristicLib.Operators.Mutator;
using HEAL.HeuristicLib.Operators.Prototypes;
using HEAL.HeuristicLib.Operators.Replacer;
using HEAL.HeuristicLib.Operators.Selector;
using HEAL.HeuristicLib.Operators.Terminator;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Algorithms.NSGA2;

public class Nsga2<TGenotype, TEncoding, TProblem>(
  ITerminator<TGenotype, Nsga2IterationResult<TGenotype>, TEncoding, TProblem> terminator,
  IInterceptor<TGenotype, Nsga2IterationResult<TGenotype>, TEncoding, TProblem>? interceptor,
  int populationSize,
  ICreator<TGenotype, TEncoding, TProblem> creator,
  ICrossover<TGenotype, TEncoding, TProblem> crossover,
  IMutator<TGenotype, TEncoding, TProblem> mutator,
  double mutationRate,
  ISelector<TGenotype, TEncoding, TProblem> selector,
  IEvaluator<TGenotype, TEncoding, TProblem> evaluator,
  int? randomSeed,
  bool dominateOnEquals) :
  IterativeAlgorithm<TGenotype, TEncoding, TProblem, Nsga2IterationResult<TGenotype>>(terminator, randomSeed, interceptor)
  where TProblem : class, IProblem<TGenotype, TEncoding>
  where TEncoding : class, IEncoding<TGenotype> {
  public int PopulationSize { get; } = populationSize;
  public ICreator<TGenotype, TEncoding, TProblem> Creator { get; } = creator;
  public ICrossover<TGenotype, TEncoding, TProblem> Crossover { get; } = crossover;
  public IMutator<TGenotype, TEncoding, TProblem> Mutator { get; } = mutator.WithRate(mutationRate);
  public ISelector<TGenotype, TEncoding, TProblem> Selector { get; } = selector;
  public IEvaluator<TGenotype, TEncoding, TProblem> Evaluator { get; } = evaluator;
  public IReplacer<TGenotype, TEncoding, TProblem> Replacer { get; } = new ParetoCrowdingReplacer<TGenotype>(dominateOnEquals);

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

    public override Nsga2<TGenotype, TEncoding, TProblem> BuildAlgorithm() => new(Terminator, Interceptor, PopulationSize, Creator,
      Crossover, Mutator, MutationRate, Selector, Evaluator,
      RandomSeed, DominateOnEquals);
  }
}

public static class Nsga2 {
  public static Nsga2<TGenotype, TEncoding, TProblem>.Builder CreatePrototype<TGenotype, TEncoding, TProblem>(
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
