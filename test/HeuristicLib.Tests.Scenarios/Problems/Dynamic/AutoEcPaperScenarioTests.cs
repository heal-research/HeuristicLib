using System.Collections.Concurrent;
using System.Text.Json;
using HEAL.HeuristicLib.Problems.Dynamic;

namespace HEAL.HeuristicLib.Tests.Scenarios.Problems.Dynamic;

public partial class AutoEcPaperScenarioTests
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
    public async Task ActivatedTspAnalysis_UsesReferenceFromEvaluatedEpoch()
    {
        var tspFile = FindLocalFile("tsp", "burma14.tsp");
        var concordePath = FindLocalFile("for_Agent", "Concorde", "executablescygwin", "concorde.exe");
        if (tspFile is null || concordePath is null)
            return;

        var scenario = new ActivatedTspAutoEcScenario(
            tspFile,
            concordePath,
            problemSeed: 31740,
            runSeed: 1623,
            targetEpochChanges: 3,
            noRacers: 1,
            epochLength: 50);
        var result = await RunScenario(scenario);

        AssertScenarioProducesExperimentSignals(result, minQualityEpochs: 2, minBbcpChanges: 1);
        result.Output.QualityCurve
              .Where(point => point.Timing.Valid)
              .ShouldAllBe(point => point.ObjectiveVector[0] >= -1e-12);
        var bbcpFromQualityCurve = result.Output.QualityCurve
                                         .Where(point => point.Timing.Valid && point.Timing.Epoch < result.Output.ObservedEpoch)
                                         .GroupBy(point => point.Timing.Epoch)
                                         .Select(epoch => epoch.Last().ObjectiveVector[0])
                                         .Average();
        result.Output.BestBeforeChangePerformance.ShouldBe(bbcpFromQualityCurve, tolerance: 1e-12);
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

                var fileName = BuildRunFileName(
                    result.Input.Problem.Kind,
                    result.Input.Problem.Instance ?? Path.GetFileNameWithoutExtension(workItem.TspFile),
                    result.Input.BaseAlgorithm.Kind,
                    workItem.Repetition,
                    $"racers-{workItem.NoRacers}");
                var relativeFile = Path.Combine("runs", fileName);
                var outputFile = Path.Combine(runsDirectory, fileName);
                if (File.Exists(outputFile))
                {
                    throw new IOException("Refusing to overwrite existing run result file: " + outputFile);
                }

                await File.WriteAllTextAsync(
                    outputFile,
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

    [Fact]
    public async Task DynamicRacingMatrix_WritesPaperScaffoldResults()
    {
        var concordePath = FindLocalFile("for_Agent", "Concorde", "executablescygwin", "concorde.exe");
        var tspFiles = MatrixScaffoldTspFiles.Select(name => FindLocalFile("tsp", name))
                                             .Where(file => file is not null)
                                             .Cast<string>()
                                             .ToArray();
        var qapFiles = MatrixScaffoldQapFiles.Select(FindLocalQapFile)
                                             .Where(file => file is not null)
                                             .Cast<string>()
                                             .ToArray();
        var batchName = $"auto-ec-matrix-scaffold-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}";
        var outputDirectory = Path.Combine(FindRepositoryRoot(), "artifacts", "autoec-paper-results", batchName);
        var runsDirectory = Path.Combine(outputDirectory, "runs");
        Directory.CreateDirectory(runsDirectory);
        var manifestRuns = new ConcurrentBag<AutoEcScenarioBatchRun>();
        var context = new ExperimentContext(manifestRuns, concordePath, runsDirectory, outputDirectory, batchName);

        var problemSpecs =
            new List<(string ProblemKey, string InstanceKey, string? TspFile, string? QapFile,
                MovingPeaksParameters? MovingPeaksParameters)>
            {
                ("sliding-window-symreg", "synthetic-sine-trend", null, null, null)
            };

        problemSpecs.AddRange(MatrixScaffoldMovingPeaksConfigs.Select(config => (
            "moving-peaks",
            config.Key,
            TspFile: (string?)null,
            QapFile: (string?)null,
            MovingPeaksParameters: (MovingPeaksParameters?)config.Parameters)));

        problemSpecs.AddRange(qapFiles.Select(file => (
            "interpolated-qap",
            Path.GetFileNameWithoutExtension(file),
            TspFile: (string?)null,
            QapFile: (string?)file,
            MovingPeaksParameters: (MovingPeaksParameters?)null)));

        problemSpecs.AddRange(tspFiles.Select(file => (
            "activated-tsp",
            Path.GetFileNameWithoutExtension(file),
            TspFile: (string?)file,
            QapFile: (string?)null,
            MovingPeaksParameters: (MovingPeaksParameters?)null)));

        var workItems = (
            from problem in problemSpecs.Select((spec, index) => new
            {
                spec.ProblemKey,
                spec.InstanceKey,
                spec.TspFile,
                spec.QapFile,
                spec.MovingPeaksParameters,
                Index = index
            })
            from algorithmKind in MatrixScaffoldAlgorithmVariants
            from noRacers in MatrixScaffoldRacerVariants
            from repetition in Enumerable.Range(0, MatrixScaffoldRepetitions)
            select new MatrixScaffoldRunSpec(problem.ProblemKey, problem.Index, problem.InstanceKey, repetition,
                noRacers, algorithmKind, problem.TspFile, problem.QapFile, problem.MovingPeaksParameters)
        ).ToArray();

        await Parallel.ForEachAsync(
            workItems,
            TestContext.Current.CancellationToken,
            async (workItem, cancellationToken) =>
            {
                var problemSeed = 30240 + workItem.ProblemIndex * 100 + workItem.Repetition;
                var runSeed = 123 + workItem.ProblemIndex * 100 + workItem.Repetition;
                await RunAndWriteScenario(workItem, problemSeed, runSeed, context, cancellationToken);
            });

        await WriteManifest(workItems, context, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task DynamicRacingGa_OnMovingPeaks_ProducesPaperExperimentSignals()
    {
        var scenario = new MovingPeaksAutoEcScenario();
        var result = await RunScenario(scenario);

        AssertScenarioProducesExperimentSignals(result, minQualityEpochs: 3, minBbcpChanges: 2);
    }

    [Fact]
    public async Task DynamicRacingHypermutation_PreservesResponseAcrossEpochs()
    {
        var ga = new MovingPeaksAutoEcScenario(
            AutoEcChildAlgorithmKind.GeneticAlgorithm,
            problemSeed: 321,
            runSeed: 123,
            targetEpochChanges: 4,
            noRacers: 1,
            epochLength: 100);
        var hypermutation = new MovingPeaksAutoEcScenario(
            AutoEcChildAlgorithmKind.HypermutationGeneticAlgorithm,
            problemSeed: 321,
            runSeed: 123,
            targetEpochChanges: 4,
            noRacers: 1,
            epochLength: 100);

        var gaResult = await RunScenario(ga);
        var hypermutationResult = await RunScenario(hypermutation);

        var gaCurve = gaResult.Output.QualityCurve.SelectMany(point => point.ObjectiveVector).ToArray();
        var hypermutationCurve = hypermutationResult.Output.QualityCurve
                                                     .SelectMany(point => point.ObjectiveVector)
                                                     .ToArray();
        gaCurve.SequenceEqual(hypermutationCurve).ShouldBeFalse();
    }

    [Fact]
    public async Task DynamicRacingGa_OnInterpolatedQap_ProducesPaperExperimentSignals()
    {
        var scenario = new InterpolatedQapAutoEcScenario();
        var result = await RunScenario(scenario);

        AssertScenarioProducesExperimentSignals(result, minQualityEpochs: 3, minBbcpChanges: 2);
    }

    [Fact]
    public async Task DynamicRacingGa_OnSlidingWindowSymReg_ProducesPaperExperimentSignals()
    {
        var scenario = new SlidingWindowSymRegAutoEcScenario();
        var result = await RunScenario(scenario);

        AssertScenarioProducesExperimentSignals(result, minQualityEpochs: 3, minBbcpChanges: 2);
    }

    [Fact]
    public async Task DynamicRacingHypermutation_OnSlidingWindowSymReg_RebuildsChooseOneMutator()
    {
        var scenario = new SlidingWindowSymRegAutoEcScenario(
            AutoEcChildAlgorithmKind.HypermutationGeneticAlgorithm,
            targetEpochChanges: 4,
            noRacers: 2,
            epochLength: 100);

        var result = await RunScenario(scenario);

        AssertScenarioProducesExperimentSignals(result, minQualityEpochs: 3, minBbcpChanges: 2);
    }
}
