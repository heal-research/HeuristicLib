using System.Collections.ObjectModel;

namespace HEAL.HeuristicLib.Tests.ApiUsageSpecs.Analysis;

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
        var run = AnalysisRun.Create(new AlgorithmAnchor());
        var quality = run.TrackSeries(Measure.Quality(), Aggregate.BestMedianWorst());

        quality.SampleCount.ShouldBe(1);
        quality.Latest!.Value.Value.Best.ShouldBe(0.5);

        var duringExecution = quality.ByIteration();
        await run.CompleteAsync(TestContext.Current.CancellationToken);

        duringExecution.ShouldHaveSingleItem();
        quality.ByEvaluations().Single().Coordinate.ShouldBe(100);
    }

    [Fact]
    public void Snapshot_IsStableWhenTheAnalysisCollectsMoreData()
    {
        var run = AnalysisRun.Create(new AlgorithmAnchor());
        var quality = run.TrackSeries(Measure.Quality(), Aggregate.Best());
        var firstSnapshot = quality.Snapshot();

        quality.Record(new AnalysisStamp(2, 200), 0.25);

        firstSnapshot.ShouldHaveSingleItem();
        quality.SampleCount.ShouldBe(2);
        quality.Snapshot().Count.ShouldBe(2);
    }

    [Fact]
    public void CommonAnalysis_IsAShortcutForTrackSeries()
    {
        var run = AnalysisRun.Create(new AlgorithmAnchor());

        var shortcut = run.TrackBestMedianWorst();
        var composed = run.TrackSeries(Measure.Quality(), Aggregate.BestMedianWorst());

        shortcut.ShouldBeOfType<TrackedSeries<BestMedianWorst>>();
        composed.ShouldBeOfType<TrackedSeries<BestMedianWorst>>();
    }

    [Fact]
    public void MeasurementsAndAggregations_ComposeWithoutExplicitTypeArguments()
    {
        var run = AnalysisRun.Create(new AlgorithmAnchor());

        var scalar = run.TrackSeries(Measure.TreeLength(), Aggregate.MinMeanMax());
        var frequency = run.TrackSeries(Measure.VariablesUsed(), Aggregate.Frequency());
        var pairwise = run.TrackSeries(
            Measure.PairwiseSimilarity(SimilarityCalculator.Levenshtein()),
            Aggregate.MinMeanMax(),
            recording: Recording.EveryNth(10));

        scalar.ShouldBeOfType<TrackedSeries<MinMeanMax>>();
        frequency.ShouldBeOfType<TrackedSeries<IReadOnlyDictionary<string, int>>>();
        pairwise.Recording.ShouldBe(Recording.EveryNth(10));
    }

    [Fact]
    public void AcrossFiringsAndRecordingPolicy_AreIndependentChoices()
    {
        var run = AnalysisRun.Create(new AlgorithmAnchor());

        var everyFiring = run.TrackSeries(Measure.Quality(), Aggregate.Best());
        var onlyImprovements = run.TrackSeries(
            Measure.Quality(),
            Aggregate.Best(),
            across: Across.RunningBest(),
            recording: Recording.OnChange());

        everyFiring.Across.ShouldBe(Across.EachFiring());
        everyFiring.Recording.ShouldBe(Recording.EveryFiring());
        onlyImprovements.Across.ShouldBe(Across.RunningBest());
        onlyImprovements.Recording.ShouldBe(Recording.OnChange());
    }

    [Fact]
    public void SeparateAttachments_CreateIndependentRunBoundAnalyses()
    {
        var run = AnalysisRun.Create(new AlgorithmAnchor());

        var explore = run.TrackSeries(Measure.TreeLength(), Aggregate.MinMeanMax(), at: new AlgorithmAnchor());
        var refine = run.TrackSeries(Measure.TreeLength(), Aggregate.MinMeanMax(), at: new AlgorithmAnchor());

        explore.ShouldNotBeSameAs(refine);
        explore.Snapshot().ShouldNotBeSameAs(refine.Snapshot());
    }

    [Fact]
    public void CandidateMeasurement_AttachesToACrossoverBoundary()
    {
        var run = AnalysisRun.Create(new AlgorithmAnchor());

        var offspringSize = run.TrackSeries(
            Measure.TreeLength(),
            Aggregate.MinMeanMax(),
            at: new CrossoverAnchor());

        offspringSize.Snapshot().ShouldHaveSingleItem();
        typeof(ISupplies<IObjectiveReadings>).IsAssignableFrom(typeof(CrossoverAnchor)).ShouldBeFalse();

        // This is intentionally absent because it must not compile:
        // run.TrackSeries(Measure.Quality(), Aggregate.Best(), at: new CrossoverAnchor());
    }

    [Fact]
    public void FrequencySeries_TransposesIntoOneStableSeriesPerKey()
    {
        var run = AnalysisRun.Create(new AlgorithmAnchor());
        var variables = run.TrackSeries(Measure.VariablesUsed(), Aggregate.Frequency());

        var byVariable = variables.Transpose();

        byVariable.Keys.ShouldBe(["x1", "x2"], ignoreOrder: true);
        byVariable["x1"].Single().Value.ShouldBe(3);
    }

    [Fact]
    public void DomainCoordinate_IsContributedByTheAnchorStamp()
    {
        var run = AnalysisRun.Create(new AlgorithmAnchor());

        var perEpoch = run.TrackSeries(
            Measure.Quality(),
            Aggregate.Best(),
            at: new DynamicEvaluatorAnchor());

        perEpoch.ByEpoch().Single().Coordinate.ShouldBe(7);
    }

    [Fact]
    public void DetachedAnchor_IsAConfigurationError()
    {
        var run = AnalysisRun.Create(new AlgorithmAnchor());

        Should.Throw<InvalidOperationException>(() => run.TrackSeries(
            Measure.Quality(),
            Aggregate.Best(),
            at: new DetachedAlgorithmAnchor()));
    }

    [Fact]
    public async Task Lineage_IsARunBoundAnalysisWithAnAnalysisSpecificReadingApi()
    {
        var run = AnalysisRun.Create(new AlgorithmAnchor());
        var lineage = run.TrackLineage(
            at: [new CreatorAnchor(), new CrossoverAnchor(), new MutatorAnchor()],
            comparer: StringComparer.Ordinal);

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

        var quality = experiment.TrackSeries(Measure.Quality(), Aggregate.Best());

        quality.Trials.Select(trial => trial.Key).ShouldBe(["small", "large"]);
        quality.Trials[0].Series.ShouldNotBeSameAs(quality.Trials[1].Series);
        quality.Trials[0].Series.Snapshot().ShouldHaveSingleItem();
    }
}

