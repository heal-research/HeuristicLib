using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Analysis;

/// <summary>
/// One boundary of a run that analysis can observe, delivering observations of the type
/// <typeparamref name="TObservation"/>.
/// </summary>
/// <remarks>
/// An anchor names where an observation is taken, the way a clock names what time is read against. Implementing this
/// is how analysis reaches a boundary the library does not cover: the anchor decorates whatever it observes and
/// delivers the typed observation to the recorders installed at it. <see cref="Anchors"/> creates the anchors the
/// library ships.
/// </remarks>
public interface IAnchor<TObservation>
    where TObservation : Observation
{
    /// <summary>
    /// Declares the decoration that delivers this anchor's observations to one recorder.
    /// </summary>
    /// <remarks>
    /// Decorations compose, so installing at the same anchor twice stacks two wrappers rather than replacing one. The
    /// anchor carries the observed configuration into the observation, so a recorder can tell which configured operator
    /// produced it even when the resolved chain contains several wrappers.
    /// </remarks>
    void Install(ExecutionInstanceResolverBuilder builder, IObservationRecorder<TObservation> recorder);
}

/// <summary>
/// An anchor whose observations report the problem the run searches, which is what a ranking analysis orders by.
/// </summary>
/// <remarks>
/// This adds no behaviour. It carries <typeparamref name="TProblem"/> so that a trace composed at this anchor infers
/// the problem type, and through it reaches the run's objective without being told one.
/// </remarks>
public interface IAnchor<TObservation, TProblem> : IAnchor<TObservation>
    where TObservation : Observation<TProblem>
    where TProblem : class, IProblem;

/// <summary>
/// Creates the anchors the library ships.
/// </summary>
/// <remarks>
/// An anchor of your own belongs beside the boundary it observes, and is offered the same way: an extension on
/// <see cref="Anchor"/> named <c>At</c>, so every anchor is reached through one name.
/// </remarks>
public static class Anchor;

/// <summary>
/// The library's anchor factories, and the call that installs a recorder at any anchor.
/// </summary>
public static class Anchors
{
    extension(Anchor)
    {
        /// <summary>
        /// Creates an anchor at every search state an algorithm yields.
        /// </summary>
        public static AlgorithmAnchor<TCandidate, TSearchSpace, TProblem, TSearchState> At<TCandidate, TSearchSpace, TProblem, TSearchState>(
            IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState> algorithm)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace>
            where TSearchState : class, ISearchState =>
            new(algorithm);

        /// <summary>
        /// Creates an anchor at every evaluator call.
        /// </summary>
        public static EvaluatorAnchor<TCandidate, TSearchSpace, TProblem> At<TCandidate, TSearchSpace, TProblem>(
            IEvaluator<TCandidate, TSearchSpace, TProblem> evaluator)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
            new(evaluator);

        /// <summary>
        /// Creates an anchor at every crossover call.
        /// </summary>
        public static CrossoverAnchor<TCandidate, TSearchSpace, TProblem> At<TCandidate, TSearchSpace, TProblem>(
            ICrossover<TCandidate, TSearchSpace, TProblem> crossover)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
            new(crossover);

        /// <summary>
        /// Creates an anchor at every mutator call.
        /// </summary>
        public static MutatorAnchor<TCandidate, TSearchSpace, TProblem> At<TCandidate, TSearchSpace, TProblem>(
            IMutator<TCandidate, TSearchSpace, TProblem> mutator)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
            new(mutator);

        /// <summary>
        /// Creates an anchor at every interceptor call.
        /// </summary>
        public static InterceptorAnchor<TCandidate, TSearchSpace, TProblem, TSearchState> At<TCandidate, TSearchSpace, TProblem, TSearchState>(
            IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState> interceptor)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace>
            where TSearchState : class, ISearchState =>
            new(interceptor);
    }

    extension(ExecutionInstanceResolverBuilder builder)
    {
        /// <summary>
        /// Installs a recorder at an anchor.
        /// </summary>
        public void Observe<TObservation>(IAnchor<TObservation> anchor, IObservationRecorder<TObservation> recorder)
            where TObservation : Observation =>
            anchor.Install(builder, recorder);

        /// <summary>
        /// Installs a callback at an anchor, for an analysis that does not need a recorder type of its own.
        /// </summary>
        public void Observe<TObservation>(IAnchor<TObservation> anchor, Action<TObservation> record)
            where TObservation : Observation =>
            anchor.Install(builder, new DelegateObservationRecorder<TObservation>(record));
    }
}

/// <summary>
/// Delivers observations to a callback, so that an analysis can install one without declaring a recorder type.
/// </summary>
internal sealed class DelegateObservationRecorder<TObservation>(Action<TObservation> record)
    : IObservationRecorder<TObservation>
    where TObservation : Observation
{
    public void Record(TObservation observation) => record(observation);
}
