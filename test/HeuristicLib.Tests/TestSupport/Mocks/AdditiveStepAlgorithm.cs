using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Tests.TestSupport.Mocks;

public sealed record AdditiveStepAlgorithm(int Increment)
  : Algorithm<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>, AdditiveStepAlgorithm.ExecutionState>
{
    public new sealed class ExecutionState
      : Algorithm<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>, ExecutionState>.ExecutionState;

    protected override ExecutionState CreateInitialExecutionState(IExecutionInstanceResolver resolver)
    {
        return new ExecutionState
        {
            Evaluator = resolver.Resolve(Evaluator)
        };
    }

    protected override IAlgorithmInstance<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>> CreateAlgorithmInstance(
      Run run,
      ExecutionState executionState)
    {
        return new Instance(run, executionState.Evaluator, Increment);
    }

    private sealed class Instance(
      Run run,
      IEvaluatorInstance<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>> evaluator,
      int increment)
      : AlgorithmInstance<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>>(run, evaluator)
    {
        public override async IAsyncEnumerable<PopulationState<int>> RunStreamingAsync(
          IProblem<int, DummySearchSpace<int>> problem,
          IRandomNumberGenerator random,
          PopulationState<int>? initialState = null,
          [EnumeratorCancellation]
      CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            var current = initialState?.Population.Solutions.Single().Genotype ?? 0;
            var next = current + increment;
            var solution = Evaluator.Evaluate([next], random, problem.SearchSpace, problem).Single();

            yield return new PopulationState<int>
            {
                Population = Population.From([solution])
            };

            await Task.CompletedTask;
        }
    }
}
