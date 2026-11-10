using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Experiments;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Experiments;

public class ExperimentAnalysisTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    public async Task RepeatedTrialsKeepIndependentResultsOfOneDefinition(int concurrency)
    {
        var algorithm = new CountingExecutionAlgorithm(1, new CountingResolutionEvaluator());
        var calls = algorithm.Evaluator.Invocations.Count();
        var run = algorithm.AnalyzedBy(calls).Repeat(3).CreateRun(
            MetaAlgorithmTestHelpers.CreateIntegerProblem(), RandomNumberGenerator.Create(42));

        await run.CompleteAsync(ExecutionConcurrency.Concurrent(concurrency), cancellationToken: TestContext.Current.CancellationToken);

        run.Trials.Select(trial => trial.Key).ShouldBe([0, 1, 2]);
        run.Trials.ShouldAllBe(trial => trial.Run.Result(calls).CurrentCount == 1);
        run.Trials.Select(trial => trial.Run.Result(calls)).Distinct(ReferenceEqualityComparer.Instance).Count().ShouldBe(3);
    }

    [Fact]
    public async Task ATrialResultIsReadableIndependentlyAsThatTrialFinishes()
    {
        var algorithm = new CountingExecutionAlgorithm(1, new CountingResolutionEvaluator());
        var calls = algorithm.Evaluator.Invocations.Count();
        var run = algorithm.AnalyzedBy(calls).Repeat(2).CreateRun(
            MetaAlgorithmTestHelpers.CreateIntegerProblem(), RandomNumberGenerator.Create(42));

        await run.Trials[0].Run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);
        run.Trials[0].Run.Result(calls).CurrentCount.ShouldBe(1);
        run.Trials[1].Run.Result(calls).CurrentCount.ShouldBe(0);

        await run.Trials[1].Run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);
        run.Trials[1].Run.Result(calls).CurrentCount.ShouldBe(1);
    }
}
