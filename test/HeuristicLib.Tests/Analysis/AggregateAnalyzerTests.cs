using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Analysis;

public sealed class AggregateAnalyzerTests
{
    private static readonly IProblem<int, DummySearchSpace<int>> Problem = MetaAlgorithmTestHelpers.CreateIntegerProblem();

    [Fact]
    public void AttachedAndExplicitPlacementProduceEqualIndependentState()
    {
        var algorithm = new AdditiveStepAlgorithm(3);
        var source = algorithm.Evaluator.Invocations;
        var totals = source.Aggregate(() => new Dictionary<int, int>(),
            (state, observation) =>
            {
                foreach (var candidate in observation.Candidates)
                    state[candidate] = state.GetValueOrDefault(candidate) + 1;
            });
        var attached = algorithm.CreateRun(Problem, RandomNumberGenerator.Create(1), [totals]);
        var placed = (algorithm with { Evaluator = algorithm.Evaluator.Observed(totals) })
            .CreateRun(Problem, RandomNumberGenerator.Create(1));

        attached.Result(totals).Value.ShouldBeEmpty();
        attached.Complete(cancellationToken: TestContext.Current.CancellationToken);
        placed.Complete(cancellationToken: TestContext.Current.CancellationToken);

        attached.Result(totals).ShouldNotBeSameAs(placed.Result(totals));
        attached.Result(totals).Value.ShouldNotBeSameAs(placed.Result(totals).Value);
        attached.Result(totals).Value.OrderBy(entry => entry.Key).ShouldBe(placed.Result(totals).Value.OrderBy(entry => entry.Key));
        attached.Result(totals).Value.ShouldNotBeEmpty();
    }

    [Fact]
    public void AnUnplacedAggregateIsRejected()
    {
        var algorithm = new AdditiveStepAlgorithm(1);
        var totals = algorithm.Evaluator.Invocations.Aggregate(() => new List<int>(), (state, observation) => state.Add(observation.Candidates.Count));
        var run = algorithm.CreateRun(Problem, RandomNumberGenerator.Create(1));

        Should.Throw<InvalidOperationException>(() => run.Result(totals)).Message.ShouldContain("does not hold");
    }

    [Fact]
    public void RepeatedSourcesObserveTheBoundaryOnce()
    {
        var algorithm = new AdditiveStepAlgorithm(3);
        var totals = Analyzer.Aggregate([algorithm.Evaluator.Invocations, algorithm.Evaluator.Invocations],
            () => new List<int>(), (state, observation) => state.Add(observation.Candidates.Count));
        var run = algorithm.CreateRun(Problem, RandomNumberGenerator.Create(1), [totals]);

        run.Complete(cancellationToken: TestContext.Current.CancellationToken);

        run.Result(totals).Value.ShouldBe([1]);
    }

    [Fact]
    public void Aggregate_MergesDistinctValueEqualSources()
    {
        var evaluator = new ProblemEvaluator<int>();
        var equalButDistinct = new ProblemEvaluator<int>();
        equalButDistinct.ShouldBe(evaluator);
        equalButDistinct.ShouldNotBeSameAs(evaluator);
        var totals = Analyzer.Aggregate([evaluator.Invocations, evaluator.Invocations, equalButDistinct.Invocations],
            () => new List<int>(), (state, observation) => state.AddRange(observation.Candidates));
        var first = new AdditiveStepAlgorithm(3) { Evaluator = evaluator };
        var second = new AdditiveStepAlgorithm(-1) { Evaluator = equalButDistinct };
        var algorithm = CycleAlgorithm.Create(first, second) with { MaximumCycles = 1 };
        var run = algorithm.CreateRun(Problem, RandomNumberGenerator.Create(1), [totals]);

        run.Complete(cancellationToken: TestContext.Current.CancellationToken);

        run.Result(totals).Value.ShouldBe([3, 2]);
    }

    [Fact]
    public void AnEmptySourceListIsRejected()
    {
        Should.Throw<InvalidOperationException>(() => Analyzer.Aggregate<EvaluationObservation<int>, List<int>>([], () => [], (_, _) => { }));
    }

    [Fact]
    public async Task ConcurrentRecordersSerializeUpdatesToOneResult()
    {
        var algorithm = new AdditiveStepAlgorithm(1);
        var totals = algorithm.Evaluator.Invocations.Aggregate(() => new List<int>(), (state, _) => state.Add(state.Count));
        var results = ResolutionScope.Create().Results;
        var first = totals.CreateRecorder(results);
        var second = totals.CreateRecorder(results);

        await Task.WhenAll(Enumerable.Range(0, 100).Select(i => Task.Run(
            () => (i % 2 == 0 ? first : second)(new EvaluationObservation<int>(algorithm.Evaluator, [], [], Problem.SearchSpace, Problem)), TestContext.Current.CancellationToken)));

        results.Result(totals).Value.ShouldBe(Enumerable.Range(0, 100));
    }
}
