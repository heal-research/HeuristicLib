using System.Collections.ObjectModel;

namespace HEAL.HeuristicLib.Tests.ApiUsageSpecs.Analysis.Desired;

#pragma warning disable S2325 // Instance methods are part of the desired authoring shape.
#pragma warning disable S2326 // Generic marker parameters express compile-time supply compatibility.

/// <summary>
/// Executable prototype of the replacement analysis API. The model below exists only to make the desired authoring
/// shape and generic inference executable before the production contracts change.
/// </summary>
public class DesiredAnalysisSpecs
{
    [Fact]
    public async Task QualitySeries_IsARunBoundObjectWithSafeLiveReads()
    {
        var algorithm = new AlgorithmAnchor();
        var iterations = Clock.FromIterations(algorithm);
        var evaluations = Clock.FromEvaluations(algorithm);
        var quality = Analyzer.Trace(
            new ObjectiveVectorsMeasurement(),
            Aggregate.BestMedianWorst(),
            at: algorithm,
            clocks: [iterations, evaluations]);
        var run = algorithm.CreateRun(quality);

        quality.SampleCount.ShouldBe(1);
        quality.Latest!.Value.Value.Best.ShouldBe(0.5);

        var duringExecution = quality.By(iterations);
        await run.CompleteAsync(TestContext.Current.CancellationToken);

        duringExecution.ShouldHaveSingleItem();
        quality.By(evaluations).Single().Time.ShouldBe(100);
    }

    [Fact]
    public void Snapshot_IsStableWhenTheAnalysisCollectsMoreData()
    {
        var algorithm = new AlgorithmAnchor();
        var quality = Analyzer.Trace(new ObjectiveVectorsMeasurement(), Aggregate.Best(), at: algorithm);
        algorithm.CreateRun(quality);
        var firstSnapshot = quality.Snapshot();

        quality.Record(new Moment(2, 200), 0.25);

        firstSnapshot.ShouldHaveSingleItem();
        quality.SampleCount.ShouldBe(2);
        quality.Snapshot().Count.ShouldBe(2);
    }

    [Fact]
    public void CommonAnalysis_IsAShortcutForTrace()
    {
        var algorithm = new AlgorithmAnchor();
        var shortcut = Analyzer.TraceBestMedianWorst(at: algorithm);
        var composed = Analyzer.Trace(new ObjectiveVectorsMeasurement(), Aggregate.BestMedianWorst(), at: algorithm);

        algorithm.CreateRun(shortcut, composed);

        shortcut.ShouldBeOfType<TraceAnalyzer<BestMedianWorst>>();
        composed.ShouldBeOfType<TraceAnalyzer<BestMedianWorst>>();
    }

    [Fact]
    public void MeasurementsAndAggregations_ComposeWithoutExplicitTypeArguments()
    {
        var algorithm = new AlgorithmAnchor();
        var scalar = Analyzer.Trace(new TreeLengthMeasurement(), Aggregate.MinMeanMax(), at: algorithm);
        var frequency = Analyzer.Trace(new VariablesUsedMeasurement(), Aggregate.Frequency(), at: algorithm);
        var pairwise = Analyzer.Trace(
            new PairwiseSimilarityMeasurement(SimilarityCalculator.Levenshtein()),
            Aggregate.MinMeanMax(),
            at: algorithm,
            recording: Recording.EveryNth(10));

        algorithm.CreateRun(scalar, frequency, pairwise);

        scalar.ShouldBeOfType<TraceAnalyzer<MinMeanMax>>();
        frequency.ShouldBeOfType<TraceAnalyzer<IReadOnlyDictionary<string, int>>>();
        pairwise.Recording.ShouldBe(Recording.EveryNth(10));
    }

