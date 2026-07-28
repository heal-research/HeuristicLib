using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms.LocalSearch;

public record HillClimberBuilder<TCandidate, TSearchSpace, TProblem>
  : AlgorithmBuilder<TCandidate, TSearchSpace, TProblem, SingleSolutionState<TCandidate>, HillClimber<TCandidate, TSearchSpace, TProblem>>,
    IBuilderWithCreator<TCandidate, TSearchSpace, TProblem>,
    IBuilderWithMutator<TCandidate, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public int MaxNeighbors { get; set; } = 100;
    public int BatchSize { get; set; } = 100;
    public LocalSearchDirection Direction { get; set; } = LocalSearchDirection.FirstImprovement;

    public required IMutator<TCandidate, TSearchSpace, TProblem> Mutator { get; set; }
    public required ICreator<TCandidate, TSearchSpace, TProblem> Creator { get; set; }

    public override HillClimber<TCandidate, TSearchSpace, TProblem> Build()
    {
        return new()
        {
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