#region Desired API prototype

public interface ISupplies<out TSupply>;
public interface IObjectiveReadings;
public interface ICandidateReadings;
public interface IDerivation : ICandidateReadings;
public interface IEvaluatedPopulation : IObjectiveReadings, ICandidateReadings;

public sealed record AlgorithmAnchor : ISupplies<IEvaluatedPopulation>;
public sealed record DetachedAlgorithmAnchor : ISupplies<IEvaluatedPopulation>;
public sealed record DynamicEvaluatorAnchor : ISupplies<IObjectiveReadings>;
public sealed record CreatorAnchor : ISupplies<IDerivation>;
public sealed record CrossoverAnchor : ISupplies<IDerivation>;
public sealed record MutatorAnchor : ISupplies<IDerivation>;

public readonly record struct AnalysisStamp(long Iteration, long Evaluations, int? Epoch = null);
public readonly record struct Sample<T>(AnalysisStamp Stamp, T Value);
public readonly record struct CoordinateValue<TCoordinate, TValue>(TCoordinate Coordinate, TValue Value);

public sealed class SeriesSnapshot<T>(IReadOnlyList<Sample<T>> samples) : ReadOnlyCollection<Sample<T>>(samples.ToArray());

public sealed class TrackedSeries<T>(Across across, Recording recording)
{
    private readonly Lock sync = new();
    private readonly List<Sample<T>> samples = [];

    public Across Across { get; } = across;
    public Recording Recording { get; } = recording;

    public int SampleCount
    {
        get { lock (sync) return samples.Count; }
    }

    public Sample<T>? Latest
    {
        get { lock (sync) return samples.Count == 0 ? null : samples[^1]; }
    }

    internal void Record(AnalysisStamp stamp, T value)
    {
        lock (sync)
            samples.Add(new Sample<T>(stamp, value));
    }

    public SeriesSnapshot<T> Snapshot()
    {
        lock (sync)
            return new SeriesSnapshot<T>(samples);
    }

    public IReadOnlyList<CoordinateValue<long, T>> ByIteration() =>
        Array.AsReadOnly(Snapshot().Select(sample => new CoordinateValue<long, T>(sample.Stamp.Iteration, sample.Value)).ToArray());

    public IReadOnlyList<CoordinateValue<long, T>> ByEvaluations() =>
        Array.AsReadOnly(Snapshot().Select(sample => new CoordinateValue<long, T>(sample.Stamp.Evaluations, sample.Value)).ToArray());

    public IReadOnlyList<CoordinateValue<int, T>> ByEpoch() =>
        Array.AsReadOnly(Snapshot().Select(sample => new CoordinateValue<int, T>(
            sample.Stamp.Epoch ?? throw new InvalidOperationException("The series has no epoch coordinate."),
            sample.Value)).ToArray());
}

