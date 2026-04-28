using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms.LocalSearch;

public record HillClimberBuilder<TG, TS, TP>
  : AlgorithmBuilder<TG, TS, TP, SingleSolutionState<TG>, HillClimber<TG, TS, TP>>,
    IBuilderWithCreator<TG, TS, TP>,
    IBuilderWithMutator<TG, TS, TP>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
  public int MaxNeighbors { get; set; } = 100;
  public int BatchSize { get; set; } = 100;
  public LocalSearchDirection Direction { get; set; } = LocalSearchDirection.FirstImprovement;

  public required IMutator<TG, TS, TP> Mutator { get; set; }
  public required ICreator<TG, TS, TP> Creator { get; set; }

  public override HillClimber<TG, TS, TP> Build()
  {
    return new HillClimber<TG, TS, TP> {
      Interceptor = Interceptor,
      Creator = Creator,
      Mutator = Mutator,
      Evaluator = Evaluator,
      MaxNeighbors = MaxNeighbors,
      BatchSize = BatchSize,
      Direction = Direction
    };
  }
}
