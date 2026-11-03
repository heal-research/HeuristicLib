using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.MoveCreators;

/// <remarks>
/// Derive directly from this base when the move creator needs mutable execution data. Use
/// <see cref="StatelessMoveCreator{TCandidate,TSearchSpace,TProblem,TMove}"/> when it does not.
/// <para>
/// The type arguments are the search space and problem this move creator is written for. The base bridges to whatever a
/// run requests, and a request the move creator was not written for is reported when the execution graph is built.
/// </para>
/// </remarks>
public abstract record MoveCreator<TCandidate, TSearchSpace, TProblem, TMove>
    : Operator<MoveCreation<TMove>, TCandidate, TSearchSpace, TProblem, IMoveCreationExecutor<TCandidate, TSearchSpace, TProblem, TMove>>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>;
