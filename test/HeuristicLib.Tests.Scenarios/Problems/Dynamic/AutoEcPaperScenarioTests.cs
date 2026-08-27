using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Problems;
using System.Collections.Concurrent;
using System.Text.Json;
using HEAL.HeuristicLib.Algorithms.AutoEC;
using HEAL.HeuristicLib.Data;
using HEAL.HeuristicLib.Encodings.Composite;
using HEAL.HeuristicLib.Encodings.IntegerVectors;
using HEAL.HeuristicLib.Encodings.Permutations;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Encodings.SymbolicExpressions;
using HEAL.HeuristicLib.MachineLearning;
using HEAL.HeuristicLib.Problems.Dynamic;
using HEAL.HeuristicLib.Problems.Dynamic.SlidingWindowRegression;
using HEAL.HeuristicLib.Problems.MachineLearning;
using HEAL.HeuristicLib.Problems.QuadraticAssignment;
using HEAL.HeuristicLib.Problems.TravelingSalesman;
using HEAL.HeuristicLib.SearchSpaces;
using MetaOptimizationGenotype =
    HEAL.HeuristicLib.Encodings.Composite.CompositeGenotype<HEAL.HeuristicLib.Encodings.RealVectors.RealVector,
        HEAL.HeuristicLib.Encodings.IntegerVectors.IntegerVector>;
using MetaOptimizationSearchSpace =
    HEAL.HeuristicLib.Encodings.Composite.CompositeSearchSpace<HEAL.HeuristicLib.Encodings.RealVectors.RealVector,
        HEAL.HeuristicLib.Encodings.RealVectors.BoundedRealVectorSearchSpace,
        HEAL.HeuristicLib.Encodings.IntegerVectors.IntegerVector,
        HEAL.HeuristicLib.Encodings.IntegerVectors.IntegerVectorSearchSpace>;
using IntegerVectorUniformDistributedCreator =
    HEAL.HeuristicLib.Encodings.IntegerVectors.UniformDistributedCreator;
using RealVectorUniformDistributedCreator =
    HEAL.HeuristicLib.Encodings.RealVectors.UniformDistributedCreator;

namespace HEAL.HeuristicLib.Tests.Scenarios.Problems.Dynamic;

public class AutoEcPaperScenarioTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    public async Task RunUntilEpochChanges_CountsAppliedUpdatesFromItsStartingEpoch(int updatesPerMove)
    {
        using var problem = new MovingPeaksAutoEcScenario().CreateProblem();
        for (var epoch = 0; epoch < 3; epoch++)
            problem.UpdateOnce();
        var moves = 0;
        var disposed = false;

        var finalState = await RunUntilEpochChanges(AdvanceEpochs(), problem, 2, TestContext.Current.CancellationToken);

        var expectedMoves = updatesPerMove == 1 ? 2 : 1;
        moves.ShouldBe(expectedMoves);
        finalState.ShouldBe($"move {expectedMoves}");
        problem.CurrentEpoch.ShouldBe(3 + expectedMoves * updatesPerMove);
        disposed.ShouldBeTrue();

        async IAsyncEnumerable<string> AdvanceEpochs()
        {
            try
            {
                for (var move = 0; move < 3; move++)
                {
                    moves++;
                    for (var update = 0; update < updatesPerMove; update++)
                        problem.UpdateOnce();
                    yield return $"move {moves}";
                    await Task.CompletedTask;
                }
            }
            finally
            {
                disposed = true;
            }
        }
    }

    private const int PrototypeRepetitions = 10;
    private const int PrototypeTargetEpochChanges = 300;
    private const int PrototypeEpochLength = 500;
    private const int MatrixScaffoldRepetitions = 3;
    private const int MatrixScaffoldTargetEpochChanges = 4;
    private const int MatrixScaffoldEpochLength = 500;
    private static readonly string[] MatrixScaffoldTspFiles =
    [
        "burma14.tsp", "bayg29.tsp", "att48.tsp"
    ];
    private static readonly string[] PrototypeTspFiles =
    [
        "burma14.tsp", "bayg29.tsp", "bays29.tsp", "att48.tsp", "berlin52.tsp", "kroA100.tsp", "kroB150.tsp",
        "pr299.tsp"
    ];
    private static readonly int[] PrototypeRacerVariants = [1, 2];
    private static readonly AutoEcChildAlgorithmKind[] PrototypeAlgorithmVariants =
    [
        AutoEcChildAlgorithmKind.GeneticAlgorithm,
        AutoEcChildAlgorithmKind.HypermutationGeneticAlgorithm,
        AutoEcChildAlgorithmKind.ChangeTriggeredRandomImmigrantGeneticAlgorithm
    ];

    private sealed record PrototypeRunSpec(string TspFile, int InstanceIndex, int Repetition, int NoRacers);

    private sealed record MatrixScaffoldRunSpec(
        string ProblemKey,
        int ProblemIndex,
        string InstanceKey,
        int Repetition,
        AutoEcChildAlgorithmKind AlgorithmKind,
        string? TspFile = null,
        string? QapFile = null);

    private enum AutoEcChildAlgorithmKind
    {
        GeneticAlgorithm,
        HypermutationGeneticAlgorithm,
        ChangeTriggeredRandomImmigrantGeneticAlgorithm
    }

    private static string FormatAlgorithmKind(AutoEcChildAlgorithmKind algorithmKind) =>
        algorithmKind switch
        {
            AutoEcChildAlgorithmKind.HypermutationGeneticAlgorithm => "hypermutation-ga",
            AutoEcChildAlgorithmKind.ChangeTriggeredRandomImmigrantGeneticAlgorithm => "change-triggered-riga",
            _ => "ga"
        };

    [Fact]
    public async Task DynamicRacingGa_OnActivatedTsp_UsesPaperLikeScenario()
    {
        var tspFile = FindLocalFile("tsp", "eil51.tsp");
        var concordePath = FindLocalFile("for_Agent", "Concorde", "executablescygwin", "concorde.exe");
        if (tspFile is null || concordePath is null)
            return;

        var scenario = new ActivatedTspAutoEcScenario(tspFile, concordePath);
        var result = await RunScenario(scenario);

        AssertScenarioProducesExperimentSignals(result, minQualityEpochs: 2, minBbcpChanges: 1);
    }

    [Fact]
    public async Task DynamicRacingGa_OnActivatedTsp_WritesPaperPrototypeResults()
    {
        var concordePath = FindLocalFile("for_Agent", "Concorde", "executablescygwin", "concorde.exe");
        if (concordePath is null)
            return;

        var tspFiles = PrototypeTspFiles.Select(name => FindLocalFile("tsp", name))
                                        .Where(path => path is not null)
                                        .Select(path => path!)
                                        .ToArray();
        if (tspFiles.Length == 0)
            return;

        var batchName = $"activated-tsp-prototype-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}";
        var outputDirectory = Path.Combine(FindRepositoryRoot(), "artifacts", "autoec-paper-results", batchName);
        var runsDirectory = Path.Combine(outputDirectory, "runs");
        Directory.CreateDirectory(runsDirectory);

        var workItems = (
            from instance in tspFiles.Select((path, index) => new
            {
                path,
                index
            })
            from repetition in Enumerable.Range(0, PrototypeRepetitions)
            from noRacers in PrototypeRacerVariants
            select new PrototypeRunSpec(instance.path, instance.index, repetition, noRacers)
        ).ToArray();
        var manifestRuns = new ConcurrentBag<AutoEcScenarioBatchRun>();
        await Parallel.ForEachAsync(
            workItems,
            TestContext.Current.CancellationToken,
            async (workItem, cancellationToken) =>
            {
                var problemSeed = 2024 + workItem.InstanceIndex * 100 + workItem.Repetition;
                var runSeed = 123 + workItem.InstanceIndex * 100 + workItem.Repetition;
                var scenario = new ActivatedTspAutoEcScenario(
                    workItem.TspFile,
                    concordePath,
                    problemSeed,
                    runSeed,
                    targetEpochChanges: PrototypeTargetEpochChanges,
                    scenarioName: $"activated-tsp-auto-ec-prototype-r{workItem.NoRacers}",
                    noRacers: workItem.NoRacers,
                    epochLength: PrototypeEpochLength);
                var result = await RunScenario(scenario);
                AssertScenarioProducesExperimentSignals(result, minQualityEpochs: 1, minBbcpChanges: 1);

                var fileName =
                    $"{Path.GetFileNameWithoutExtension(workItem.TspFile)}_r{workItem.NoRacers}_rep-{workItem.Repetition:00}.json";
                var relativeFile = Path.Combine("runs", fileName);
                await File.WriteAllTextAsync(
                    Path.Combine(runsDirectory, fileName),
                    JsonSerializer.Serialize(result, JsonOptions),
                    cancellationToken);
                manifestRuns.Add(new AutoEcScenarioBatchRun(
                    result.Input.ScenarioName,
                    result.Input.Problem.Instance ?? Path.GetFileName(workItem.TspFile),
                    workItem.Repetition,
                    result.Input.Seeds.ProblemSeed,
                    result.Input.Seeds.RunSeed,
                    relativeFile.Replace(Path.DirectorySeparatorChar, '/')));
            });
        var orderedManifestRuns = manifestRuns
                                  .OrderBy(run => run.ProblemInstance)
                                  .ThenBy(run => run.Repetition)
                                  .ThenBy(run => run.ScenarioName)
                                  .ToArray();

        var manifest = new AutoEcScenarioBatchManifest(
            "auto-ec-scenario-batch/v1",
            batchName,
            DateTimeOffset.UtcNow,
            orderedManifestRuns.Length,
            orderedManifestRuns);
        await File.WriteAllTextAsync(
            Path.Combine(outputDirectory, "manifest.json"),
            JsonSerializer.Serialize(manifest, JsonOptions),
            TestContext.Current.CancellationToken);

        orderedManifestRuns.Length.ShouldBe(tspFiles.Length * PrototypeRepetitions * PrototypeRacerVariants.Length);
    }

    private record ExperimentContext(
        ConcurrentBag<AutoEcScenarioBatchRun> ManifestRuns,
        string? ConcordePath,
        string RunsDirectory,
        string OutputDirectory,
        string BatchName);

    [Fact]
    public async Task DynamicRacingMatrix_WritesPaperScaffoldResults()
    {
        var concordePath = FindLocalFile("for_Agent", "Concorde", "executablescygwin", "concorde.exe");
        var tspFiles = MatrixScaffoldTspFiles.Select(name => FindLocalFile("tsp", name))
                                             .Where(file => file is not null)
                                             .Cast<string>()
                                             .ToArray();
        var qapFiles = FindLocalFiles("qap", "*.dat")
                       .Concat(FindLocalFiles("qaplib", "*.dat"))
                       .Concat(FindLocalFiles("QAPLIB", "*.dat"))
                       .Distinct(StringComparer.OrdinalIgnoreCase)
                       .OrderBy(file => file)
                       .Take(10)
                       .ToArray();
        var batchName = $"auto-ec-matrix-scaffold-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}";
        var outputDirectory = Path.Combine(FindRepositoryRoot(), "artifacts", "autoec-paper-results", batchName);
        var runsDirectory = Path.Combine(outputDirectory, "runs");
        Directory.CreateDirectory(runsDirectory);
        var manifestRuns = new ConcurrentBag<AutoEcScenarioBatchRun>();
        var context = new ExperimentContext(manifestRuns, concordePath, runsDirectory, outputDirectory, batchName);

        var problemSpecs = new List<(string ProblemKey, string InstanceKey, string? TspFile, string? QapFile)>
        {
            ("moving-peaks", "default", null, null),
            ("sliding-window-symreg", "synthetic-sine-trend", null, null)
        };

        problemSpecs.AddRange(qapFiles.Select(file => (
            "interpolated-qap",
            Path.GetFileNameWithoutExtension(file),
            TspFile: (string?)null,
            QapFile: (string?)file)));

        problemSpecs.AddRange(tspFiles.Select(file => (
            "activated-tsp",
            Path.GetFileNameWithoutExtension(file),
            TspFile: (string?)file,
            QapFile: (string?)null)));

        var workItems = (
            from problem in problemSpecs.Select((spec, index) => new
            {
                spec.ProblemKey,
                spec.InstanceKey,
                spec.TspFile,
                spec.QapFile,
                Index = index
            })
            from algorithmKind in PrototypeAlgorithmVariants
            from repetition in Enumerable.Range(0, MatrixScaffoldRepetitions)
            select new MatrixScaffoldRunSpec(problem.ProblemKey, problem.Index, problem.InstanceKey, repetition,
                algorithmKind, problem.TspFile, problem.QapFile)
        ).ToArray();

        await Parallel.ForEachAsync(
            workItems,
            TestContext.Current.CancellationToken,
            async (workItem, cancellationToken) =>
            {
                var problemSeed = 2024 + workItem.ProblemIndex * 100 + workItem.Repetition;
                var runSeed = 123 + workItem.ProblemIndex * 100 + workItem.Repetition;
                await RunAndWriteScenario(workItem, problemSeed, runSeed, context, cancellationToken);
            });

        await WriteManifest(workItems, context, TestContext.Current.CancellationToken);
    }

    private static async Task WriteManifest(
        MatrixScaffoldRunSpec[] workItems,
        ExperimentContext context,
        CancellationToken cancellationToken)
    {
        var orderedManifestRuns = context.ManifestRuns
                                         .OrderBy(run => run.ProblemInstance)
                                         .ThenBy(run => run.Repetition)
                                         .ThenBy(run => run.ScenarioName)
                                         .ToArray();
        var manifest = new AutoEcScenarioBatchManifest(
            "auto-ec-scenario-batch/v1",
            context.BatchName,
            DateTimeOffset.UtcNow,
            orderedManifestRuns.Length,
            orderedManifestRuns);
        await File.WriteAllTextAsync(
            Path.Combine(context.OutputDirectory, "manifest.json"),
            JsonSerializer.Serialize(manifest, JsonOptions),
            cancellationToken);

        orderedManifestRuns.Length.ShouldBe(workItems.Length);
    }

    private static async Task RunScenarioAndWriteResult<TCandidate, TSearchSpace, TProblem>(
        AutoEcPaperScenario<TCandidate, TSearchSpace, TProblem> scenario,
        MatrixScaffoldRunSpec workItem,
        ExperimentContext context,
        CancellationToken cancellationToken)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : DynamicProblem<TProblem, TCandidate, TSearchSpace>
    {
        var result = await RunScenario(scenario);
        AssertScenarioProducesExperimentSignals(result, minQualityEpochs: 1, minBbcpChanges: 1);

        var fileName =
            $"{workItem.ProblemKey}_{SanitizeFileName(workItem.InstanceKey)}_{FormatAlgorithmKind(workItem.AlgorithmKind)}_rep-{workItem.Repetition:00}.json";
        var relativeFile = Path.Combine("runs", fileName);
        await File.WriteAllTextAsync(
            Path.Combine(context.RunsDirectory, fileName),
            JsonSerializer.Serialize(result, JsonOptions),
            cancellationToken);
        context.ManifestRuns.Add(new AutoEcScenarioBatchRun(
            result.Input.ScenarioName,
            result.Input.Problem.Instance ?? workItem.ProblemKey,
            workItem.Repetition,
            result.Input.Seeds.ProblemSeed,
            result.Input.Seeds.RunSeed,
            relativeFile.Replace(Path.DirectorySeparatorChar, '/')));
    }

    private static async Task RunAndWriteScenario(MatrixScaffoldRunSpec workItem,
                                                  int problemSeed,
                                                  int runSeed,
                                                  ExperimentContext context,
                                                  CancellationToken cancellationToken)
    {
        switch (workItem.ProblemKey)
        {
            case "moving-peaks":
                await RunScenarioAndWriteResult(
                    new MovingPeaksAutoEcScenario(
                        workItem.AlgorithmKind,
                        problemSeed,
                        runSeed),
                    workItem, context, cancellationToken);
                break;
            case "interpolated-qap":
                await RunScenarioAndWriteResult(
                    new InterpolatedQapAutoEcScenario(
                        workItem.AlgorithmKind,
                        problemSeed,
                        runSeed,
                        instanceKey: workItem.InstanceKey,
                        qapFile: workItem.QapFile),
                    workItem, context, cancellationToken);
                break;
            case "sliding-window-symreg":
                await RunScenarioAndWriteResult(
                    new SlidingWindowSymRegAutoEcScenario(
                        workItem.AlgorithmKind,
                        problemSeed,
                        runSeed),
                    workItem, context, cancellationToken);
                break;
            case "activated-tsp":
                if (workItem.TspFile is null)
                    throw new InvalidOperationException("Activated TSP matrix run requires TSP data.");

                await RunScenarioAndWriteResult(
                    new ActivatedTspAutoEcScenario(
                        workItem.TspFile,
                        context.ConcordePath,
                        problemSeed,
                        runSeed,
                        targetEpochChanges: MatrixScaffoldTargetEpochChanges,
                        noRacers: 2,
                        epochLength: MatrixScaffoldEpochLength,
                        childAlgorithmKind: workItem.AlgorithmKind),
                    workItem, context, cancellationToken);
                break;
            default:
                throw new InvalidOperationException($"Unknown matrix scaffold problem key '{workItem.ProblemKey}'.");
        }
    }

    [Fact]
    public async Task DynamicRacingGa_OnMovingPeaks_ProducesPaperExperimentSignals()
    {
        var scenario = new MovingPeaksAutoEcScenario();
        var result = await RunScenario(scenario);

        AssertScenarioProducesExperimentSignals(result, minQualityEpochs: 3, minBbcpChanges: 2);
    }

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

    private abstract class AutoEcPaperScenario<TCandidate, TSearchSpace, TProblem>(
        AutoEcChildAlgorithmKind childAlgorithmKind = AutoEcChildAlgorithmKind.GeneticAlgorithm)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : DynamicProblem<TProblem, TCandidate, TSearchSpace>
    {
        protected const double HypermutationRate = 1.0;
        protected const int HypermutationDuration = 2;
        protected const double ChangeTriggeredImmigrantRate = 0.2;

        public virtual int TargetEpochChanges => MatrixScaffoldTargetEpochChanges;
        protected virtual int EpochLength => MatrixScaffoldEpochLength;
        public abstract string ScenarioName { get; }
        public AutoEcChildAlgorithmKind ChildAlgorithmKind { get; } = childAlgorithmKind;
        public virtual int ProblemSeed => 321;
        public virtual int RunSeed => 123;
        public virtual int NoRacers => 2;
        public virtual int BurnInEpochs => 1;
        public virtual double EarlyTerminationStrength => 0.0;
        public virtual double HallOfFameStrength => 0.25;
        public virtual int MinimumModelObservationCount => 10;
        public virtual int ModelObservationInterval => 10;
        public virtual double MetaMutationStrength => 0.15;
        public abstract IReadOnlyList<AutoEcParameterRange> HyperParameterRanges { get; }
        public DynamicRacingQualityTrace<TCandidate> QualityTrace { get; private set; } = new();

        public abstract TProblem CreateProblem();

        public abstract AutoEcProblemDescription CreateProblemDescription();

        public abstract AutoEcBaseAlgorithmDescription CreateBaseAlgorithmDescription();

        protected abstract MetaOptimizationSearchSpace CreateHyperParameterSearchSpace();

        public virtual IEvaluator<TCandidate> CreateEvaluator(TProblem problem) =>
            ProblemEvaluator.For(problem);

        public DynamicRacingAlgorithm<TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>,
            IAlgorithm<TCandidate, PopulationState<TCandidate>>> CreateRacingAlgorithm(
            TProblem problem,
            IEvaluator<TCandidate> evaluator)
        {
            var metaSpace = CreateHyperParameterSearchSpace();
            var metaCreator = metaSpace.CombineCreators(
                new RealVectorUniformDistributedCreator(),
                new HEAL.HeuristicLib.Encodings.IntegerVectors.UniformDistributedCreator());
            var metaMutator = metaSpace.CombineMutator(
                new GaussianMutator(mutationRate: 1.0, mutationStrength: MetaMutationStrength),
                new UniformOnePositionMutator());

            return new DynamicRacingAlgorithm<TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>,
                IAlgorithm<TCandidate, PopulationState<TCandidate>>>(
                metaSpace,
                metaCreator,
                metaMutator,
                new BestPopulationStateMerger<TCandidate>(),
                candidate => CreateAlgorithm(problem, evaluator, candidate),
                _ => evaluator)
            {
                NoRacers = NoRacers,
                BurnInEpochs = BurnInEpochs,
                EarlyTerminationStrength = EarlyTerminationStrength,
                HallOfFameStrength = HallOfFameStrength,
                MinimumModelObservationCount = MinimumModelObservationCount,
                ModelObservationInterval = ModelObservationInterval,
                QualityTraceSinkFactory = CreateQualityTraceSink
            };
        }

        public BestBeforeChangePerformanceAnalyzer<TCandidate, TSearchSpace, TProblem>
            CreateBestBeforeChangePerformanceAnalysis(
                TProblem problem,
                IEvaluator<TCandidate> evaluator) =>
            new(problem, [evaluator]);

        protected abstract IAlgorithm<TCandidate, PopulationState<TCandidate>> CreateAlgorithm(
            TProblem problem,
            IEvaluator<TCandidate> evaluator,
            MetaOptimizationGenotype hyperParameters);

        private DynamicRacingQualityTrace<TCandidate> CreateQualityTraceSink()
        {
            QualityTrace = new DynamicRacingQualityTrace<TCandidate>();
            return QualityTrace;
        }
    }

    private sealed class MovingPeaksAutoEcScenario(
        AutoEcChildAlgorithmKind childAlgorithmKind = AutoEcChildAlgorithmKind.GeneticAlgorithm,
        int problemSeed = 321,
        int runSeed = 123,
        int targetEpochChanges = MatrixScaffoldTargetEpochChanges,
        int noRacers = 2,
        int epochLength = MatrixScaffoldEpochLength)
        : AutoEcPaperScenario<RealVector, BoundedRealVectorSearchSpace, MovingPeaksProblem>(childAlgorithmKind)
    {
        private readonly MovingPeaksParameters parameters = new(
            Dimension: 2,
            NumberOfPeaks: 3,
            LowerBound: -5.0,
            UpperBound: 5.0,
            MinHeight: 20.0,
            MaxHeight: 80.0,
            MinWidth: 0.5,
            MaxWidth: 2.0,
            ShiftSeverity: 0.5,
            HeightSeverity: 2.0,
            WidthSeverity: 0.1);

        public override string ScenarioName => $"moving-peaks-{FormatAlgorithmKind(ChildAlgorithmKind)}-auto-ec-smoke";
        public override int ProblemSeed => problemSeed;
        public override int RunSeed => runSeed;
        public override int TargetEpochChanges => targetEpochChanges;
        public override int NoRacers => noRacers;
        protected override int EpochLength => epochLength;
        public override IReadOnlyList<AutoEcParameterRange> HyperParameterRanges =>
        [
            new("mutationRate", "real", 0.05, 0.4),
            new("populationSize", "integer", 8, 16)
        ];

        public override MovingPeaksProblem CreateProblem() =>
            new(parameters,
                RandomNumberGenerator.Create(ProblemSeed),
                epochSchedule: new EvaluationCountSchedule(EpochLength), updatePolicy: UpdatePolicy.AfterEachBatchEvaluation);

        public override AutoEcProblemDescription CreateProblemDescription() =>
            new("MovingPeaks", null,
                new Dictionary<string, string>
                {
                    [nameof(MovingPeaksParameters.Dimension)] = parameters.Dimension.ToString(),
                    [nameof(MovingPeaksParameters.NumberOfPeaks)] = parameters.NumberOfPeaks.ToString(),
                    [nameof(MovingPeaksParameters.LowerBound)] = parameters.LowerBound.ToString("R"),
                    [nameof(MovingPeaksParameters.UpperBound)] = parameters.UpperBound.ToString("R"),
                    [nameof(MovingPeaksParameters.MinHeight)] = parameters.MinHeight.ToString("R"),
                    [nameof(MovingPeaksParameters.MaxHeight)] = parameters.MaxHeight.ToString("R"),
                    [nameof(MovingPeaksParameters.MinWidth)] = parameters.MinWidth.ToString("R"),
                    [nameof(MovingPeaksParameters.MaxWidth)] = parameters.MaxWidth.ToString("R"),
                    [nameof(MovingPeaksParameters.ShiftSeverity)] = parameters.ShiftSeverity.ToString("R"),
                    [nameof(MovingPeaksParameters.HeightSeverity)] = parameters.HeightSeverity.ToString("R"),
                    [nameof(MovingPeaksParameters.WidthSeverity)] = parameters.WidthSeverity.ToString("R"),
                    ["UpdatePolicy"] = UpdatePolicy.AfterEachBatchEvaluation.ToString(),
                    ["EpochLength"] = EpochLength.ToString()
                });

        public override AutoEcBaseAlgorithmDescription CreateBaseAlgorithmDescription() =>
            new(FormatAlgorithmKind(ChildAlgorithmKind),
                new Dictionary<string, string>
                {
                    ["Encoding"] = "RealVector",
                    ["Creator"] = nameof(RealVectorUniformDistributedCreator),
                    ["Crossover"] = nameof(SimulatedBinaryCrossover),
                    ["Mutator"] = nameof(GaussianMutator),
                    ["MutationStrength"] = "0.5",
                    ["Selector"] = "TournamentSelector",
                    ["TournamentSize"] = "2",
                    ["Elites"] = "1",
                    ["HypermutationRate"] = HypermutationRate.ToString("R"),
                    ["HypermutationDuration"] = HypermutationDuration.ToString(),
                    ["ChangeTriggeredImmigrantRate"] = ChangeTriggeredImmigrantRate.ToString("R")
                });

        protected override MetaOptimizationSearchSpace CreateHyperParameterSearchSpace() =>
            new BoundedRealVectorSearchSpace(1, new RealVector(0.05), new RealVector(0.4))
                .CombinedWith<RealVector, BoundedRealVectorSearchSpace, IntegerVector, IntegerVectorSearchSpace>(
                    new IntegerVectorSearchSpace(1, new IntegerVector(8), new IntegerVector(16)));

        protected override IAlgorithm<RealVector, PopulationState<RealVector>>
            CreateAlgorithm(
                MovingPeaksProblem problem,
                IEvaluator<RealVector> evaluator,
                MetaOptimizationGenotype hyperParameters)
        {
            var creator = new RealVectorUniformDistributedCreator();
            var crossover = new SimulatedBinaryCrossover();
            var mutator = new GaussianMutator(mutationRate: 1.0, mutationStrength: 0.5);
            var selector = TournamentSelector.For(problem, tournamentSize: 2);
            var populationSize = hyperParameters.Part2[0];
            var mutationRate = hyperParameters.Part1[0];
            IInterceptor<RealVector>? interceptor = null;

            return ChildAlgorithmKind switch
            {
                AutoEcChildAlgorithmKind.HypermutationGeneticAlgorithm => HypermutationGeneticAlgorithm.Create<RealVector, BoundedRealVectorSearchSpace, MovingPeaksProblem>(
                    creator, crossover, mutator, mutationRate, HypermutationRate, HypermutationDuration,
                    selector, populationSize, evaluator, interceptor: interceptor),
                AutoEcChildAlgorithmKind.ChangeTriggeredRandomImmigrantGeneticAlgorithm =>
                    ChangeTriggeredRandomImmigrantGeneticAlgorithm.Create<RealVector, BoundedRealVectorSearchSpace, MovingPeaksProblem>(
                        creator, crossover, mutator, mutationRate, ChangeTriggeredImmigrantRate,
                        selector, populationSize, evaluator, interceptor: interceptor),
                _ => GeneticAlgorithm.Create(creator, crossover, mutator, selector, evaluator,
                    populationSize: populationSize, mutationRate: mutationRate, interceptor: interceptor)
            };
        }
    }

    private sealed class ActivatedTspAutoEcScenario(
        string tspFile,
        string? concordePath,
        int problemSeed = 2024,
        int runSeed = 123,
        int targetEpochChanges = 50,
        string? scenarioName = null,
        int noRacers = 2,
        int epochLength = 120,
        AutoEcChildAlgorithmKind childAlgorithmKind = AutoEcChildAlgorithmKind.GeneticAlgorithm)
        : AutoEcPaperScenario<Permutation, PermutationSearchSpace, ActivatedTravelingSalesmanProblem>(
            childAlgorithmKind)
    {
        private const double ActivationProbability = 0.7;
        private const double SwitchProbability = 0.25;

        public override string ScenarioName =>
            scenarioName ?? $"activated-tsp-{FormatAlgorithmKind(ChildAlgorithmKind)}-auto-ec-smoke";
        public override int ProblemSeed => problemSeed;
        public override int RunSeed => runSeed;
        public override int TargetEpochChanges => targetEpochChanges;
        public override int NoRacers => noRacers;
        public override double MetaMutationStrength => 0.1;
        public override IReadOnlyList<AutoEcParameterRange> HyperParameterRanges =>
        [
            new("mutationRate", "real", 0.01, 0.2),
            new("populationSize", "integer", 20, 60)
        ];

        public override ActivatedTravelingSalesmanProblem CreateProblem()
        {
            var tspData = TsplibTspInstanceProvider.LoadData(tspFile).ToDistanceMatrixData();
            return new ActivatedTravelingSalesmanProblem(
                tspData,
                RandomNumberGenerator.Create(ProblemSeed),
                activationProb: ActivationProbability,
                switchProbability: SwitchProbability,
                epochSchedule: new EvaluationCountSchedule(epochLength), updatePolicy: UpdatePolicy.AfterEachBatchEvaluation);
        }

        public override AutoEcProblemDescription CreateProblemDescription() =>
            new("ActivatedTravelingSalesman", Path.GetFileName(tspFile),
                new Dictionary<string, string>
                {
                    ["SourceFile"] = tspFile,
                    ["ActivationProbability"] = ActivationProbability.ToString("R"),
                    ["SwitchProbability"] = SwitchProbability.ToString("R"),
                    ["UpdatePolicy"] = UpdatePolicy.AfterEachBatchEvaluation.ToString(),
                    ["EpochLength"] = epochLength.ToString(),
                    ["QualityScale"] = concordePath is null ? "AbsoluteObjectiveValue" : "RelativeToExactEpochOptimum",
                    ["ExactSolver"] = concordePath is null ? "None" : "Concorde",
                    ["ExactSolverPath"] = concordePath ?? string.Empty,
                    ["EvaluatorCache"] = "DynamicCachingEvaluator",
                    ["EvaluatorCacheKey"] = nameof(PermutationCacheKeySelector),
                    ["EvaluatorCacheGraceCount"] = epochLength.ToString()
                });

        public override AutoEcBaseAlgorithmDescription CreateBaseAlgorithmDescription() =>
            new(FormatAlgorithmKind(ChildAlgorithmKind),
                new Dictionary<string, string>
                {
                    ["Encoding"] = "Permutation",
                    ["Creator"] = nameof(RandomPermutationCreator),
                    ["Crossover"] = nameof(EdgeRecombinationCrossover),
                    ["Mutator"] = nameof(InversionMutator),
                    ["Selector"] = "GeneralizedRankSelector",
                    ["RankPressure"] = "4",
                    ["Elites"] = "1",
                    ["HypermutationRate"] = HypermutationRate.ToString("R"),
                    ["HypermutationDuration"] = HypermutationDuration.ToString(),
                    ["ChangeTriggeredImmigrantRate"] = ChangeTriggeredImmigrantRate.ToString("R")
                });

        public override IEvaluator<Permutation>
            CreateEvaluator(ActivatedTravelingSalesmanProblem problem) =>
            CreateBaseEvaluator(problem).Cached(problem, PermutationCacheKeySelector.Instance) with
            {
                GraceCount = epochLength
            };

        private IEvaluator<Permutation> CreateBaseEvaluator(
            ActivatedTravelingSalesmanProblem problem)
        {
            var evaluator = ProblemEvaluator.For(problem);
            if (concordePath is null)
            {
                return evaluator;
            }

            return evaluator.ScaledToDynamicBestKnown(
                problem,
                new ActivatedTravelingSalesmanExactBestKnownProvider(
                    new ConcordeTravelingSalesmanExactSolver(concordePath)));
        }

        protected override MetaOptimizationSearchSpace CreateHyperParameterSearchSpace() =>
            new BoundedRealVectorSearchSpace(1, new RealVector(0.01), new RealVector(0.2))
                .CombinedWith<RealVector, BoundedRealVectorSearchSpace, IntegerVector, IntegerVectorSearchSpace>(
                    new IntegerVectorSearchSpace(1, new IntegerVector(20), new IntegerVector(60)));

        protected override IAlgorithm<Permutation, PopulationState<Permutation>>
            CreateAlgorithm(
                ActivatedTravelingSalesmanProblem problem,
                IEvaluator<Permutation> evaluator,
                MetaOptimizationGenotype hyperParameters)
        {
            var creator = new RandomPermutationCreator();
            var crossover = new EdgeRecombinationCrossover();
            var mutator = new InversionMutator();
            var selector = GeneralizedRankSelector.For(problem, pressure: 4.0);
            var populationSize = hyperParameters.Part2[0];
            var mutationRate = hyperParameters.Part1[0];
            IInterceptor<Permutation>? interceptor = null;

            return ChildAlgorithmKind switch
            {
                AutoEcChildAlgorithmKind.HypermutationGeneticAlgorithm => HypermutationGeneticAlgorithm.Create<Permutation, PermutationSearchSpace, ActivatedTravelingSalesmanProblem>(
                    creator, crossover, mutator, mutationRate, HypermutationRate, HypermutationDuration,
                    selector, populationSize, evaluator, interceptor: interceptor),
                AutoEcChildAlgorithmKind.ChangeTriggeredRandomImmigrantGeneticAlgorithm =>
                    ChangeTriggeredRandomImmigrantGeneticAlgorithm.Create<Permutation, PermutationSearchSpace, ActivatedTravelingSalesmanProblem>(
                        creator, crossover, mutator, mutationRate, ChangeTriggeredImmigrantRate,
                        selector, populationSize, evaluator, interceptor: interceptor),
                _ => GeneticAlgorithm.Create(creator, crossover, mutator, selector, evaluator,
                    populationSize: populationSize, mutationRate: mutationRate, interceptor: interceptor)
            };
        }
    }

    private sealed class InterpolatedQapAutoEcScenario(
        AutoEcChildAlgorithmKind childAlgorithmKind = AutoEcChildAlgorithmKind.GeneticAlgorithm,
        int problemSeed = 321,
        int runSeed = 123,
        int targetEpochChanges = MatrixScaffoldTargetEpochChanges,
        int noRacers = 2,
        int epochLength = MatrixScaffoldEpochLength,
        string instanceKey = "synthetic-4-a",
        string? qapFile = null)
        : AutoEcPaperScenario<Permutation, PermutationSearchSpace, InterpolatedQuadraticAssignmentProblem>(
            childAlgorithmKind)
    {
        public override string ScenarioName =>
            $"interpolated-qap-{instanceKey}-{FormatAlgorithmKind(ChildAlgorithmKind)}-auto-ec-smoke";
        public override int ProblemSeed => problemSeed;
        public override int RunSeed => runSeed;
        public override int TargetEpochChanges => targetEpochChanges;
        public override int NoRacers => noRacers;
        protected override int EpochLength => epochLength;
        public override IReadOnlyList<AutoEcParameterRange> HyperParameterRanges =>
        [
            new("mutationRate", "real", 0.01, 0.2),
            new("populationSize", "integer", 8, 20)
        ];

        public override InterpolatedQuadraticAssignmentProblem CreateProblem()
        {
            var problemDataA = CreateProblemDataA();
            var problemDataB = CreateProblemDataB(problemDataA);

            return new InterpolatedQuadraticAssignmentProblem(
                problemDataA,
                problemDataB,
                RandomNumberGenerator.Create(ProblemSeed),
                alphaStart: 0.0,
                alphaStep: 0.1,
                interpolateDistances: true,
                pingPong: true,
                epochSchedule: new EvaluationCountSchedule(EpochLength), updatePolicy: UpdatePolicy.AfterEachBatchEvaluation);
        }

        public override AutoEcProblemDescription CreateProblemDescription() =>
            new("InterpolatedQuadraticAssignment", instanceKey,
                new Dictionary<string, string>
                {
                    ["SourceFile"] = qapFile ?? "synthetic",
                    ["Size"] = CreateProblemDataA().Size.ToString(),
                    ["AlphaStart"] = "0",
                    ["AlphaStep"] = "0.1",
                    ["InterpolateDistances"] = "true",
                    ["PingPong"] = "true",
                    ["UpdatePolicy"] = nameof(UpdatePolicy.AfterEachBatchEvaluation),
                    ["EpochLength"] = EpochLength.ToString()
                });

        public override AutoEcBaseAlgorithmDescription CreateBaseAlgorithmDescription() =>
            new(FormatAlgorithmKind(ChildAlgorithmKind),
                new Dictionary<string, string>
                {
                    ["Encoding"] = "Permutation",
                    ["Creator"] = nameof(RandomPermutationCreator),
                    ["Crossover"] = nameof(EdgeRecombinationCrossover),
                    ["Mutator"] = nameof(InversionMutator),
                    ["Selector"] = "GeneralizedRankSelector",
                    ["RankPressure"] = "4",
                    ["Elites"] = "1",
                    ["HypermutationRate"] = HypermutationRate.ToString("R"),
                    ["HypermutationDuration"] = HypermutationDuration.ToString(),
                    ["ChangeTriggeredImmigrantRate"] = ChangeTriggeredImmigrantRate.ToString("R")
                });

        protected override MetaOptimizationSearchSpace CreateHyperParameterSearchSpace() =>
            new BoundedRealVectorSearchSpace(1, new RealVector(0.01), new RealVector(0.2))
                .CombinedWith<RealVector, BoundedRealVectorSearchSpace, IntegerVector, IntegerVectorSearchSpace>(
                    new IntegerVectorSearchSpace(1, new IntegerVector(8), new IntegerVector(20)));

        protected override IAlgorithm<Permutation, PopulationState<Permutation>>
            CreateAlgorithm(
                InterpolatedQuadraticAssignmentProblem problem,
                IEvaluator<Permutation> evaluator,
                MetaOptimizationGenotype hyperParameters)
        {
            var creator = new RandomPermutationCreator();
            var crossover = new EdgeRecombinationCrossover();
            var mutator = new InversionMutator();
            var selector = GeneralizedRankSelector.For(problem, pressure: 4.0);
            var populationSize = hyperParameters.Part2[0];
            var mutationRate = hyperParameters.Part1[0];
            IInterceptor<Permutation>? interceptor = null;

            return ChildAlgorithmKind switch
            {
                AutoEcChildAlgorithmKind.HypermutationGeneticAlgorithm => HypermutationGeneticAlgorithm.Create<Permutation, PermutationSearchSpace, InterpolatedQuadraticAssignmentProblem>(
                    creator, crossover, mutator, mutationRate, HypermutationRate, HypermutationDuration,
                    selector, populationSize, evaluator, interceptor: interceptor),
                AutoEcChildAlgorithmKind.ChangeTriggeredRandomImmigrantGeneticAlgorithm =>
                    ChangeTriggeredRandomImmigrantGeneticAlgorithm.Create<Permutation, PermutationSearchSpace, InterpolatedQuadraticAssignmentProblem>(
                        creator, crossover, mutator, mutationRate, ChangeTriggeredImmigrantRate,
                        selector, populationSize, evaluator, interceptor: interceptor),
                _ => GeneticAlgorithm.Create(creator, crossover, mutator, selector, evaluator,
                    populationSize: populationSize, mutationRate: mutationRate, interceptor: interceptor)
            };
        }

        private QuadraticAssignmentProblemData CreateProblemDataA()
        {
            if (qapFile is not null)
            {
                return QaplibQapInstanceProvider.LoadData(qapFile).ToProblemData();
            }

            return CreateSyntheticProblemData(instanceKey);
        }

        private QuadraticAssignmentProblemData CreateProblemDataB(QuadraticAssignmentProblemData problemDataA)
        {
            if (qapFile is not null)
            {
                return CreatePermutedVariant(problemDataA, StableHash(instanceKey));
            }

            return instanceKey switch
            {
                "synthetic-4-b" => CreateSyntheticProblemData("synthetic-4-a"),
                _ => CreateSyntheticProblemData("synthetic-4-b")
            };
        }

        private static QuadraticAssignmentProblemData CreateSyntheticProblemData(string instance)
        {
            return instance switch
            {
                "synthetic-4-b" => CreateSyntheticProblemDataB(),
                _ => CreateSyntheticProblemDataA()
            };
        }

        private static QuadraticAssignmentProblemData CreateSyntheticProblemDataA()
        {
            var flowMatrix = new double[,] { { 0, 2, 0, 1 }, { 2, 0, 3, 0 }, { 0, 3, 0, 4 }, { 1, 0, 4, 0 } };
            var distanceMatrix = new double[,] { { 0, 1, 2, 3 }, { 1, 0, 1, 2 }, { 2, 1, 0, 1 }, { 3, 2, 1, 0 } };
            return new QuadraticAssignmentProblemData(flowMatrix, distanceMatrix);
        }

        private static QuadraticAssignmentProblemData CreateSyntheticProblemDataB()
        {
            var flowMatrix = new double[,] { { 0, 0, 0, 0 }, { 1, 0, 0, 0 }, { 1, 1, 0, 0 }, { 1, 1, 1, 0 } };
            var distanceMatrix = new double[,] { { 0, 1, 2, 3 }, { 1, 0, 1, 2 }, { 2, 1, 0, 1 }, { 3, 2, 1, 0 } };
            return new QuadraticAssignmentProblemData(flowMatrix, distanceMatrix);
        }

        private static QuadraticAssignmentProblemData CreatePermutedVariant(
            IQuadraticAssignmentProblemData source,
            int offsetSeed)
        {
            var size = source.Size;
            var offset = offsetSeed % Math.Max(1, size - 1) + 1;
            var flows = new double[size, size];
            var distances = new double[size, size];
            for (var i = 0; i < size; i++)
            {
                var pi = (i + offset) % size;
                for (var j = 0; j < size; j++)
                {
                    var pj = (j + offset) % size;
                    flows[i, j] = source.GetFlow(pi, pj);
                    distances[i, j] = source.GetDistance(pi, pj);
                }
            }

            return new QuadraticAssignmentProblemData(flows, distances);
        }

        private static int StableHash(string value)
        {
            var hash = 17;
            foreach (var c in value)
            {
                hash = unchecked(hash * 31 + c);
            }

            return hash & int.MaxValue;
        }
    }

    private sealed class SlidingWindowSymRegAutoEcScenario(
        AutoEcChildAlgorithmKind childAlgorithmKind = AutoEcChildAlgorithmKind.GeneticAlgorithm,
        int problemSeed = 321,
        int runSeed = 123,
        int targetEpochChanges = MatrixScaffoldTargetEpochChanges,
        int noRacers = 2,
        int epochLength = MatrixScaffoldEpochLength)
        : AutoEcPaperScenario<ExpressionTree, ExpressionTreeSearchSpace,
            SlidingWindowSymbolicRegressionProblem>(childAlgorithmKind)
    {
        public override string ScenarioName =>
            $"sliding-window-symreg-{FormatAlgorithmKind(ChildAlgorithmKind)}-auto-ec-smoke";
        public override int ProblemSeed => problemSeed;
        public override int RunSeed => runSeed;
        public override int TargetEpochChanges => targetEpochChanges;
        public override int NoRacers => noRacers;
        protected override int EpochLength => epochLength;
        public override IReadOnlyList<AutoEcParameterRange> HyperParameterRanges =>
        [
            new("mutationRate", "real", 0.01, 0.2),
            new("populationSize", "integer", 8, 20)
        ];

        public override SlidingWindowSymbolicRegressionProblem CreateProblem()
        {
            var data = CreateRegressionProblemData();
            var inner = new SymbolicRegressionProblem(
                data,
                Metrics.RMSE,
                CreateSymbolicRegressionSearchSpace());
            return new SlidingWindowSymbolicRegressionProblem(
                inner,
                windowStart: 0,
                windowLength: 12,
                stepSize: 4,
                epochSchedule: new EvaluationCountSchedule(EpochLength), updatePolicy: UpdatePolicy.AfterEachBatchEvaluation);
        }

        public override AutoEcProblemDescription CreateProblemDescription() =>
            new("SlidingWindowSymbolicRegression", "synthetic-sine-trend",
                new Dictionary<string, string>
                {
                    ["Rows"] = "48",
                    ["WindowStart"] = "0",
                    ["WindowLength"] = "12",
                    ["StepSize"] = "4",
                    ["UpdatePolicy"] = nameof(UpdatePolicy.AfterEachBatchEvaluation),
                    ["EpochLength"] = EpochLength.ToString()
                });

        public override AutoEcBaseAlgorithmDescription CreateBaseAlgorithmDescription() =>
            new(FormatAlgorithmKind(ChildAlgorithmKind),
                new Dictionary<string, string>
                {
                    ["Encoding"] = "SymbolicExpressionTree",
                    ["Creator"] = nameof(ProbabilisticTreeCreator),
                    ["Crossover"] = nameof(SubtreeCrossover),
                    ["Mutator"] = nameof(ChooseOneMutator),
                    ["Selector"] = "TournamentSelector",
                    ["TournamentSize"] = "3",
                    ["Elites"] = "1",
                    ["HypermutationRate"] = HypermutationRate.ToString("R"),
                    ["HypermutationDuration"] = HypermutationDuration.ToString(),
                    ["ChangeTriggeredImmigrantRate"] = ChangeTriggeredImmigrantRate.ToString("R")
                });

        protected override MetaOptimizationSearchSpace CreateHyperParameterSearchSpace() =>
            new BoundedRealVectorSearchSpace(1, new RealVector(0.01), new RealVector(0.2))
                .CombinedWith<RealVector, BoundedRealVectorSearchSpace, IntegerVector, IntegerVectorSearchSpace>(
                    new IntegerVectorSearchSpace(1, new IntegerVector(8), new IntegerVector(20)));

        protected override IAlgorithm<ExpressionTree, PopulationState<ExpressionTree>>
            CreateAlgorithm(
                SlidingWindowSymbolicRegressionProblem problem,
                IEvaluator<ExpressionTree> evaluator,
                MetaOptimizationGenotype hyperParameters)
        {
            var creator = new ProbabilisticTreeCreator();
            var crossover = new SubtreeCrossover();
            var mutator = CreateSymbolicRegressionMutator();
            var selector = TournamentSelector.For(problem, tournamentSize: 3);
            var populationSize = hyperParameters.Part2[0];
            var mutationRate = hyperParameters.Part1[0];
            IInterceptor<ExpressionTree>? interceptor = null;

            return ChildAlgorithmKind switch
            {
                AutoEcChildAlgorithmKind.HypermutationGeneticAlgorithm => HypermutationGeneticAlgorithm.Create<ExpressionTree, ExpressionTreeSearchSpace, SlidingWindowSymbolicRegressionProblem>(
                    creator, crossover, mutator, mutationRate, HypermutationRate, HypermutationDuration,
                    selector, populationSize, evaluator, interceptor: interceptor),
                AutoEcChildAlgorithmKind.ChangeTriggeredRandomImmigrantGeneticAlgorithm =>
                    ChangeTriggeredRandomImmigrantGeneticAlgorithm.Create<ExpressionTree, ExpressionTreeSearchSpace, SlidingWindowSymbolicRegressionProblem>(
                        creator, crossover, mutator, mutationRate, ChangeTriggeredImmigrantRate,
                        selector, populationSize, evaluator, interceptor: interceptor),
                _ => GeneticAlgorithm.Create(creator, crossover, mutator, selector, evaluator,
                    populationSize: populationSize, mutationRate: mutationRate, interceptor: interceptor)
            };
        }

        private static RegressionData CreateRegressionProblemData()
        {
            var rows = Enumerable.Range(0, 48)
                                 .Select(i =>
                                 {
                                     var x = i / 8.0;
                                     return new[] { x, Math.Sin(x) + 0.05 * i };
                                 })
                                 .ToArray();
            var values = new double[rows.Length, 2];
            for (var i = 0; i < rows.Length; i++)
            {
                values[i, 0] = rows[i][0];
                values[i, 1] = rows[i][1];
            }

            var frame = DataFrame.FromMatrix(["x", "y"], values);
            return new RegressionData(
                new DataFrame([frame.Get<double>("x")]),
                frame.Get<double>("y"));
        }

        private static ExpressionTreeSearchSpace CreateSymbolicRegressionSearchSpace() =>
            new(
                maximumLength: 31,
                maximumDepth: 6,
                operations: [Symbols.Addition, Symbols.Subtraction, Symbols.Multiplication, Symbols.Sine],
                variables: ["x"]);

        private static ChooseOneMutator<ExpressionTree> CreateSymbolicRegressionMutator() =>
            ChooseOneMutator.Create<ExpressionTree>(
                new NodeReplacementMutator(),
                new LocalPerturbationMutator(LocalPerturbationTargets.All),
                new LocalPerturbationMutator(),
                new ShrinkSubtreeMutator(),
                new SubtreeMutator());
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

    private static IEnumerable<string> FindLocalFiles(string relativeDirectory, string searchPattern)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, relativeDirectory);
            if (Directory.Exists(candidate))
            {
                return Directory.EnumerateFiles(candidate, searchPattern, SearchOption.AllDirectories);
            }

            directory = directory.Parent;
        }

        return [];
    }

    private static string SanitizeFileName(string value)
    {
        var invalidCharacters = Path.GetInvalidFileNameChars();
        return string.Concat(value.Select(c => invalidCharacters.Contains(c) ? '_' : c));
    }
}
