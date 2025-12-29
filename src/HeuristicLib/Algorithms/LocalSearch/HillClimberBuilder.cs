using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Operators.Interceptors;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Operators.Terminators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms.LocalSearch;

public record HillClimberBuilder<TG, TS, TP>
  : AlgorithmBuilder<TG, TS, TP, SingleSolutionIterationState<TG>, HillClimber<TG, TS, TP>>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
  where TG : class 
{
  public int? RandomSeed { get; set; } = null; 
  
  public int MaxNeighbors { get; set; } = 100;
  public int BatchSize { get; set; } = 100;
  public LocalSearchDirection Direction { get; set; } = LocalSearchDirection.FirstImprovement;
  
  public required IMutator<TG, TS, TP> Mutator { get; set; }
  public required ICreator<TG, TS, TP> Creator { get; set; }
  
  public IEvaluator<TG, TS, TP> Evaluator { get; set; } = new DirectEvaluator<TG>();
  public ITerminator<TG, SingleSolutionIterationState<TG>, TS, TP> Terminator { get; set; } = new AfterIterationsTerminator<TG>(100);
  public IInterceptor<TG, SingleSolutionIterationState<TG>, TS, TP>? Interceptor { get; set; } = null;

  public override HillClimber<TG, TS, TP> Build() => new() {
    AlgorithmRandom = SystemRandomNumberGenerator.Default(RandomSeed),
    Terminator = Terminator,
    Interceptor = Interceptor,
    Creator = Creator,
    Mutator = Mutator,
    Evaluator = Evaluator,
    MaxNeighbors = MaxNeighbors,
    BatchSize = BatchSize,
    Direction = Direction
  };
}
