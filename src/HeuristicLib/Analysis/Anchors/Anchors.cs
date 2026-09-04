using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Analysis;

/// <summary>
/// Installs a recorder at one of the anchors analysis can observe.
/// </summary>
/// <remarks>
/// Every method decorates the anchor, so installing at the same anchor twice stacks two wrappers rather than replacing
/// one. The anchor itself is carried into the observation, so a recorder can tell which configured operator produced it
/// even when the resolved chain contains several wrappers.
/// </remarks>
public static class Anchors
{
    extension(ExecutionInstanceResolverBuilder builder)
    {
        public void Observe<TCandidate, TSearchSpace, TProblem, TSearchState>(
            IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState> anchor,
            IObservationRecorder<AlgorithmObservation<TCandidate, TSearchSpace, TProblem, TSearchState>> recorder)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace>
            where TSearchState : class, ISearchState =>
            builder.Decorate(anchor, current => new ObservingAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState>(anchor, current, [recorder]));

        public void Observe<TCandidate, TSearchSpace, TProblem>(
            IEvaluator<TCandidate, TSearchSpace, TProblem> anchor,
            IObservationRecorder<EvaluatorObservation<TCandidate, TSearchSpace, TProblem>> recorder)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
            builder.Decorate(anchor, current => new ObservingEvaluator<TCandidate, TSearchSpace, TProblem>(anchor, current, [recorder]));

        public void Observe<TCandidate, TSearchSpace, TProblem>(
            ICrossover<TCandidate, TSearchSpace, TProblem> anchor,
            IObservationRecorder<CrossoverObservation<TCandidate, TSearchSpace, TProblem>> recorder)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
            builder.Decorate(anchor, current => new ObservingCrossover<TCandidate, TSearchSpace, TProblem>(anchor, current, [recorder]));

        public void Observe<TCandidate, TSearchSpace, TProblem>(
            IMutator<TCandidate, TSearchSpace, TProblem> anchor,
            IObservationRecorder<MutatorObservation<TCandidate, TSearchSpace, TProblem>> recorder)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
            builder.Decorate(anchor, current => new ObservingMutator<TCandidate, TSearchSpace, TProblem>(anchor, current, [recorder]));

        public void Observe<TCandidate, TSearchSpace, TProblem, TSearchState>(
            IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState> anchor,
            IObservationRecorder<InterceptorObservation<TCandidate, TSearchSpace, TProblem, TSearchState>> recorder)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace>
            where TSearchState : class, ISearchState =>
            builder.Decorate(anchor, current => new ObservingInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState>(anchor, current, [recorder]));

        /// <summary>
        /// Installs a callback at the anchor, for an analysis that does not need a recorder type of its own.
        /// </summary>
        public void Observe<TCandidate, TSearchSpace, TProblem, TSearchState>(
            IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState> anchor,
            Action<AlgorithmObservation<TCandidate, TSearchSpace, TProblem, TSearchState>> record)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace>
            where TSearchState : class, ISearchState =>
            builder.Observe(anchor, new DelegateObservationRecorder<AlgorithmObservation<TCandidate, TSearchSpace, TProblem, TSearchState>>(record));

        /// <summary>
        /// Installs a callback at the anchor, for an analysis that does not need a recorder type of its own.
        /// </summary>
        public void Observe<TCandidate, TSearchSpace, TProblem>(
            IEvaluator<TCandidate, TSearchSpace, TProblem> anchor,
            Action<EvaluatorObservation<TCandidate, TSearchSpace, TProblem>> record)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
            builder.Observe(anchor, new DelegateObservationRecorder<EvaluatorObservation<TCandidate, TSearchSpace, TProblem>>(record));

        /// <summary>
        /// Installs a callback at the anchor, for an analysis that does not need a recorder type of its own.
        /// </summary>
        public void Observe<TCandidate, TSearchSpace, TProblem>(
            ICrossover<TCandidate, TSearchSpace, TProblem> anchor,
            Action<CrossoverObservation<TCandidate, TSearchSpace, TProblem>> record)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
            builder.Observe(anchor, new DelegateObservationRecorder<CrossoverObservation<TCandidate, TSearchSpace, TProblem>>(record));

        /// <summary>
        /// Installs a callback at the anchor, for an analysis that does not need a recorder type of its own.
        /// </summary>
        public void Observe<TCandidate, TSearchSpace, TProblem>(
            IMutator<TCandidate, TSearchSpace, TProblem> anchor,
            Action<MutatorObservation<TCandidate, TSearchSpace, TProblem>> record)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
            builder.Observe(anchor, new DelegateObservationRecorder<MutatorObservation<TCandidate, TSearchSpace, TProblem>>(record));

        /// <summary>
        /// Installs a callback at the anchor, for an analysis that does not need a recorder type of its own.
        /// </summary>
        public void Observe<TCandidate, TSearchSpace, TProblem, TSearchState>(
            IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState> anchor,
            Action<InterceptorObservation<TCandidate, TSearchSpace, TProblem, TSearchState>> record)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace>
            where TSearchState : class, ISearchState =>
            builder.Observe(anchor, new DelegateObservationRecorder<InterceptorObservation<TCandidate, TSearchSpace, TProblem, TSearchState>>(record));
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
