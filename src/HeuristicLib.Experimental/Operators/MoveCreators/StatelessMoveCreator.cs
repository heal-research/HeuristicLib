using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.MoveCreators;

/// <summary>A stateless move operator whose executor retains its bound problem.</summary>
public abstract record StatelessMoveCreator<TCandidate, TSearchSpace, TProblem, TMove>
    : Operator<MoveCreation<TMove>, TCandidate, TSearchSpace, TProblem, IMoveCreationExecutor<TCandidate, TMove>>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected sealed override IMoveCreationExecutor<TCandidate, TMove> CreateExecutor<TRunSearchState>(ResolutionScope<TCandidate, TSearchSpace, TProblem, TRunSearchState> scope) => new Executor(this, scope.Problem);

    public abstract IEnumerable<TMove> Moves(TCandidate candidate, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

    private sealed class Executor(StatelessMoveCreator<TCandidate, TSearchSpace, TProblem, TMove> configuration, TProblem problem) : IMoveCreationExecutor<TCandidate, TMove>
    {
        public IEnumerable<TMove> Moves(TCandidate candidate, IRandomNumberGenerator random) => configuration.Moves(candidate, random, problem.SearchSpace, problem);
    }
}
