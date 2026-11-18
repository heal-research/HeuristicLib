using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Analysis;

/// <summary>Builds analyses over the observations of algorithms and operators.</summary>
/// <remarks>
/// <para>
/// A trace reads one value from each observation through a function, such as
/// <c>crossover.Invocations.Trace(observation =&gt; observation.Offspring.Count)</c>. Counting, collecting and ranking are
/// ordinary functions in that form; ranking receives the call's own objective directions, as in
/// <c>evaluator.Invocations.Trace(observation =&gt; observation.ObjectiveVectors.BestMedianWorst(observation.Problem.Objective))</c>.
/// A value that keeps state across observations, such as a best so far, comes from an explicit state factory, which each run calls once.
/// </para>
/// <para>
/// An aggregate creates plain state for each run and updates it in place, as in
/// <c>evaluator.Invocations.Aggregate(() =&gt; new List&lt;int&gt;(), (counts, observation) =&gt; counts.Add(observation.Candidates.Count))</c>.
/// Read its state after execution with <c>run.Result(aggregate).Value</c>. Updates are serialized per run.
/// </para>
/// <para>
/// Both forms observe each boundary once, by its configuration and observation type, so a source given twice counts once.
/// Sources of different operations of one configuration are each observed.
/// </para>
/// <para>
/// Both forms hold delegates, so a configuration they are placed in is not serializable. Captured mutable state
/// shared across runs remains the caller's responsibility; per-result locks do not synchronize it across runs.
/// </para>
/// </remarks>
public static class Analyzer
{
    /// <summary>Records improvements at evaluators with no evaluator of this candidate type among their declared descendants.</summary>
    /// <remarks>
    /// Includes evaluators inside refiners and beneath caches; settings and delegates are not searched.
    /// Every point includes its observed batch in <see cref="BestSoFarPoint{TCandidate}.Evaluations"/>.
    /// Pass the definition to <c>algorithm.CreateRun(problem, random, [best])</c> and read its curve with
    /// <see cref="AlgorithmRun.Result{TResult}"/>.
    /// </remarks>
    /// <param name="clocks">Additional axes, distinct by equality. Omitted clocks are not recorded; elapsed time is opt-in.</param>
    /// <param name="retention">Decides which running best candidates are retained; defaults to <see cref="TraceRetention.OnChange"/>.</param>
    /// <param name="objectiveComparer">Orders objective vectors; when omitted, each observation's problem must supply a total order.</param>
    public static BestSoFarAnalyzer<TCandidate> BestSoFar<TCandidate>(IReadOnlyList<IClock>? clocks = null,
        TraceRetention? retention = null, IComparer<ObjectiveVector>? objectiveComparer = null) =>
        new(null, clocks, retention, objectiveComparer);

    /// <summary>Records improvements at the named evaluator, counting its evaluated candidates.</summary>
    /// <param name="evaluator">The evaluator to observe, matched by reference, including any wrapper explicitly named.</param>
    /// <param name="clocks">Additional axes, distinct by equality. Omitted clocks are not recorded; elapsed time is opt-in.</param>
    /// <param name="retention">Decides which running best candidates are retained; defaults to <see cref="TraceRetention.OnChange"/>.</param>
    /// <param name="objectiveComparer">Orders objective vectors; when omitted, the observation's problem must supply a total order.</param>
    public static BestSoFarAnalyzer<TCandidate> BestSoFar<TCandidate>(IOperator<Evaluation, TCandidate> evaluator,
        IReadOnlyList<IClock>? clocks = null, TraceRetention? retention = null, IComparer<ObjectiveVector>? objectiveComparer = null) =>
        new(evaluator, clocks, retention, objectiveComparer);

    /// <summary>Creates one state per run and updates it for observations from any source, once per configuration reference.</summary>
    /// <remarks>The analyzer holds delegates, so a configuration it is placed in is not serializable. Updates are serialized per result.</remarks>
    /// <param name="sources">At least one source; repeated configuration references are observed once.</param>
    /// <param name="create">Creates fresh mutable state for each run.</param>
    /// <param name="update">Updates that run's state on the observation's thread, under the result's lock.</param>
    /// <exception cref="InvalidOperationException"><paramref name="sources"/> is empty.</exception>
    public static AggregateAnalyzer<TObservation, TState> Aggregate<TObservation, TState>(
        IReadOnlyList<ObservationSource<TObservation>> sources, Func<TState> create, Action<TState, TObservation> update)
        where TState : class => new(sources, create, update);