    [Fact]
    public void AcrossFiringsAndRecordingPolicy_AreIndependentChoices()
    {
        var algorithm = new AlgorithmAnchor();
        var everyFiring = Analyzer.Trace(new ObjectiveVectorsMeasurement(), Aggregate.Best(), at: algorithm);
        var onlyImprovements = Analyzer.Trace(
            new ObjectiveVectorsMeasurement(),
            Aggregate.Best(),
            at: algorithm,
            across: Across.RunningBest(),
            recording: Recording.OnChange());

        algorithm.CreateRun(everyFiring, onlyImprovements);

        everyFiring.Across.ShouldBe(Across.EachFiring());
        everyFiring.Recording.ShouldBe(Recording.EveryFiring());
        onlyImprovements.Across.ShouldBe(Across.RunningBest());
        onlyImprovements.Recording.ShouldBe(Recording.OnChange());
    }

    [Fact]
    public void SeparateAttachments_CreateIndependentRunBoundAnalyses()
    {
        var algorithm = new AlgorithmAnchor();
        var explore = Analyzer.Trace(new TreeLengthMeasurement(), Aggregate.MinMeanMax(), at: algorithm);
        var refine = Analyzer.Trace(new TreeLengthMeasurement(), Aggregate.MinMeanMax(), at: algorithm);

        algorithm.CreateRun(explore, refine);

        explore.ShouldNotBeSameAs(refine);
        explore.Snapshot().ShouldNotBeSameAs(refine.Snapshot());
    }

    [Fact]
    public void CandidateMeasurement_AttachesToACrossoverBoundary()
    {
        var algorithm = new AlgorithmAnchor();
        var crossover = new CrossoverAnchor();
        var offspringSize = Analyzer.Trace(
            observation => [observation.Offspring.Count],
            Aggregate.MinMeanMax(),
            at: crossover);

        algorithm.CreateRun(offspringSize);

        offspringSize.Snapshot().ShouldHaveSingleItem();
        // This is intentionally absent because it must not compile:
        // Analyzer.Trace(new ObjectiveVectorsMeasurement(), Aggregate.Best(), at: crossover);
    }

    [Fact]
    public void FrequencySeries_TransposesIntoOneStableSeriesPerKey()
    {
        var algorithm = new AlgorithmAnchor();
        var variables = Analyzer.Trace(new VariablesUsedMeasurement(), Aggregate.Frequency(), at: algorithm);
        algorithm.CreateRun(variables);

        var byVariable = variables.Transpose();

        byVariable.Keys.ShouldBe(["x1", "x2"], ignoreOrder: true);
        byVariable["x1"].Single().Value.ShouldBe(3);
    }

    [Fact]
    public void DomainCoordinate_IsContributedByTheAnchorStamp()
    {
        var algorithm = new AlgorithmAnchor();
        var evaluator = new DynamicEvaluatorAnchor();
        var epoch = Clock.FromValue(evaluator, static _ => 7);
        var perEpoch = Analyzer.Trace(
            _ => [0.5],
            Aggregate.Best(),
            at: evaluator,
            clocks: [epoch]);

        algorithm.CreateRun(perEpoch);

        perEpoch.By(epoch).Single().Time.ShouldBe(7);
    }

    [Fact]
    public void DetachedAnchor_IsAConfigurationError()
    {
        var algorithm = new AlgorithmAnchor();
        var detached = Analyzer.Trace(
            new ObjectiveVectorsMeasurement(),
            Aggregate.Best(),
            at: new DetachedAlgorithmAnchor());

        Should.Throw<InvalidOperationException>(() => algorithm.CreateRun(detached));
    }

    [Fact]
    public async Task Lineage_IsARunBoundAnalysisWithAnAnalysisSpecificReadingApi()
    {
        var algorithm = new AlgorithmAnchor();
        var lineage = Analyzer.TrackLineage(
            at: [new CreatorAnchor(), new CrossoverAnchor(), new MutatorAnchor()],
            comparer: StringComparer.Ordinal);
        var run = algorithm.CreateRun(lineage);

        lineage.RootCount.ShouldBe(1);
        var duringExecution = lineage.Snapshot();

        await run.CompleteAsync(TestContext.Current.CancellationToken);

        duringExecution.Roots.ShouldBe(["root"]);
        lineage.DescendantsOf("root").ShouldBe(["child"]);
    }

