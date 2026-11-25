using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.MoveAppliers;

/// <summary>A stateless move operator whose executor retains its bound problem.</summary>
public abstract record StatelessMoveApplier<TCandidate, TSearchSpace, TProblem, TMove>
    : Operator<MoveApplication<TMove>, TCandidate, TSearchSpace, TProblem, IMoveApplicationExecutor<TCandidate, TMove>>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected sealed override IMoveApplicationExecutor<TCandidate, TMove> CreateExecutor<TRunSearchState>(ResolutionScope<TCandidate, TSearchSpace, TProblem, TRunSearchState> scope) => new Executor(this, scope.Problem);

    public abstract TCandidate Apply(TCandidate candidate, TMove move, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

    private sealed class Executor(StatelessMoveApplier<TCandidate, TSearchSpace, TProblem, TMove> configuration, TProblem problem) : IMoveApplicationExecutor<TCandidate, TMove>
    {
        public TCandidate Apply(TCandidate candidate, TMove move, IRandomNumberGenerator random) => configuration.Apply(candidate, move, random, problem.SearchSpace, problem);
    }
}
