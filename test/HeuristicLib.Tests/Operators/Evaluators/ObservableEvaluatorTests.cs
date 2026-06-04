using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.Tests.TestSupport.Execution;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Operators.Evaluators;

public class ObservableEvaluatorTests
{
    [Fact]
    public void CountEvaluatorCalls_IncrementsOncePerEvaluateCall()
    {
        var counter = new InvocationCounter();
        var evaluator = CreateEvaluator().CountEvaluatorCalls(counter);
        var instance = evaluator.CreateExecutionInstance(TestRun.Instance);
        var problem = CreateProblem();

        instance.Evaluate([1, 2, 3], RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        instance.Evaluate([4], RandomNumberGenerator.Create(2), problem.SearchSpace, problem);

        counter.CurrentCount.ShouldBe(2);
    }

    [Fact]
    public void CountEvaluatedGenotypes_IncrementsByBatchSize()
    {
        var counter = new InvocationCounter();
        var evaluator = CreateEvaluator().CountEvaluatedGenotypes(counter);
        var instance = evaluator.CreateExecutionInstance(TestRun.Instance);
        var problem = CreateProblem();

        instance.Evaluate([1, 2, 3], RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        instance.Evaluate([4], RandomNumberGenerator.Create(2), problem.SearchSpace, problem);

        counter.CurrentCount.ShouldBe(4);
    }

    private static IEvaluator<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>> CreateEvaluator()
    {
        return new DummyEvaluator<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>();
    }

    private static FuncProblem<int, DummySearchSpace<int>> CreateProblem()
    {
        return FuncProblem.Create<int, DummySearchSpace<int>>(
            evaluateFunc: static genotype => genotype,
            encoding: DummySearchSpace<int>.Instance,
            objective: CreateObjective());
    }

    private static Objective CreateObjective()
    {
        return new Objective(
            [ObjectiveDirection.Minimize],
            Comparer<ObjectiveVector>.Create(static (left, right) => left[0].CompareTo(right[0])));
    }
}