    [Fact]
    public void Experiment_CreatesAnIndependentRunBoundSeriesPerTrial()
    {
        var experiment = ExperimentAnalysisRun.Create("small", "large");

        var quality = experiment.Trace(new ObjectiveVectorsMeasurement(), Aggregate.Best());

        quality.Trials.Select(trial => trial.Key).ShouldBe(["small", "large"]);
        quality.Trials[0].Trace.ShouldNotBeSameAs(quality.Trials[1].Trace);
        quality.Trials[0].Trace.Snapshot().ShouldHaveSingleItem();
    }

}

#region Desired API prototype

public record AlgorithmAnchor;
public sealed record DetachedAlgorithmAnchor : AlgorithmAnchor;
public sealed record DynamicEvaluatorAnchor;
public sealed record CreatorAnchor;
public sealed record CrossoverAnchor;
public sealed record MutatorAnchor;
public sealed record CrossoverObservation(
    IReadOnlyList<string> Offspring,
    IReadOnlyList<(string Parent1, string Parent2)> Parents);
public sealed record AlgorithmObservation(
    IReadOnlyList<double> ObjectiveVectors,
    IReadOnlyList<string> Candidates);
public sealed record DynamicEvaluatorObservation(IReadOnlyList<double> ObjectiveVectors);

public readonly record struct Moment(long Iteration, long Evaluations, int? Epoch = null);
public readonly record struct TraceEntry<T>(Moment Moment, T Value)
{
    public TTime At<TTime>(Clock<TTime> clock) => clock.Read(Moment);
}
public readonly record struct TracePoint<TTime, TValue>(TTime Time, TValue Value);

public sealed class Clock<T>(Func<Moment, T> read)
{
    internal T Read(Moment moment) => read(moment);
}

public static class Clock
{
    public static Clock<long> FromIterations(AlgorithmAnchor _) => new(moment => moment.Iteration);
    public static Clock<long> FromEvaluations(AlgorithmAnchor _) => new(moment => moment.Evaluations);
    public static Clock<int> FromValue<TAnchor>(TAnchor _, Func<TAnchor, int> __) =>
        new(moment => moment.Epoch ?? throw new InvalidOperationException("The moment has no time for this clock."));
}

public sealed class TraceSnapshot<T>(IReadOnlyList<TraceEntry<T>> entries) : ReadOnlyCollection<TraceEntry<T>>(entries.ToArray());

public interface IAnalyzer
{
    void Bind(AlgorithmAnchor root);
}

public sealed class TraceAnalyzer<T>(
    Across across,
    Recording recording,
    IReadOnlyList<object> clocks,
    Action<AlgorithmAnchor, TraceAnalyzer<T>> bind) : IAnalyzer
{
    private readonly Lock sync = new();
    private readonly List<TraceEntry<T>> entries = [];

    public Across Across { get; } = across;
    public Recording Recording { get; } = recording;

    public int SampleCount
    {
        get { lock (sync) return entries.Count; }
    }

    public TraceEntry<T>? Latest
    {
        get { lock (sync) return entries.Count == 0 ? null : entries[^1]; }
    }

    internal void Record(Moment moment, T value)
    {
        lock (sync)
            entries.Add(new TraceEntry<T>(moment, value));
    }

    public TraceSnapshot<T> Snapshot()
    {
        lock (sync)
            return new TraceSnapshot<T>(entries);
    }

    public IReadOnlyList<TracePoint<TTime, T>> By<TTime>(Clock<TTime> clock)
    {
        if (!clocks.Contains(clock))
            throw new InvalidOperationException("The trace does not use the requested clock.");

        return Array.AsReadOnly(Snapshot()
            .Select(entry => new TracePoint<TTime, T>(entry.At(clock), entry.Value))
            .ToArray());
    }

    void IAnalyzer.Bind(AlgorithmAnchor root) => bind(root, this);
}

public static class TraceViews
{
    public static IReadOnlyDictionary<TKey, TraceSnapshot<TValue>> Transpose<TKey, TValue>(
        this TraceAnalyzer<IReadOnlyDictionary<TKey, TValue>> trace)
        where TKey : notnull =>
        new ReadOnlyDictionary<TKey, TraceSnapshot<TValue>>(trace.Snapshot()
            .SelectMany(traceEntry => traceEntry.Value.Select(value => (traceEntry.Moment, value.Key, value.Value)))
            .GroupBy(entry => entry.Key)
            .ToDictionary(
                group => group.Key,
                group => new TraceSnapshot<TValue>(group.Select(entry => new TraceEntry<TValue>(entry.Moment, entry.Value)).ToArray())));
}

