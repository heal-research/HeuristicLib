using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Neighborhoods;

/// <summary>
/// A neighborhood's moves, their application and their evaluation, configured as one operator that performs all three
/// move operations.
/// </summary>
/// <remarks>
/// The type arguments are the search space and problem this neighborhood is written for, and a run it was not written
/// for is reported when the execution graph is built. A neighborhood holds no execution data, so it is its own executor
/// for every move operation, and <see cref="MoveCreator"/>, <see cref="MoveApplier"/> and <see cref="MoveEvaluator"/>
/// return the neighborhood itself. Advice selecting one of them therefore applies to that operation of this
/// neighborhood.
/// </remarks>
public abstract record Neighborhood<TCandidate, TSearchSpace, TProblem, TMove>
    : INeighborhood<TCandidate, TMove>,
      IOperator<MoveCreation<TMove>, TCandidate>,
      IOperator<MoveApplication<TMove>, TCandidate>,
      IOperator<MoveEvaluation<TMove>, TCandidate>,
      IMoveCreationExecutor<TCandidate, TSearchSpace, TProblem, TMove>,
      IMoveApplicationExecutor<TCandidate, TSearchSpace, TProblem, TMove>,
      IMoveEvaluationExecutor<TCandidate, TSearchSpace, TProblem, TMove>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public IOperator<MoveCreation<TMove>, TCandidate> MoveCreator => this;
    public IOperator<MoveApplication<TMove>, TCandidate> MoveApplier => this;
    public IOperator<MoveEvaluation<TMove>, TCandidate> MoveEvaluator => this;

    IExecutor IOperator<MoveCreation<TMove>, TCandidate>.CreateExecutor<TRunSearchSpace, TRunProblem, TRunSearchState>(
        ResolutionScope<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState> scope) => this;

    IExecutor IOperator<MoveApplication<TMove>, TCandidate>.CreateExecutor<TRunSearchSpace, TRunProblem, TRunSearchState>(
        ResolutionScope<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState> scope) => this;

    IExecutor IOperator<MoveEvaluation<TMove>, TCandidate>.CreateExecutor<TRunSearchSpace, TRunProblem, TRunSearchState>(
        ResolutionScope<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState> scope) => this;

    public bool Fits(ExecutionSignature execution) =>
        execution.SearchSpace.IsAssignableTo(typeof(TSearchSpace)) && execution.Problem.IsAssignableTo(typeof(TProblem));

    public abstract IEnumerable<TMove> Moves(TCandidate candidate, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
    public abstract TCandidate Apply(TCandidate candidate, TMove move, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

    // this is a trivial implementation that applies and evaluates,
    // override this if you support partial evaluation (which may not require creating a new solution)
    public virtual ObjectiveVector Evaluate(TCandidate before, TMove? move, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
    {
        var d = move is null ? before : Apply(before, move, random, searchSpace, problem);
        return problem.Evaluate([d], random)[0];
    }

    ObjectiveVector IMoveEvaluationExecutor<TCandidate, TSearchSpace, TProblem, TMove>.Evaluate(ObjectiveVector oldQuality, TCandidate candidate, TMove move, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
        Evaluate(candidate, move, random, searchSpace, problem);

    ObjectiveVector IMoveEvaluationExecutor<TCandidate, TSearchSpace, TProblem, TMove>.Evaluate(TCandidate candidate, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
        problem.Evaluate([candidate], random)[0];
}
