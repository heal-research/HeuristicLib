using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Problems;
using System.Text.Json;
using HEAL.HeuristicLib.Encodings.Permutations;
using HEAL.HeuristicLib.Problems.Dynamic;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Tests.Scenarios.Problems.Dynamic;

public partial class AutoEcPaperScenarioTests
{
    private static void AssertScenarioProducesExperimentSignals(
        AutoEcScenarioResult result,
        int minQualityEpochs,
        int minBbcpChanges)
    {
        result.Output.FinalPopulationSize.ShouldBeGreaterThan(0);
        result.Output.QualityCurve.Count.ShouldBeGreaterThanOrEqualTo(minQualityEpochs);
        result.Output.QualityCurve.Select(entry => entry.Timing.Epoch).Distinct().Count()
              .ShouldBeGreaterThanOrEqualTo(minQualityEpochs);
        result.Output.BestBeforeChange.Count.ShouldBeGreaterThanOrEqualTo(minBbcpChanges);
        double.IsFinite(result.Output.BestBeforeChangePerformance).ShouldBeTrue();
        result.Output.RacingQualityTrace.Count.ShouldBeGreaterThan(0);
        var expectedContenderSlots = Enumerable.Range(0, result.Input.Racer.NoRacers).ToArray();
        result.Output.RacingQualityTrace.ShouldAllBe(point =>
            point.ContenderIndex >= 0 && point.ContenderIndex < result.Input.Racer.NoRacers);
        var postBurnInTrace = result.Output.RacingQualityTrace
                                    .Where(point => point.Epoch >= result.Input.Racer.BurnInEpochs)
                                    .ToArray();
        if (postBurnInTrace.Length > 0)
        {
            postBurnInTrace.GroupBy(point => point.Race)
                           .Select(group => group.Select(point => point.ContenderIndex)
                                                 .Distinct()
                                                 .Order()
                                                 .ToArray())
                           .ShouldContain(contenderSlots => contenderSlots.SequenceEqual(expectedContenderSlots));
        }

        var json = JsonSerializer.Serialize(result, JsonOptions);
        using var parsed = JsonDocument.Parse(json);
        parsed.RootElement.GetProperty(nameof(AutoEcScenarioResult.SchemaVersion)).GetString()
              .ShouldBe("auto-ec-scenario-result/v1");
    }

    private static async Task<AutoEcScenarioResult> RunScenario<TCandidate, TSearchSpace, TProblem>(
        AutoEcPaperScenario<TCandidate, TSearchSpace, TProblem> scenario)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : DynamicProblem<TProblem, TCandidate, TSearchSpace>
    {
        using var problem = scenario.CreateProblem();
        var evaluator = scenario.CreateEvaluator(problem);
        var racing = scenario.CreateRacingAlgorithm(problem, evaluator);
        var epoch = Clock.FromEpoch(problem);
        var evaluations = new ProblemEvaluationClock(() => problem.TotalEvaluations);
        var qualityCurve = Analyzer.Trace(evaluator, observation => observation.Candidates.ToEvaluated(observation.ObjectiveVectors), Aggregate.Best<TCandidate>(), [epoch, evaluations]);
        var bbcp = scenario.CreateBestBeforeChangePerformanceAnalysis(problem, evaluator);
        var run = racing.CreateRun(problem, RandomNumberGenerator.Create(scenario.RunSeed))
                        .AddAspect(qualityCurve).AddAspect(bbcp);
        var stream = run.Stream(cancellationToken: TestContext.Current.CancellationToken);
        var finalState = await RunUntilEpochChanges(
            stream,
            problem,
            scenario.TargetEpochChanges,
            TestContext.Current.CancellationToken);


        //test some result aspects
        finalState.Population.EvaluatedCandidates.Count.ShouldBeGreaterThan(0);
        finalState.Population.EvaluatedCandidates.ShouldAllBe(candidate =>
            problem.SearchSpace.Contains(candidate.Candidate));

        return CreateScenarioResult(scenario, problem, finalState,
            qualityCurve.Snapshot().GroupBy(point => point.At(epoch))
                .Select(group => group.MinBy(point => point.Value.ObjectiveVector, problem.Objective.TotalOrderComparer))
                .Select(point => new AutoEcQualityCurvePoint(
                new AutoEcEvaluationTiming(point.At(evaluations), point.At(epoch), true), AutoEcScenarioResultExport.Export(point.Value.ObjectiveVector), FormatCandidate(point.Value.Candidate))).ToArray(), bbcp);
    }