public interface IMeasurement<in TObservation, out TValue>
{
    IReadOnlyList<TValue> Read(TObservation observation);
}
public interface IAggregation<in TValue, out TResult> { TResult Example { get; } }
public sealed record Aggregation<TValue, TResult>(TResult Example) : IAggregation<TValue, TResult>;

public readonly record struct MinMeanMax(double Min, double Mean, double Max);
public readonly record struct BestMedianWorst(double Best, double Median, double Worst);

public sealed record Recording(string Name, int Interval = 1)
{
    public static Recording EveryFiring() => new("Every firing");
    public static Recording EveryNth(int interval) => new("Every nth firing", interval);
    public static Recording OnChange() => new("On change");
}

public sealed record Across(string Name)
{
    public static Across EachFiring() => new("Each firing");
    public static Across RunningBest() => new("Running best");
}

public sealed record ObjectiveVectorsMeasurement : IMeasurement<AlgorithmObservation, double>
{
    public IReadOnlyList<double> Read(AlgorithmObservation observation) => observation.ObjectiveVectors;
}

public sealed record TreeLengthMeasurement : IMeasurement<AlgorithmObservation, double>
{
    public IReadOnlyList<double> Read(AlgorithmObservation _) => [12];
}

public sealed record VariablesUsedMeasurement : IMeasurement<AlgorithmObservation, string>
{
    public IReadOnlyList<string> Read(AlgorithmObservation _) => ["x1"];
}

public sealed record PairwiseSimilarityMeasurement(SimilarityCalculator Similarity)
    : IMeasurement<AlgorithmObservation, double>
{
    public IReadOnlyList<double> Read(AlgorithmObservation _) => [0.75];
}

public sealed record SimilarityCalculator(string Name)
{
    public static SimilarityCalculator Levenshtein() => new("Levenshtein");
}

public static class Aggregate
{
    public static Aggregation<double, double> Best() => new(0.5);
    public static Aggregation<double, BestMedianWorst> BestMedianWorst() => new(new(0.5, 1, 2));
    public static Aggregation<double, MinMeanMax> MinMeanMax() => new(new(1, 2, 3));
    public static Aggregation<string, IReadOnlyDictionary<string, int>> Frequency() =>
        new(new ReadOnlyDictionary<string, int>(new Dictionary<string, int> { ["x1"] = 3, ["x2"] = 1 }));
}

public sealed record LineageSnapshot(IReadOnlyList<string> Roots, IReadOnlyList<long> Generations);

public sealed class TrackedLineage(IReadOnlyList<object> anchors) : IAnalyzer
{
    private readonly ReadOnlyCollection<string> roots = Array.AsReadOnly(["root"]);
    private readonly ReadOnlyCollection<string> descendants = Array.AsReadOnly(["child"]);

    public int RootCount => roots.Count;
    public LineageSnapshot Snapshot() => new([.. roots], [1]);
    public IReadOnlyList<string> DescendantsOf(string root) => root == "root" ? descendants : [];

    void IAnalyzer.Bind(AlgorithmAnchor root)
    {
        if (anchors.Count == 0)
            throw new InvalidOperationException("At least one derivation anchor is required.");
    }
}

public sealed class AnalysisRun
{
    internal AnalysisRun(AlgorithmAnchor root, params IReadOnlyList<IAnalyzer> analyzers)
    {
        foreach (var analyzer in analyzers)
            analyzer.Bind(root);
    }

    public Task CompleteAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}

public static class AlgorithmRunCreation
{
    public static AnalysisRun CreateRun(this AlgorithmAnchor algorithm, params IReadOnlyList<IAnalyzer> analyzers) =>
        new(algorithm, analyzers);
}

