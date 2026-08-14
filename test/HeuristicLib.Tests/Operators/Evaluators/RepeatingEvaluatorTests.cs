using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Operators.Evaluators;

public class RepeatingEvaluatorTests
{
    [Fact]
    public void Aggregator_DefaultsToMean()
    {
        var evaluator = new RepeatingEvaluator<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>(new CandidateEvaluator(), 3);

        evaluator.Aggregator.ShouldBe(ObjectiveVectorAggregation.Mean);
    }

    [Fact]
    public void Evaluate_PerformsConfiguredTotalNumberOfRepetitions()
    {
        var counter = new ObservationCounter();
        var problem = CreateProblem();
        var evaluator = new CandidateEvaluator().CountEvaluatorCalls(counter).AsRepeated(3);

        evaluator.CreateExecutionInstance(new ExecutionInstanceRegistry())
            .Evaluate([1], RandomNumberGenerator.Create(1), problem.SearchSpace, problem);

        counter.CurrentCount.ShouldBe(3);
    }

    [Fact]
    public void Evaluate_AggregatesRepeatedObjectiveVectorsByCandidate()
    {
        var problem = CreateProblem();
        var evaluator = new RandomEvaluator().AsRepeated(5);
        var instance = evaluator.CreateExecutionInstance(new ExecutionInstanceRegistry());

        var actual = instance.Evaluate([1, 2], RandomNumberGenerator.Create(42), problem.SearchSpace, problem);
        var rootRandom = RandomNumberGenerator.Create(42);
        var repeated = Enumerable.Range(0, 5)
            .Select(repetition => Enumerable.Range(0, 2)
                .Select(candidate => new ObjectiveVector(rootRandom.Fork(repetition).Fork(candidate).NextDouble()))
                .ToArray())
            .ToArray();
        var expected = Enumerable.Range(0, 2)
            .Select(candidateIndex => ObjectiveVectorAggregation.Mean.Aggregate(repeated.Select(values => values[candidateIndex]).ToArray(), problem.Objective))
            .ToArray();

        actual.Select(evaluatedCandidate => evaluatedCandidate.ObjectiveVector).ShouldBe(expected);
    }

    [Fact]
    public void Evaluate_IsIndependentOfConfiguredConcurrency()
    {
        var problem = CreateProblem();
        var sequential = new RandomEvaluator().AsRepeated(32);
        var concurrent = sequential with { Concurrency = ExecutionConcurrency.Concurrent(4) };

        var sequentialResult = sequential.CreateExecutionInstance(new ExecutionInstanceRegistry())
            .Evaluate([1, 2], RandomNumberGenerator.Create(42), problem.SearchSpace, problem);
        var concurrentResult = concurrent.CreateExecutionInstance(new ExecutionInstanceRegistry())
            .Evaluate([1, 2], RandomNumberGenerator.Create(42), problem.SearchSpace, problem);

        concurrentResult.ShouldBe(sequentialResult);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ExecutionInstanceCreation_RejectsNonPositiveRepetitions(int repetitions)
    {
        var constructed = new RepeatingEvaluator<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>(new CandidateEvaluator(), repetitions);
        var reconfigured = new RepeatingEvaluator<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>(new CandidateEvaluator(), 1) with
        {
            Repetitions = repetitions
        };

        constructed.Repetitions.ShouldBe(repetitions);
        reconfigured.Repetitions.ShouldBe(repetitions);
        Should.Throw<InvalidOperationException>(() => constructed.CreateExecutionInstance(new ExecutionInstanceRegistry()));
        Should.Throw<InvalidOperationException>(() => reconfigured.CreateExecutionInstance(new ExecutionInstanceRegistry()));
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