    private sealed class ProblemEvaluationClock(Func<long> readEvaluations) : Clock<long>
    {
        protected override long ReadTime() => readEvaluations();
    }

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private static AutoEcScenarioResult CreateScenarioResult<TCandidate, TSearchSpace, TProblem>(
        AutoEcPaperScenario<TCandidate, TSearchSpace, TProblem> scenario,
        TProblem problem,
        PopulationState<TCandidate> finalState,
        IReadOnlyList<AutoEcQualityCurvePoint> qualityResult,
        BestBeforeChangePerformanceAnalyzer<TCandidate, TSearchSpace, TProblem> bbcpResult)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : DynamicProblem<TProblem, TCandidate, TSearchSpace>
    {
        var input = new AutoEcScenarioInput(
            scenario.ScenarioName,
            scenario.CreateProblemDescription(),
            new AutoEcSeedPlan(scenario.ProblemSeed, scenario.RunSeed),
            new AutoEcRacerParameters(
                scenario.NoRacers,
                scenario.BurnInEpochs,
                scenario.EarlyTerminationStrength,
                scenario.HallOfFameStrength,
                scenario.MinimumModelObservationCount,
                scenario.ModelObservationInterval,
                scenario.MetaMutationStrength,
                scenario.TargetEpochChanges),
            scenario.CreateBaseAlgorithmDescription(),
            scenario.HyperParameterRanges);

        var output = new AutoEcScenarioOutput(
            problem.CurrentEpoch,
            problem.TotalEvaluations,
            finalState.Population.EvaluatedCandidates.Count,
            bbcpResult.Performance,
            qualityResult,
            bbcpResult.BestBeforeChange.Select(point => new AutoEcBestBeforeChangePoint(
                qualityResult.Single(entry => entry.Timing.Epoch == point.Epoch).Timing,
                AutoEcScenarioResultExport.Export(point.ObjectiveVector),
                point.ObjectiveValue,
                FormatCandidate(point.Candidate))).ToArray(),
            scenario.QualityTrace.Points.Select(point => new AutoEcRacingQualityTracePoint(
                point.Race,
                point.Epoch,
                point.ContenderIndex,
                point.IsIncumbent,
                point.Origin.ToString(),
                AutoEcScenarioResultExport.Export(point.Configuration),
                AutoEcScenarioResultExport.Export(point.ObjectiveVector),
                point.ObjectiveValue,
                point.ProblemEvaluationCount,
                point.ContenderEvaluationCount,
                FormatCandidate(point.Candidate))).ToArray(),
            scenario.QualityTrace.Races.Select(race => new AutoEcRacingRaceSummary(
                race.Race,
                race.Epoch,
                race.TerminationReason.ToString(),
                race.WinnerContenderIndex,
                AutoEcScenarioResultExport.Export(race.WinnerConfiguration),
                race.ProblemEvaluationCount)).ToArray());

        return new AutoEcScenarioResult("auto-ec-scenario-result/v1", input, output);
    }

    private static string FormatCandidate<TCandidate>(TCandidate candidate) => candidate?.ToString() ?? string.Empty;

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "HEAL.HeuristicLib.slnx")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find the repository root.");
    }

    private static async Task<TSearchState> RunUntilEpochChanges<TProblem, TCandidate, TSearchSpace, TSearchState>(
        IAsyncEnumerable<TSearchState> stream,
        DynamicProblem<TProblem, TCandidate, TSearchSpace> problem,
        int epochChanges,
        CancellationToken cancellationToken)
        where TProblem : Problem<TProblem, TCandidate, TSearchSpace>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TSearchState : class
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(epochChanges);

        var initialEpoch = problem.CurrentEpoch;
        TSearchState? finalState = null;
        await using var enumerator = stream.GetAsyncEnumerator(cancellationToken);
        while (problem.CurrentEpoch - initialEpoch < epochChanges)
        {
            var hasNext = await enumerator.MoveNextAsync();
            hasNext.ShouldBeTrue();
            finalState = enumerator.Current;
        }

        return finalState ?? throw new InvalidOperationException("The stream did not produce a state.");
    }

    private sealed record PermutationCacheKeySelector : ICacheKeySelector<Permutation, string>
    {
        public static PermutationCacheKeySelector Instance { get; } = new();

        public string SelectKey(Permutation candidate) => string.Join(",", candidate);
    }

    private static string? FindLocalFile(params string[] relativePathParts)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(new[] { directory.FullName }.Concat(relativePathParts).ToArray());
            if (File.Exists(candidate))
                return candidate;

            directory = directory.Parent;
        }

        return null;
    }

    private static string? FindLocalQapFile(string fileName) =>
        FindLocalFile("qap", fileName)
        ?? FindLocalFile("qaplib", fileName)
        ?? FindLocalFile("QAPLIB", fileName);

    private static string BuildRunFileName(
        string problemKind,
        string problemInstance,
        string baseAlgorithm,
        int repetition,
        string? qualifier = null)
    {
        var parts =
            new[] { problemKind, problemInstance, baseAlgorithm, qualifier, $"rep-{repetition:00}" }.Where(part =>
                !string.IsNullOrWhiteSpace(part));

        return SanitizeFileName(string.Join("_", parts)) + ".json";
    }

    private static string SanitizeFileName(string value)
    {
        var invalidCharacters = Path.GetInvalidFileNameChars();
        return string.Concat(value.Select(c => invalidCharacters.Contains(c) ? '_' : c));
    }
}
