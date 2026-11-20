using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.MoveAppliers;

/// <remarks>
/// Derive directly from this base when the move applier needs mutable execution data. Use
/// <see cref="StatelessMoveApplier{TCandidate,TSearchSpace,TProblem,TMove}"/> when it does not.
/// <para>
/// The type arguments are the search space and problem this move applier is written for. The base bridges to whatever a
/// run requests, and a request the move applier was not written for is reported when the execution graph is built.
/// </para>
/// </remarks>
public abstract record MoveApplier<TCandidate, TSearchSpace, TProblem, TMove>
    : Operator<MoveApplication<TMove>, TCandidate, TSearchSpace, TProblem, IMoveApplicationExecutor<TCandidate, TMove>>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>;
