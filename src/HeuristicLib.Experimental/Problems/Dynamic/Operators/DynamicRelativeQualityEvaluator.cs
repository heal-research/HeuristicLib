using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Problems.Dynamic;

public interface IBestKnownObjectiveProvider<TCandidate, TSearchSpace, in TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : DynamicProblem<TProblem, TCandidate, TSearchSpace>
{
    ObjectiveVector GetBestKnown(TProblem problem);
}

public sealed class FuncBestKnownObjectiveProvider<TCandidate, TSearchSpace, TProblem>(Func<TProblem, ObjectiveVector> getBestKnown)
    : IBestKnownObjectiveProvider<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : DynamicProblem<TProblem, TCandidate, TSearchSpace>
{
    public ObjectiveVector GetBestKnown(TProblem problem) => getBestKnown(problem);
}

/// <summary>
/// Normalizes the objective vectors produced by the child evaluator against the best-known objective vector of a
/// dynamic problem, refreshing that reference when evaluation observes a new epoch.
/// </summary>
/// <remarks>
/// The normalization reference belongs to one problem instance, so this evaluator is bound to that instance rather
/// than to a problem type, and a run over any other problem is refused when it evaluates.
/// </remarks>
public sealed record DynamicRelativeQualityEvaluator<TCandidate, TSearchSpace, TProblem>
    : WrappingOperator<Evaluation, TCandidate>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : DynamicProblem<TProblem, TCandidate, TSearchSpace>
{
    /// <summary>
    /// Gets the dynamic problem this evaluator is bound to. Executors observe its epoch clock and can only evaluate this problem.
    /// </summary>
    public TProblem SourceProblem { get; init; }

    /// <summary>
    /// Gets the provider that supplies the best-known objective vector for the current epoch.
    /// </summary>
    public IBestKnownObjectiveProvider<TCandidate, TSearchSpace, TProblem> BestKnownProvider { get; init; }

    public RelativeQualityZeroBestKnownPolicy ZeroBestKnownPolicy { get; init; } = RelativeQualityZeroBestKnownPolicy.SignedInfinity;

    public DynamicRelativeQualityEvaluator(IOperator<Evaluation, TCandidate> childEvaluator, TProblem problem, IBestKnownObjectiveProvider<TCandidate, TSearchSpace, TProblem> bestKnownProvider)
        : base(childEvaluator)
    {
        SourceProblem = problem;
        BestKnownProvider = bestKnownProvider;
    }

    protected override OperatorExecutorFactory CreateOperatorFactory()
    {
        var state = new ExecutionState();
        return new Factory(this, state);
    }

    private sealed class Factory(DynamicRelativeQualityEvaluator<TCandidate, TSearchSpace, TProblem> source, ExecutionState state) : OperatorExecutorFactory<Evaluation, TCandidate>
    {
        public override ExecutorFactory GetExecutorFactory<TRunSearchSpace, TRunProblem, TRunSearchState>(
            OperationExecutorAdapter<Evaluation, TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState> adapter) =>
            ExecutorFactory.FromFactory<IEvaluationExecutor<TCandidate, TRunSearchSpace, TRunProblem>>(scope =>
            {
                var typed = scope.For<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState>();
                var childEvaluator = typed.Resolve(source.ChildOperator);
                return new Executor<TRunSearchSpace, TRunProblem>(childEvaluator, source.SourceProblem, source.BestKnownProvider, source.ZeroBestKnownPolicy, state);
            });
    }

    private sealed class ExecutionState
    {
        public ObjectiveVector? BestKnown { get; set; }
        public int BestKnownEpoch { get; set; } = -1;
    }

    private sealed class Executor<TRunSearchSpace, TRunProblem> : WrappingEvaluationExecutor<TCandidate, TRunSearchSpace, TRunProblem>
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>
    {
        private readonly TProblem sourceProblem;
        private readonly IBestKnownObjectiveProvider<TCandidate, TSearchSpace, TProblem> bestKnownProvider;
        private readonly RelativeQualityZeroBestKnownPolicy zeroBestKnownPolicy;
        private readonly ExecutionState state;

        public Executor(IEvaluationExecutor<TCandidate, TRunSearchSpace, TRunProblem> childEvaluator, TProblem sourceProblem, IBestKnownObjectiveProvider<TCandidate, TSearchSpace, TProblem> bestKnownProvider, RelativeQualityZeroBestKnownPolicy zeroBestKnownPolicy, ExecutionState state)
            : base(childEvaluator)
        {
            this.sourceProblem = sourceProblem;
            this.bestKnownProvider = bestKnownProvider;
            this.zeroBestKnownPolicy = zeroBestKnownPolicy;
            this.state = state;
        }

        public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, TRunSearchSpace searchSpace, TRunProblem problem)
        {
            if (!ReferenceEquals(problem, sourceProblem))
                throw new InvalidOperationException("Dynamic relative quality evaluator executions can only evaluate the dynamic problem they were created for.");

            var objectiveVectors = ChildEvaluator.Evaluate(candidates, random, searchSpace, problem);
            // A deferred batch update is applied inside Evaluate. Read its reference afterwards, while that
            // environment is still current, instead of retaining an event subscription to the problem.
            if (state.BestKnownEpoch != sourceProblem.CurrentEpoch)
            {
                state.BestKnown = bestKnownProvider.GetBestKnown(sourceProblem);
                state.BestKnownEpoch = sourceProblem.CurrentEpoch;
            }
            var currentBestKnown = state.BestKnown!;
            return objectiveVectors
                .Select(objectiveVector => RelativeQuality.Normalize(objectiveVector, currentBestKnown, zeroBestKnownPolicy))
                .ToArray();
        }

    }
}

