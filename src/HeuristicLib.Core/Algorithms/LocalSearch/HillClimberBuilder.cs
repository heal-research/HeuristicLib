using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Operators.Interceptors;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Operators.Terminators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms.LocalSearch;

public record HillClimberBuilder<TG, TS, TP>
  : AlgorithmBuilder<TG, TS, TP, SingleSolutionIterationState<TG>, HillClimber<TG, TS, TP>, HillClimberBuildSpec<TG, TS, TP>>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
  where TG : class 
{
  public int MaxNeighbors { get; set; } = 100;
  public int BatchSize { get; set; } = 100;
  public LocalSearchDirection Direction { get; set; } = LocalSearchDirection.FirstImprovement;
  
  public required IMutator<TG, TS, TP> Mutator { get; set; }
  public required ICreator<TG, TS, TP> Creator { get; set; }
  
  public IEvaluator<TG, TS, TP> Evaluator { get; set; } = new DirectEvaluator<TG>();
  public ITerminator<TG, SingleSolutionIterationState<TG>, TS, TP> Terminator { get; set; } = new AfterIterationsTerminator<TG>(100);
  public IInterceptor<TG, SingleSolutionIterationState<TG>, TS, TP>? Interceptor { get; set; } = null;

  protected override HillClimberBuildSpec<TG, TS, TP> CreateBuildSpec() => new HillClimberBuildSpec<TG, TS, TP>(
    Evaluator, Terminator, Interceptor, Observer, MaxNeighbors, BatchSize, Direction, Mutator, Creator
  );
  
  protected override HillClimber<TG, TS, TP> BuildFromSpec(HillClimberBuildSpec<TG, TS, TP> spec) => new() {
    Terminator = spec.Terminator,
    Interceptor = spec.Interceptor,
    Creator = spec.Creator,
    Mutator = spec.Mutator,
    Evaluator = spec.Evaluator,
    MaxNeighbors = spec.MaxNeighbors,
    BatchSize = spec.BatchSize,
    Direction = spec.Direction
  };
}