public static class SeriesViews
{
    public static IReadOnlyDictionary<TKey, SeriesSnapshot<TValue>> Transpose<TKey, TValue>(
        this TrackedSeries<IReadOnlyDictionary<TKey, TValue>> series)
        where TKey : notnull =>
        new ReadOnlyDictionary<TKey, SeriesSnapshot<TValue>>(series.Snapshot()
            .SelectMany(sample => sample.Value.Select(entry => (sample.Stamp, entry.Key, entry.Value)))
            .GroupBy(entry => entry.Key)
            .ToDictionary(
                group => group.Key,
                group => new SeriesSnapshot<TValue>(group.Select(entry => new Sample<TValue>(entry.Stamp, entry.Value)).ToArray())));
}

public interface IMeasurement<in TSupply, out TValue> { TValue Example { get; } }
public sealed record Measurement<TSupply, TValue>(TValue Example) : IMeasurement<TSupply, TValue>;
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

public static class Measure
{
    public static Measurement<IObjectiveReadings, double> Quality() => new(0.5);
    public static Measurement<ICandidateReadings, double> TreeLength() => new(12);
    public static Measurement<ICandidateReadings, string> VariablesUsed() => new("x1");
    public static Measurement<ICandidateReadings, double> PairwiseSimilarity(SimilarityCalculator _) => new(0.75);
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

public sealed class TrackedLineage
{
    private readonly ReadOnlyCollection<string> roots = Array.AsReadOnly(["root"]);
    private readonly ReadOnlyCollection<string> descendants = Array.AsReadOnly(["child"]);

    public int RootCount => roots.Count;
    public LineageSnapshot Snapshot() => new([.. roots], [1]);
    public IReadOnlyList<string> DescendantsOf(string root) => root == "root" ? descendants : [];
}

public sealed class AnalysisRun
{
    private readonly AlgorithmAnchor root;

    private AnalysisRun(AlgorithmAnchor root) => this.root = root;
    public static AnalysisRun Create(AlgorithmAnchor root) => new(root);

    public TrackedSeries<TResult> TrackSeries<TValue, TResult>(
        IMeasurement<IEvaluatedPopulation, TValue> measurement,
        IAggregation<TValue, TResult> aggregation,
        Across? across = null,
        Recording? recording = null) =>
        TrackSeries(measurement, aggregation, root, across, recording);

    public TrackedSeries<TResult> TrackSeries<TSupply, TValue, TResult>(
        IMeasurement<TSupply, TValue> measurement,
        IAggregation<TValue, TResult> aggregation,
        ISupplies<TSupply> at,
        Across? across = null,
        Recording? recording = null)
    {
        if (at is DetachedAlgorithmAnchor)
            throw new InvalidOperationException("The observation anchor is not part of the run's execution graph.");

        var series = new TrackedSeries<TResult>(across ?? Across.EachFiring(), recording ?? Recording.EveryFiring());
        int? epoch = at is DynamicEvaluatorAnchor ? 7 : null;
        series.Record(new AnalysisStamp(1, 100, epoch), aggregation.Example);
        return series;
    }

    public TrackedSeries<BestMedianWorst> TrackBestMedianWorst() =>
        TrackSeries(Measure.Quality(), Aggregate.BestMedianWorst());

    public TrackedLineage TrackLineage(IReadOnlyList<ISupplies<IDerivation>> at, IEqualityComparer<string> comparer)
    {
        if (at.Count == 0)
            throw new InvalidOperationException("At least one derivation anchor is required.");
        return new TrackedLineage();
    }

    public Task CompleteAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}

public readonly record struct TrialSeries<T>(string Key, TrackedSeries<T> Series);

public sealed class TrackedExperimentSeries<T>(IReadOnlyList<TrialSeries<T>> trials)
{
    public IReadOnlyList<TrialSeries<T>> Trials { get; } = trials;
}

public sealed class ExperimentAnalysisRun
{
    private readonly IReadOnlyList<string> trialKeys;

    private ExperimentAnalysisRun(IReadOnlyList<string> trialKeys) => this.trialKeys = trialKeys;
    public static ExperimentAnalysisRun Create(params IReadOnlyList<string> trialKeys) => new(trialKeys.ToArray());

    public TrackedExperimentSeries<TResult> TrackSeries<TSupply, TValue, TResult>(
        IMeasurement<TSupply, TValue> measurement,
        IAggregation<TValue, TResult> aggregation)
    {
        var trials = trialKeys.Select((key, index) =>
        {
            var series = new TrackedSeries<TResult>(Across.EachFiring(), Recording.EveryFiring());
            series.Record(new AnalysisStamp(1, 100L * (index + 1)), aggregation.Example);
            return new TrialSeries<TResult>(key, series);
        }).ToArray();
        return new TrackedExperimentSeries<TResult>(trials);
    }
}

#endregion

#pragma warning restore S2326
#pragma warning restore S2325