    /// <summary>Traces one value per observation of any of the sources.</summary>
    /// <param name="sources">At least one source; repeated configuration references are observed once.</param>
    /// <param name="value">Computes a value under the result's lock, even for observations retention skips.</param>
    /// <param name="clocks">The clocks each retained entry records, distinct by equality. Omitted clocks are not recorded.</param>
    /// <param name="retention">Decides which computed values become entries; defaults to <see cref="TraceRetention.EveryObservation"/>.</param>
    /// <exception cref="InvalidOperationException"><paramref name="sources"/> is empty.</exception>
    public static TraceAnalyzer<TObservation, TValue> Trace<TObservation, TValue>(
        IReadOnlyList<ObservationSource<TObservation>> sources,
        Func<TObservation, TValue> value,
        IReadOnlyList<IClock>? clocks = null, TraceRetention? retention = null) =>
        new(sources, () => value, clocks, retention);

    /// <summary>Traces one value per observation of any of the sources, with a value function created for each run.</summary>
    /// <param name="sources">At least one source; repeated configuration references are observed once.</param>
    /// <param name="createState">Creates private accumulation state once per run.</param>
    /// <param name="value">Computes a value from that state and the observation under the result's lock, even when retention skips it.</param>
    /// <param name="clocks">The clocks each entry records. When omitted, entries record no time.</param>
    /// <param name="retention">Decides which observations become entries. When omitted, every observation does.</param>
    /// <exception cref="InvalidOperationException"><paramref name="sources"/> is empty.</exception>
    public static TraceAnalyzer<TObservation, TValue> Trace<TObservation, TState, TValue>(
        IReadOnlyList<ObservationSource<TObservation>> sources,
        Func<TState> createState, Func<TState, TObservation, TValue> value,
        IReadOnlyList<IClock>? clocks = null, TraceRetention? retention = null) where TState : class =>
        new(sources, () =>
        {
            var state = createState();
            return observation => value(state, observation);
        }, clocks, retention);
}

/// <summary>Builds traces over the observations of one source.</summary>
/// <remarks>A trace holds its functions, so a configuration it is placed in is not serializable.</remarks>
public static class ObservationTraces
{
    extension<TObservation>(ObservationSource<TObservation> source)
    {
        /// <summary>Traces one value per observation of this source.</summary>
        /// <param name="value">Computes a value under the result's lock, even for observations retention skips.</param>
        /// <param name="clocks">The clocks each retained entry records, distinct by equality. Omitted clocks are not recorded.</param>
        /// <param name="retention">Decides which computed values become entries; defaults to <see cref="TraceRetention.EveryObservation"/>.</param>
        public TraceAnalyzer<TObservation, TValue> Trace<TValue>(Func<TObservation, TValue> value,
            IReadOnlyList<IClock>? clocks = null, TraceRetention? retention = null) =>
            Analyzer.Trace([source], value, clocks, retention);

        /// <summary>Traces one value per observation of this source, with a value function created for each run.</summary>
        /// <param name="createState">Creates private accumulation state once per run.</param>
        /// <param name="value">Computes a value from that state and the observation under the result's lock, even when retention skips it.</param>
        /// <param name="clocks">The clocks each entry records. When omitted, entries record no time.</param>
        /// <param name="retention">Decides which observations become entries. When omitted, every observation does.</param>
        public TraceAnalyzer<TObservation, TValue> Trace<TState, TValue>(Func<TState> createState, Func<TState, TObservation, TValue> value,
            IReadOnlyList<IClock>? clocks = null, TraceRetention? retention = null) where TState : class =>
            Analyzer.Trace([source], createState, value, clocks, retention);
    }
}

/// <summary>Builds accumulating analyzers over one source.</summary>
/// <remarks>An analyzer holds delegates, so a configuration it is placed in is not serializable.</remarks>
public static class ObservationAggregates
{
    extension<TObservation>(ObservationSource<TObservation> source)
    {
        /// <summary>Creates one state per run and updates it for each observation, with updates serialized per result.</summary>
        /// <param name="create">Creates fresh mutable state for each run.</param>
        /// <param name="update">Updates that run's state on the observation's thread, under the result's lock.</param>
        public AggregateAnalyzer<TObservation, TState> Aggregate<TState>(Func<TState> create, Action<TState, TObservation> update)
            where TState : class => Analyzer.Aggregate([source], create, update);
    }
}

/// <summary>A configuration whose execution node is one run's analysis result.</summary>
/// <remarks>
/// Recording and result lookup use the root scope and span the run, including every turn of a Cycle.
/// <see cref="ResolutionScope.Share"/> does not narrow the result that recording and <c>run.Result(analyzer)</c> use; resolving
/// a shared analyzer directly through a scope gives a separate execution node that nothing records into.
/// Definitions use reference identity and own no run data; synchronization belongs to the result or its recorder.
/// </remarks>
public abstract class Analyzer<TResult> : SubstitutionGroup, IConfigurationNode<TResult> where TResult : class, IRunResult
{
    protected Analyzer() { }

    /// <summary>Creates one run's empty result.</summary>
    /// <param name="scope">The construction scope; use <see cref="ResolutionScope.Results"/> to read other results.</param>
    /// <returns>A fresh result, with no mutable state shared with another run.</returns>
    public abstract TResult CreateExecutionNode(ResolutionScope scope);
}
