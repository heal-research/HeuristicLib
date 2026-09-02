using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators.MoveAppliers;
using HEAL.HeuristicLib.Operators.MoveCreators;
using HEAL.HeuristicLib.Operators.MoveEvaluators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Neighborhoods;

/// <remarks>
/// The type arguments are the search space and problem this neighborhood is written for. The three move operators it
/// exposes are bound to them and satisfy the agnostic <see cref="INeighborhood{TCandidate,TMove}"/>, so a run the
/// neighborhood was not written for is reported when the execution graph is built rather than at this declaration.
/// </remarks>
public abstract record Neighborhood<TCandidate, TSearchSpace, TProblem, TMove> :
    INeighborhood<TCandidate, TMove>, IDirectNeighborhood<TCandidate, TSearchSpace, TProblem, TMove>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public IMoveCreator<TCandidate, TMove> MoveCreator => new NeighborhoodCreator<TCandidate, TSearchSpace, TProblem, TMove>(this);
    public IMoveApplier<TCandidate, TMove> MoveApplier => new NeighborhoodApplier<TCandidate, TSearchSpace, TProblem, TMove>(this);
    public IMoveEvaluator<TCandidate, TMove> MoveEvaluator => new NeighborhoodEvaluator<TCandidate, TSearchSpace, TProblem, TMove>(this);

    public abstract IEnumerable<TMove> Moves(TCandidate candidate, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
    public abstract TCandidate Apply(TCandidate candidate, TMove move, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

    // this is a trivial implementation that applies and evaluates,
    // override this if you support partial evaluation (which may not require creating a new solution)
    public virtual ObjectiveVector Evaluate(TCandidate before, TMove? move, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
    {
        var d = move is null ? before : Apply(before, move, random, searchSpace, problem);
        return problem.Evaluate([d], random)[0];
    }
}
