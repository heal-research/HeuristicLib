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
/// for is reported when the execution graph is built. The bound executor serves every move operation, and <see cref="MoveCreator"/>, <see cref="MoveApplier"/> and <see cref="MoveEvaluator"/>
/// return the neighborhood itself. A wrapper placed in one of these slots therefore wraps that operation of this
/// neighborhood.
/// </remarks>
public abstract record Neighborhood<TCandidate, TSearchSpace, TProblem, TMove>
    : Operator<MoveCreation<TMove>, TCandidate, TSearchSpace, TProblem, IExecutor>,
      INeighborhood<TCandidate, TMove>,
      IOperator<MoveApplication<TMove>, TCandidate>,
      IOperator<MoveEvaluation<TMove>, TCandidate>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public IOperator<MoveCreation<TMove>, TCandidate> MoveCreator => this;
    public IOperator<MoveApplication<TMove>, TCandidate> MoveApplier => this;
    public IOperator<MoveEvaluation<TMove>, TCandidate> MoveEvaluator => this;

    /// <summary>Creates a bound executor for all three move operations.</summary>
    /// <remarks>
    /// The base's one hook serves every operation this record implements, so a scope holds one executor for the
    /// neighborhood whichever operation it resolves first. The base names move creation only because it takes one operation.
    /// </remarks>
    protected override IExecutor CreateExecutor<TSearchState>(ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState> scope) =>
        new Executor(this, scope.Problem);

    public abstract IEnumerable<TMove> Moves(TCandidate candidate, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
    public abstract TCandidate Apply(TCandidate candidate, TMove move, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

    // this is a trivial implementation that applies and evaluates,
    // override this if you support partial evaluation (which may not require creating a new solution)
    public virtual ObjectiveVector Evaluate(TCandidate before, TMove? move, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
    {
        var d = move is null ? before : Apply(before, move, random, searchSpace, problem);
        return problem.Evaluate([d], random)[0];
    }

    private sealed class Executor(Neighborhood<TCandidate, TSearchSpace, TProblem, TMove> configuration, TProblem problem)
        : IMoveCreationExecutor<TCandidate, TMove>, IMoveApplicationExecutor<TCandidate, TMove>, IMoveEvaluationExecutor<TCandidate, TMove>
    {
        public IEnumerable<TMove> Moves(TCandidate candidate, IRandomNumberGenerator random) => configuration.Moves(candidate, random, problem.SearchSpace, problem);
        public TCandidate Apply(TCandidate candidate, TMove move, IRandomNumberGenerator random) => configuration.Apply(candidate, move, random, problem.SearchSpace, problem);
        public ObjectiveVector Evaluate(ObjectiveVector oldQuality, TCandidate candidate, TMove move, IRandomNumberGenerator random) => configuration.Evaluate(candidate, move, random, problem.SearchSpace, problem);
        public ObjectiveVector Evaluate(TCandidate candidate, IRandomNumberGenerator random) => problem.Evaluate([candidate], random)[0];
    }
}