public static class DynamicRelativeQualityEvaluator
{
    public static DynamicRelativeQualityEvaluator<TCandidate, TSearchSpace, TProblem> Create<TCandidate, TSearchSpace, TProblem>(IOperator<Evaluation, TCandidate> childEvaluator, TProblem problem, IBestKnownObjectiveProvider<TCandidate, TSearchSpace, TProblem> bestKnownProvider)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : DynamicProblem<TProblem, TCandidate, TSearchSpace> =>
        new(childEvaluator, problem, bestKnownProvider);
}

public static class DynamicRelativeQualityEvaluatorExtensions
{
    extension<TCandidate, TSearchSpace, TProblem>(IOperator<Evaluation, TCandidate> evaluator)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : DynamicProblem<TProblem, TCandidate, TSearchSpace>
    {
        public DynamicRelativeQualityEvaluator<TCandidate, TSearchSpace, TProblem> ScaledToDynamicBestKnown(TProblem problem, IBestKnownObjectiveProvider<TCandidate, TSearchSpace, TProblem> bestKnownProvider) =>
            new(evaluator, problem, bestKnownProvider);

        public DynamicRelativeQualityEvaluator<TCandidate, TSearchSpace, TProblem> ScaledToDynamicBestKnown(TProblem problem, IBestKnownObjectiveProvider<TCandidate, TSearchSpace, TProblem> bestKnownProvider, RelativeQualityZeroBestKnownPolicy zeroBestKnownPolicy) =>
            new(evaluator, problem, bestKnownProvider) { ZeroBestKnownPolicy = zeroBestKnownPolicy };

        public DynamicRelativeQualityEvaluator<TCandidate, TSearchSpace, TProblem> ScaledToDynamicBestKnown(TProblem problem, Func<TProblem, ObjectiveVector> getBestKnown) =>
            new(evaluator, problem, new FuncBestKnownObjectiveProvider<TCandidate, TSearchSpace, TProblem>(getBestKnown));

        public DynamicRelativeQualityEvaluator<TCandidate, TSearchSpace, TProblem> ScaledToDynamicBestKnown(TProblem problem, Func<TProblem, ObjectiveVector> getBestKnown, RelativeQualityZeroBestKnownPolicy zeroBestKnownPolicy) =>
            new(evaluator, problem, new FuncBestKnownObjectiveProvider<TCandidate, TSearchSpace, TProblem>(getBestKnown)) { ZeroBestKnownPolicy = zeroBestKnownPolicy };
    }
}
