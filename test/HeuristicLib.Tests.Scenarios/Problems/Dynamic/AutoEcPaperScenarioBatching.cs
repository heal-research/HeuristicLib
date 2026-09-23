using System.Collections.Concurrent;
using System.Text.Json;
using HEAL.HeuristicLib.Problems.Dynamic;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Tests.Scenarios.Problems.Dynamic;

public partial class AutoEcPaperScenarioTests
{
    private record ExperimentContext(
        ConcurrentBag<AutoEcScenarioBatchRun> ManifestRuns,
        string? ConcordePath,
        string RunsDirectory,
        string OutputDirectory,
        string BatchName);

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

        var fileName = BuildRunFileName(
            result.Input.Problem.Kind,
            result.Input.Problem.Instance ?? workItem.InstanceKey,
            result.Input.BaseAlgorithm.Kind,
            workItem.Repetition,
            $"racers-{workItem.NoRacers}");
        var relativeFile = Path.Combine("runs", fileName);
        var outputFile = Path.Combine(context.RunsDirectory, fileName);
        if (File.Exists(outputFile))
        {
            throw new IOException("Refusing to overwrite existing run result file: " + outputFile);
        }

        await File.WriteAllTextAsync(
            outputFile,
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
                        runSeed,
                        noRacers: workItem.NoRacers,
                        instanceKey: workItem.InstanceKey,
                        parameters: workItem.MovingPeaksParameters),
                    workItem, context, cancellationToken);
                break;
            case "interpolated-qap":
                await RunScenarioAndWriteResult(
                    new InterpolatedQapAutoEcScenario(
                        workItem.AlgorithmKind,
                        problemSeed,
                        runSeed,
                        noRacers: workItem.NoRacers,
                        instanceKey: workItem.InstanceKey,
                        qapFile: workItem.QapFile),
                    workItem, context, cancellationToken);
                break;
            case "sliding-window-symreg":
                await RunScenarioAndWriteResult(
                    new SlidingWindowSymRegAutoEcScenario(
                        workItem.AlgorithmKind,
                        problemSeed,
                        runSeed,
                        noRacers: workItem.NoRacers),
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
                        noRacers: workItem.NoRacers,
                        epochLength: MatrixScaffoldEpochLength,
                        childAlgorithmKind: workItem.AlgorithmKind),
                    workItem, context, cancellationToken);
                break;
            default:
                throw new InvalidOperationException($"Unknown matrix scaffold problem key '{workItem.ProblemKey}'.");
        }
    }
}
