using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Instrumentation;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Evaluators;

public sealed record CountingEvaluator<TCandidate>
    : WrappingEvaluator<TCandidate>
{
    public CountAccumulator Counter { get; init; }
    public OperatorCountMetric Metric { get; init; }

    public CountingEvaluator(IEvaluator<TCandidate> childEvaluator, CountAccumulator counter, OperatorCountMetric metric)
        : base(childEvaluator)
    {
        Counter = counter;
        Metric = metric;
    }

    protected override WrapperExecutionFactory<IEvaluatorExecution<TCandidate, TRunSearchSpace, TRunProblem>> CreateWrapperFactory<TRunSearchSpace, TRunProblem>() =>
        childEvaluator => new Execution<TRunSearchSpace, TRunProblem>(childEvaluator, Counter, Metric);

    private sealed class Execution<TSearchSpace, TProblem>(IEvaluatorExecution<TCandidate, TSearchSpace, TProblem> childEvaluator, CountAccumulator counter, OperatorCountMetric metric)
        : WrappingEvaluatorExecution<TCandidate, TSearchSpace, TProblem>(childEvaluator)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            var objectives = ChildEvaluator.Evaluate(candidates, random, searchSpace, problem);
            counter.IncrementBy(metric == OperatorCountMetric.Calls ? 1 : candidates.Count);
            return objectives;
        }
    }
}

public static class CountingEvaluator
{
    public static CountingEvaluator<TCandidate> Create<TCandidate>(IEvaluator<TCandidate> childEvaluator, CountAccumulator counter, OperatorCountMetric metric) =>
        new(childEvaluator, counter, metric);
}

public static class EvaluatorCounterExtensions
{
    extension<TCandidate>(IEvaluator<TCandidate> evaluator)
    {
        public CountingEvaluator<TCandidate> CountCalls(CountAccumulator counter) => new(evaluator, counter, OperatorCountMetric.Calls);

        public CountingEvaluator<TCandidate> CountCalls(out CountAccumulator counter)
        {
            counter = new CountAccumulator();
            return evaluator.CountCalls(counter);
        }

        public CountingEvaluator<TCandidate> CountCandidates(CountAccumulator counter) =>
            new(evaluator, counter, OperatorCountMetric.Candidates);

        public CountingEvaluator<TCandidate> CountCandidates(out CountAccumulator counter)
        {
            counter = new CountAccumulator();
            return evaluator.CountCandidates(counter);
        }
    }
}
