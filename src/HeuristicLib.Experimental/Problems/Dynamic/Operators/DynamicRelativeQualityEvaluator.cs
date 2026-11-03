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

    protected override IExecutor CreateExecutor<TRunSearchSpace, TRunProblem, TRunSearchState>(
        ResolutionScope<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState> scope)
    {
        var childEvaluator = scope.Resolve(ChildOperator);
        return new Executor<TRunSearchSpace, TRunProblem>(childEvaluator, SourceProblem, BestKnownProvider, ZeroBestKnownPolicy);
    }

    private sealed class Executor<TRunSearchSpace, TRunProblem> : IEvaluationExecutor<TCandidate, TRunSearchSpace, TRunProblem>
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>
    {
        private readonly TProblem sourceProblem;
        private readonly IBestKnownObjectiveProvider<TCandidate, TSearchSpace, TProblem> bestKnownProvider;
        private readonly RelativeQualityZeroBestKnownPolicy zeroBestKnownPolicy;
        private readonly IEvaluationExecutor<TCandidate, TRunSearchSpace, TRunProblem> childEvaluator;
        private ObjectiveVector? bestKnown;
        private int bestKnownEpoch = -1;

        public Executor(IEvaluationExecutor<TCandidate, TRunSearchSpace, TRunProblem> childEvaluator, TProblem sourceProblem, IBestKnownObjectiveProvider<TCandidate, TSearchSpace, TProblem> bestKnownProvider, RelativeQualityZeroBestKnownPolicy zeroBestKnownPolicy)
        {
            this.sourceProblem = sourceProblem;
            this.bestKnownProvider = bestKnownProvider;
            this.zeroBestKnownPolicy = zeroBestKnownPolicy;
            this.childEvaluator = childEvaluator;
        }

        public IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, TRunSearchSpace searchSpace, TRunProblem problem)
        {
            if (!ReferenceEquals(problem, sourceProblem))
                throw new InvalidOperationException("Dynamic relative quality evaluator executions can only evaluate the dynamic problem they were created for.");

            var objectiveVectors = childEvaluator.Evaluate(candidates, random, searchSpace, problem);
            // A deferred batch update is applied inside Evaluate. Read its reference afterwards, while that
            // environment is still current, instead of retaining an event subscription to the problem.
            if (bestKnownEpoch != sourceProblem.CurrentEpoch)
            {
                bestKnown = bestKnownProvider.GetBestKnown(sourceProblem);
                bestKnownEpoch = sourceProblem.CurrentEpoch;
            }
            var currentBestKnown = bestKnown!;
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
