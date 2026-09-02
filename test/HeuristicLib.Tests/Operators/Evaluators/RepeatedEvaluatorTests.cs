using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Operators.Evaluators;

public class RepeatedEvaluatorTests
{
    [Fact]
    public void RepeatedEvaluator_UsesMeanObjectiveAggregation_ByDefault()
    {
        var problem = CreateProblem();
        var evaluator = new StatefulObjectiveEvaluator().AsRepeated(repetitions: 3);
        var instance = new ExecutionInstanceRegistry().Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(evaluator);

        var objectiveVectors = instance.Evaluate([7], RandomNumberGenerator.Create(1), problem.SearchSpace, problem);

        objectiveVectors.Single().ShouldBe(new ObjectiveVector(2.0));
    }

    [Fact]
    public void RepeatedEvaluator_AggregatesEachCandidatePositionally()
    {
        var problem = CreateProblem();
        var evaluator = new ProblemEvaluator<int>()
            .AsRepeated(repetitions: 2);
        var instance = new ExecutionInstanceRegistry().Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(evaluator);

        var objectiveVectors = instance.Evaluate([3, 5, 8], RandomNumberGenerator.Create(1), problem.SearchSpace, problem);

        objectiveVectors.ShouldBe([new ObjectiveVector(3.0), new ObjectiveVector(5.0), new ObjectiveVector(8.0)]);
    }

    private static FuncProblem<int, DummySearchSpace<int>> CreateProblem()
    {
        return FuncProblem.Create<int, DummySearchSpace<int>>(
            evaluateFunc: static genotype => genotype,
            encoding: DummySearchSpace<int>.Instance,
            objective: SingleObjective.Minimize);
    }

    private sealed record StatefulObjectiveEvaluator
        : StatefulEvaluator<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, StatefulObjectiveEvaluator.ExecutionState>
    {
        public sealed class ExecutionState
        {
            public int Calls;
        }

        protected override ExecutionState CreateInitialState() => new();

        protected override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<int> candidates, ExecutionState executionState, IRandomNumberGenerator random, DummySearchSpace<int> searchSpace, FuncProblem<int, DummySearchSpace<int>> problem)
        {
            var calls = Interlocked.Increment(ref executionState.Calls);
            return candidates.Select(_ => problem.Evaluate(calls, random)).ToArray();
        }
    }
}
