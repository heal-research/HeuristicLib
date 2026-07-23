using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.Tests.TestSupport.Execution;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Operators.Evaluators;

public class RepeatedEvaluatorTests
{
    [Fact]
    public void RepeatedEvaluator_UsesMeanObjectiveAggregation_ByDefault()
    {
        var problem = CreateProblem();
        var evaluator = new StatefulObjectiveEvaluator().AsRepeated(repeats: 3);
        var instance = new ExecutionInstanceRegistry().Resolve(evaluator);

        var solutions = instance.Evaluate([7], RandomNumberGenerator.Create(1), problem.SearchSpace, problem);

        solutions.Single().ShouldBe(EvaluatedCandidate.From(7, new ObjectiveVector(2.0)));
    }

    [Fact]
    public void RepeatedEvaluator_Throws_WhenRepeatedEvaluationReturnsDifferentGenotype()
    {
        var problem = CreateProblem();
        var evaluator = new StatefulReplacingEvaluator().AsRepeated(
            repeats: 2,
            aggregator: objectives => objectives[0]);
        var instance = new ExecutionInstanceRegistry().Resolve(evaluator);

        Should.Throw<InvalidOperationException>(() =>
            instance.Evaluate([0], RandomNumberGenerator.Create(1), problem.SearchSpace, problem));
    }

    [Fact]
    public void RepeatedEvaluator_UsesCustomGenotypeComparer()
    {
        var problem = CreateProblem();
        var evaluator = new StatefulReplacingEvaluator().AsRepeated(
            repeats: 2,
            comparer: EqualityComparer<int>.Create(static (_, _) => true));
        var instance = new ExecutionInstanceRegistry().Resolve(evaluator);

        var solutions = instance.Evaluate([0], RandomNumberGenerator.Create(1), problem.SearchSpace, problem);

        solutions.Single().ShouldBe(EvaluatedCandidate.From(1, new ObjectiveVector(1.5)));
    }

    [Fact]
    public void IteratedEvaluator_FeedsReturnedGenotypesIntoNextIteration()
    {
        var problem = CreateProblem();
        var evaluator = new IncrementingEvaluator().AsIterated(iterations: 3);
        var instance = new ExecutionInstanceRegistry().Resolve(evaluator);

        var solutions = instance.Evaluate([0], RandomNumberGenerator.Create(1), problem.SearchSpace, problem);

        solutions.Single().ShouldBe(EvaluatedCandidate.From(3, new ObjectiveVector(3.0)));
    }

    private static FuncProblem<int, DummySearchSpace<int>> CreateProblem()
    {
        return FuncProblem.Create<int, DummySearchSpace<int>>(
            evaluateFunc: static genotype => genotype,
            encoding: DummySearchSpace<int>.Instance,
            objective: SingleObjective.Minimize);
    }

    private sealed record IncrementingEvaluator
        : StatelessEvaluator<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>
    {
        public override IReadOnlyList<EvaluatedCandidate<int>> Evaluate(
            IReadOnlyList<int> candidates,
            IRandomNumberGenerator random,
            DummySearchSpace<int> searchSpace,
            FuncProblem<int, DummySearchSpace<int>> problem)
            => candidates.Select(candidate =>
            {
                var next = candidate + 1;
                return EvaluatedCandidate.From(next, problem.Evaluate(next, random));
            }).ToArray();
    }

    private sealed record StatefulReplacingEvaluator
        : StatefulEvaluator<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, StatefulReplacingEvaluator.ExecutionState>
    {
        public sealed class ExecutionState
        {
            public int Calls;
        }

        protected override ExecutionState CreateInitialState() => new();

        protected override IReadOnlyList<EvaluatedCandidate<int>> Evaluate(
            IReadOnlyList<int> candidates,
            ExecutionState executionState,
            IRandomNumberGenerator random,
            DummySearchSpace<int> searchSpace,
            FuncProblem<int, DummySearchSpace<int>> problem)
        {
            var calls = Interlocked.Increment(ref executionState.Calls);
            return candidates.Select(_ => EvaluatedCandidate.From(calls, problem.Evaluate(calls, random))).ToArray();
        }
    }

    private sealed record StatefulObjectiveEvaluator
        : StatefulEvaluator<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, StatefulObjectiveEvaluator.ExecutionState>
    {
        public sealed class ExecutionState
        {
            public int Calls;
        }

        protected override ExecutionState CreateInitialState() => new();

        protected override IReadOnlyList<EvaluatedCandidate<int>> Evaluate(
            IReadOnlyList<int> candidates,
            ExecutionState executionState,
            IRandomNumberGenerator random,
            DummySearchSpace<int> searchSpace,
            FuncProblem<int, DummySearchSpace<int>> problem)
        {
            var calls = Interlocked.Increment(ref executionState.Calls);
            return candidates.Select(candidate => EvaluatedCandidate.From(candidate, problem.Evaluate(calls, random))).ToArray();
        }
    }
}
