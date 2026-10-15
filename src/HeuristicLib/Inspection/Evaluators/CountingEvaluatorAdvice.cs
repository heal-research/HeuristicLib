using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Inspection;

public sealed class CountingEvaluatorAdvice<TCandidate>
    : EvaluatorAdvice<TCandidate>
{
    public CountAccumulator Counter { get; }
    public OperatorCountMetric Metric { get; }

    public CountingEvaluatorAdvice(CountAccumulator counter, OperatorCountMetric metric)
    {
        Counter = counter;
        Metric = metric;
    }

    public override WrapperExecutionFactory<IEvaluatorExecution<TCandidate, TRunSearchSpace, TRunProblem>> CreateWrapperFactory<TRunSearchSpace, TRunProblem>(IEvaluator<TCandidate> evaluator) =>
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

public sealed record EvaluatorCounting<TCandidate>(OperatorCountMetric Metric) : IOperatorCounting<IEvaluator<TCandidate>>
{
    public Advice<IEvaluator<TCandidate>> CreateAdvice(CountAccumulator counter) =>
        new CountingEvaluatorAdvice<TCandidate>(counter, Metric);
}

public static class EvaluatorCounterExtensions
{
    extension(Aspect)
    {
        /// <summary>Counts every call to one chosen evaluator.</summary>
        public static Aspect CountCalls<TCandidate>(IEvaluator<TCandidate> evaluator, CountAccumulator counter) =>
            Aspect.Create(Pointcut.Reference(evaluator), new CountingEvaluatorAdvice<TCandidate>(counter, OperatorCountMetric.Calls));

        /// <summary>Counts the candidates one chosen evaluator evaluates.</summary>
        public static Aspect CountCandidates<TCandidate>(IEvaluator<TCandidate> evaluator, CountAccumulator counter) =>
            Aspect.Create(Pointcut.Reference(evaluator), new CountingEvaluatorAdvice<TCandidate>(counter, OperatorCountMetric.Candidates));

        public static Aspect CountCandidates<TCandidate>(IEvaluator<TCandidate> evaluator, out CountAccumulator counter)
        {
            counter = new CountAccumulator();
            return Aspect.CountCandidates(evaluator, counter);
        }
    }
}