public static class Analyzer
{
    public static TraceAnalyzer<TResult> Trace<TValue, TResult>(
        IMeasurement<AlgorithmObservation, TValue> measurement,
        IAggregation<TValue, TResult> aggregation,
        AlgorithmAnchor at,
        IReadOnlyList<object>? clocks = null,
        Across? across = null,
        Recording? recording = null) =>
        Trace(measurement.Read, aggregation, at, clocks, across, recording);

    public static TraceAnalyzer<TResult> Trace<TValue, TResult>(
        Func<AlgorithmObservation, IReadOnlyList<TValue>> measure,
        IAggregation<TValue, TResult> aggregation,
        AlgorithmAnchor at,
        IReadOnlyList<object>? clocks = null,
        Across? across = null,
        Recording? recording = null)
    {
        return new(
            across ?? Across.EachFiring(),
            recording ?? Recording.EveryFiring(),
            clocks ?? [],
            (_, trace) =>
            {
                if (at is DetachedAlgorithmAnchor)
                    throw new InvalidOperationException("The observation anchor is not part of the run's execution graph.");

                trace.Record(new Moment(1, 100), aggregation.Example);
            });
    }

    public static TraceAnalyzer<TResult> Trace<TValue, TResult>(
        Func<CrossoverObservation, IReadOnlyList<TValue>> measure,
        IAggregation<TValue, TResult> aggregation,
        CrossoverAnchor at,
        IReadOnlyList<object>? clocks = null,
        Across? across = null,
        Recording? recording = null) =>
        new(
            across ?? Across.EachFiring(),
            recording ?? Recording.EveryFiring(),
            clocks ?? [],
            (_, trace) => trace.Record(new Moment(1, 100), aggregation.Example));

    public static TraceAnalyzer<TResult> Trace<TValue, TResult>(
        Func<DynamicEvaluatorObservation, IReadOnlyList<TValue>> measure,
        IAggregation<TValue, TResult> aggregation,
        DynamicEvaluatorAnchor at,
        IReadOnlyList<object>? clocks = null,
        Across? across = null,
        Recording? recording = null) =>
        new(
            across ?? Across.EachFiring(),
            recording ?? Recording.EveryFiring(),
            clocks ?? [],
            (_, trace) => trace.Record(new Moment(1, 100, 7), aggregation.Example));

    public static TraceAnalyzer<BestMedianWorst> TraceBestMedianWorst(
        AlgorithmAnchor at,
        IReadOnlyList<object>? clocks = null) =>
        Trace(new ObjectiveVectorsMeasurement(), Aggregate.BestMedianWorst(), at, clocks);

    public static TrackedLineage TrackLineage(
        IReadOnlyList<object> at,
        IEqualityComparer<string> comparer) => new(at);
}

public readonly record struct TrialTrace<T>(string Key, TraceAnalyzer<T> Trace);

public sealed class ExperimentTrace<T>(IReadOnlyList<TrialTrace<T>> trials)
{
    public IReadOnlyList<TrialTrace<T>> Trials { get; } = trials;
}

public sealed class ExperimentAnalysisRun
{
    private readonly IReadOnlyList<string> trialKeys;

    private ExperimentAnalysisRun(IReadOnlyList<string> trialKeys) => this.trialKeys = trialKeys;
    public static ExperimentAnalysisRun Create(params IReadOnlyList<string> trialKeys) => new(trialKeys.ToArray());

    public ExperimentTrace<TResult> Trace<TValue, TResult>(
        IMeasurement<AlgorithmObservation, TValue> measurement,
        IAggregation<TValue, TResult> aggregation) =>
        Trace(measurement.Read, aggregation);

    public ExperimentTrace<TResult> Trace<TValue, TResult>(
        Func<AlgorithmObservation, IReadOnlyList<TValue>> measure,
        IAggregation<TValue, TResult> aggregation)
    {
        var trials = trialKeys.Select((key, index) =>
        {
            var trace = new TraceAnalyzer<TResult>(Across.EachFiring(), Recording.EveryFiring(), [], (_, _) => { });
            trace.Record(new Moment(1, 100L * (index + 1)), aggregation.Example);
            return new TrialTrace<TResult>(key, trace);
        }).ToArray();
        return new ExperimentTrace<TResult>(trials);
    }
}

#endregion

#pragma warning restore S2326
#pragma warning restore S2325
