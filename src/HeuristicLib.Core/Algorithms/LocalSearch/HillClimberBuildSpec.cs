using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Operators.Interceptors;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Operators.Terminators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms.LocalSearch;

public sealed record HillClimberBuildSpec<TG, TS, TP>
  : AlgorithmBuildSpec<TG, TS, TP, SingleSolutionState<TG>>,
    ISpecWithCreator<TG, TS, TP>,
    ISpecWithMutator<TG, TS, TP>
  where TG : class
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
  public int MaxNeighbors { get; set; }
  public int BatchSize { get; set; }
  public LocalSearchDirection Direction { get; set; }
  public IMutator<TG, TS, TP> Mutator { get; set; }
  public ICreator<TG, TS, TP> Creator { get; set; }

  public HillClimberBuildSpec(
    IEvaluator<TG, TS, TP> Evaluator,
    ITerminator<TG, SingleSolutionState<TG>, TS, TP> Terminator,
    IInterceptor<TG, SingleSolutionState<TG>, TS, TP>? Interceptor,
    int MaxNeighbors,
    int BatchSize,
    LocalSearchDirection Direction,
    IMutator<TG, TS, TP> Mutator,
    ICreator<TG, TS, TP> Creator
  )
    : base(Evaluator, Terminator, Interceptor)
  {
    this.MaxNeighbors = MaxNeighbors;
    this.BatchSize = BatchSize;
    this.Direction = Direction;
    this.Mutator = Mutator;
    this.Creator = Creator;
  }
}
