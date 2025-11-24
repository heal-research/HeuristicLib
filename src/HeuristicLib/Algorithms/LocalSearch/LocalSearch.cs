using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Creator;
using HEAL.HeuristicLib.Operators.Evaluator;
using HEAL.HeuristicLib.Operators.Interceptor;
using HEAL.HeuristicLib.Operators.Mutator;
using HEAL.HeuristicLib.Operators.Prototypes;
using HEAL.HeuristicLib.Operators.Selector;
using HEAL.HeuristicLib.Operators.Terminator;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Algorithms.LocalSearch;

public class LocalSearch<TGenotype, TEncoding, TProblem>(
  ITerminator<TGenotype, SingleISolutionIterationResult<TGenotype>, TEncoding, TProblem> terminator,
  IInterceptor<TGenotype, SingleISolutionIterationResult<TGenotype>, TEncoding, TProblem>? interceptor,
  ICreator<TGenotype, TEncoding, TProblem> creator,
  IMutator<TGenotype, TEncoding, TProblem> mutator,
  IEvaluator<TGenotype, TEncoding, TProblem> evaluator,
  int? randomSeed,
  int maxNeighbors,
  int batchSize,
  LocalSearchDirection direction)
  : IterativeAlgorithm<TGenotype, TEncoding, TProblem, SingleISolutionIterationResult<TGenotype>>(terminator, randomSeed, interceptor)
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : class, IProblem<TGenotype, TEncoding> {
  public ICreator<TGenotype, TEncoding, TProblem> Creator { get; } = creator;
  public IMutator<TGenotype, TEncoding, TProblem> Mutator { get; } = mutator;
  public IEvaluator<TGenotype, TEncoding, TProblem> Evaluator { get; } = evaluator;
  public LocalSearchDirection Direction { get; } = direction;
  public int MaxNeighbors { get; } = maxNeighbors;
  public int BatchSize { get; } = batchSize;

  public override SingleISolutionIterationResult<TGenotype> ExecuteStep(
    TProblem problem,
    TEncoding searchSpace,
    SingleISolutionIterationResult<TGenotype>? previousIterationResult,
    IRandomNumberGenerator random) {
    if (previousIterationResult == null) {
      var ind = Creator.Create(1, random, searchSpace, problem);
      var obj = Evaluator.Evaluate(ind, random, searchSpace, problem);
      return new SingleISolutionIterationResult<TGenotype>(new Solution<TGenotype>(ind[0], obj[0]));
    }

    var sol = previousIterationResult.Solution;
    var newISolution = sol;

    for (int i = 0; i < MaxNeighbors; i += BatchSize) {
      var child = Mutator.Mutate(Enumerable.Repeat(sol.Genotype, BatchSize).ToArray(), random, searchSpace, problem);
      var res = Evaluator.Evaluate(child, random, searchSpace, problem);
      var best = BestSelector.Select(res.Append(sol.ObjectiveVector).ToArray(), problem.Objective, 1, random)[0];
      if (best == BatchSize)
        continue;
      newISolution = new Solution<TGenotype>(child[best], res[best]);
      if (Direction == LocalSearchDirection.FirstImprovement)
        return new SingleISolutionIterationResult<TGenotype>(newISolution);
    }

    return new SingleISolutionIterationResult<TGenotype>(newISolution);
  }

  public class Prototype : Prototype<TGenotype, TEncoding, TProblem, SingleISolutionIterationResult<TGenotype>>, IMutatorPrototype<TGenotype, TEncoding, TProblem> {
    public Prototype(ICreator<TGenotype, TEncoding, TProblem> creator, IMutator<TGenotype, TEncoding, TProblem> mutator, ITerminator<TGenotype, SingleISolutionIterationResult<TGenotype>, TEncoding, TProblem> terminator, IEvaluator<TGenotype, TEncoding, TProblem> evaluator, int? randomSeed, int maxNeighbors, int batchSize, LocalSearchDirection direction, IInterceptor<TGenotype, SingleISolutionIterationResult<TGenotype>, TEncoding, TProblem>? interceptor) : base(creator, terminator, evaluator, randomSeed, interceptor) {
      Mutator = mutator;
      MaxNeighbors = maxNeighbors;
      BatchSize = batchSize;
      Direction = direction;
    }

    public IMutator<TGenotype, TEncoding, TProblem> Mutator { get; set; }
    public int MaxNeighbors { get; set; }
    public int BatchSize { get; set; }
    public LocalSearchDirection Direction { get; set; }

    public LocalSearch<TGenotype, TEncoding, TProblem> Create() =>
      new(Terminator, Interceptor, Creator, Mutator, Evaluator, RandomSeed, MaxNeighbors, BatchSize, Direction);

    protected override IIterativeAlgorithm<TGenotype, TEncoding, TProblem, SingleISolutionIterationResult<TGenotype>> BuildAlgorithm() => Create();
  }
}

public static class LocalSearch {
  public static LocalSearch<TGenotype, TEncoding, TProblem>.Prototype GetPrototype<TGenotype, TEncoding, TProblem>(ICreator<TGenotype, TEncoding, TProblem> creator,
                                                                                                                   IMutator<TGenotype, TEncoding, TProblem> mutator,
                                                                                                                   ITerminator<TGenotype, SingleISolutionIterationResult<TGenotype>, TEncoding, TProblem> terminator,
                                                                                                                   IEvaluator<TGenotype, TEncoding, TProblem> evaluator,
                                                                                                                   int? randomSeed,
                                                                                                                   int maxNeighbors,
                                                                                                                   int batchSize,
                                                                                                                   LocalSearchDirection direction,
                                                                                                                   IInterceptor<TGenotype, SingleISolutionIterationResult<TGenotype>, TEncoding, TProblem>? interceptor = null) where TEncoding : class, IEncoding<TGenotype> where TProblem : class, IProblem<TGenotype, TEncoding> {
    return new LocalSearch<TGenotype, TEncoding, TProblem>.Prototype(creator, mutator, terminator, evaluator, randomSeed, maxNeighbors, batchSize, direction, interceptor);
  }
}
