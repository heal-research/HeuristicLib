using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <summary>One successful creation call, with the requested count and the candidates it returned.</summary>
/// <remarks>
/// The search space and problem are read at the candidate's interfaces, so an observation fits every run over the
/// candidate.
/// </remarks>
public sealed record CreationObservation<TCandidate>(
    IOperator<Creation, TCandidate> Creator,
    int Count,
    IReadOnlyList<TCandidate> Candidates,
    ISearchSpace<TCandidate> SearchSpace,
    IProblem<TCandidate, ISearchSpace<TCandidate>> Problem);

file sealed class CreationObservationAdvice<TCandidate>(Action<CreationObservation<TCandidate>> observe) : CreatorAdvice<TCandidate>
{
    protected override ICreationExecutor<TCandidate, TSearchSpace, TProblem> Wrap<TSearchSpace, TProblem>(
        ICreationExecutor<TCandidate, TSearchSpace, TProblem> executor, IOperator<Creation, TCandidate> creator) =>
        new Executor<TSearchSpace, TProblem>(executor, creator, observe);

    private sealed class Executor<TSearchSpace, TProblem>(ICreationExecutor<TCandidate, TSearchSpace, TProblem> executor,
        IOperator<Creation, TCandidate> creator, Action<CreationObservation<TCandidate>> observe)
        : ICreationExecutor<TCandidate, TSearchSpace, TProblem>, IExecutorControlProvider
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public IReadOnlyList<TCandidate> Create(int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            var candidates = executor.Create(count, random, searchSpace, problem);
            observe(new CreationObservation<TCandidate>(creator, count, candidates, searchSpace, problem));
            return candidates;
        }

        public TControl? Get<TControl>() where TControl : class => ExecutorControls.Find<TControl>(executor);
    }
}

public static class CreationObservations
{
    extension<TCandidate>(IOperator<Creation, TCandidate> creator)
    {
        /// <summary>The observations of this creator's successful calls.</summary>
        public ObservationSource<CreationObservation<TCandidate>> Calls =>
            new(creator, observe => Aspect.Create(Pointcut.Reference(creator), new CreationObservationAdvice<TCandidate>(observe)));
    }
}
