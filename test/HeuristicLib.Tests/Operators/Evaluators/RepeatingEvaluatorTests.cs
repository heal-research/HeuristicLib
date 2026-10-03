using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Operators.Evaluators;

public class RepeatingEvaluatorTests
{
    [Fact]
    public void Aggregator_DefaultsToMean()
    {
        var evaluator = new RepeatingEvaluator<int>(new CandidateEvaluator(), 3);

        evaluator.Aggregator.ShouldBe(ObjectiveVectorAggregation.Mean);
    }

    [Fact]
    public void Evaluate_PerformsConfiguredTotalNumberOfRepetitions()
    {
        var counter = new CountAccumulator();
        var problem = CreateProblem();
        var evaluator = new CandidateEvaluator().CountCalls(counter).AsRepeated(3);

        ResolutionScope.Create().Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(evaluator)
            .Evaluate([1], RandomNumberGenerator.Create(1), problem.SearchSpace, problem);

        counter.CurrentCount.ShouldBe(3);
    }

    [Fact]
    public void Evaluate_AggregatesRepeatedObjectiveVectorsByCandidate()
    {
        var problem = CreateProblem();
        var evaluator = new RandomEvaluator().AsRepeated(5);
        var execution = ResolutionScope.Create().Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(evaluator);

        var actual = execution.Evaluate([1, 2], RandomNumberGenerator.Create(42), problem.SearchSpace, problem);
        var rootRandom = RandomNumberGenerator.Create(42);
        var repeated = Enumerable.Range(0, 5)
            .Select(repetition => Enumerable.Range(0, 2)
                .Select(candidate => new ObjectiveVector(rootRandom.Fork(repetition).Fork(candidate).NextDouble()))
                .ToArray())
            .ToArray();
        var expected = Enumerable.Range(0, 2)
            .Select(candidateIndex => ObjectiveVectorAggregation.Mean.Aggregate(repeated.Select(values => values[candidateIndex]).ToArray(), problem.Objective))
            .ToArray();

        actual.ShouldBe(expected);
    }

    [Fact]
    public void Evaluate_IsIndependentOfConfiguredConcurrency()
    {
        var problem = CreateProblem();
        var sequential = new RandomEvaluator().AsRepeated(32);
        var concurrent = sequential with { Concurrency = ExecutionConcurrency.Concurrent(4) };

        var sequentialResult = ResolutionScope.Create().Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(sequential)
            .Evaluate([1, 2], RandomNumberGenerator.Create(42), problem.SearchSpace, problem);
        var concurrentResult = ResolutionScope.Create().Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(concurrent)
            .Evaluate([1, 2], RandomNumberGenerator.Create(42), problem.SearchSpace, problem);

        concurrentResult.ShouldBe(sequentialResult);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Preparation_RejectsNonPositiveRepetitions(int repetitions)
    {
        var constructed = new RepeatingEvaluator<int>(new CandidateEvaluator(), repetitions);
        var reconfigured = new RepeatingEvaluator<int>(new CandidateEvaluator(), 1) with
        {
            Repetitions = repetitions
        };

        constructed.Repetitions.ShouldBe(repetitions);
        reconfigured.Repetitions.ShouldBe(repetitions);
        Should.Throw<InvalidOperationException>(() => constructed.CreateExecutionFactory<DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>());
        Should.Throw<InvalidOperationException>(() => reconfigured.CreateExecutionFactory<DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>());
    }

    private static FuncProblem<int, DummySearchSpace<int>> CreateProblem() =>
        FuncProblem.Create(static (int candidate) => candidate, DummySearchSpace<int>.Instance, SingleObjective.Minimize);

    private sealed record CandidateEvaluator : SingleCandidateEvaluator<int, DummySearchSpace<int>>
    {
        public override ObjectiveVector EvaluateCandidate(int candidate, IRandomNumberGenerator random, DummySearchSpace<int> searchSpace) =>
            new(candidate);
    }

    private sealed record RandomEvaluator : SingleCandidateEvaluator<int, DummySearchSpace<int>>
    {
        public override ObjectiveVector EvaluateCandidate(int candidate, IRandomNumberGenerator random, DummySearchSpace<int> searchSpace) =>
            new(random.NextDouble());
    }
}
