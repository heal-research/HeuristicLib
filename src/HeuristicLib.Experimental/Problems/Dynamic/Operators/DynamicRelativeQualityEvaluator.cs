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
/// Each executor retains the problem supplied during resolution and keeps a separate normalization reference for its epochs.
/// </remarks>
public sealed record DynamicRelativeQualityEvaluator<TCandidate, TSearchSpace, TProblem>
    : Evaluator<TCandidate, TSearchSpace, TProblem>, IOperatorContract<TCandidate>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : DynamicProblem<TProblem, TCandidate, TSearchSpace>
{
    public IOperator<Evaluation, TCandidate> ChildOperator { get; init; }

    public ValueArray<ICandidateInvariant<TCandidate>> Requires => OperatorContractComposition.Requires<TCandidate>([ChildOperator]);

    /// <summary>
    /// Gets the provider that supplies the best-known objective vector for the current epoch.
    /// </summary>
    public IBestKnownObjectiveProvider<TCandidate, TSearchSpace, TProblem> BestKnownProvider { get; init; }

    public RelativeQualityZeroBestKnownPolicy ZeroBestKnownPolicy { get; init; } = RelativeQualityZeroBestKnownPolicy.SignedInfinity;

    public static Parameter<DynamicRelativeQualityEvaluator<TCandidate, TSearchSpace, TProblem>, IOperator<Evaluation, TCandidate>> ChildOperatorParameter { get; } =
        new(nameof(ChildOperator), static evaluator => evaluator.ChildOperator, static (evaluator, value) => evaluator with { ChildOperator = value });

    public static Parameter<DynamicRelativeQualityEvaluator<TCandidate, TSearchSpace, TProblem>, IBestKnownObjectiveProvider<TCandidate, TSearchSpace, TProblem>> BestKnownProviderParameter { get; } =
        new(nameof(BestKnownProvider), static evaluator => evaluator.BestKnownProvider, static (evaluator, value) => evaluator with { BestKnownProvider = value });

    public static Parameter<DynamicRelativeQualityEvaluator<TCandidate, TSearchSpace, TProblem>, RelativeQualityZeroBestKnownPolicy> ZeroBestKnownPolicyParameter { get; } =
        new(nameof(ZeroBestKnownPolicy), static evaluator => evaluator.ZeroBestKnownPolicy, static (evaluator, value) => evaluator with { ZeroBestKnownPolicy = value });

    public static IReadOnlyList<IParameter> Parameters { get; } = [ChildOperatorParameter, BestKnownProviderParameter, ZeroBestKnownPolicyParameter];

    public override IReadOnlyList<IParameter> GetParameters() => [.. base.GetParameters(), .. Parameters];

    public DynamicRelativeQualityEvaluator(IOperator<Evaluation, TCandidate> childEvaluator, IBestKnownObjectiveProvider<TCandidate, TSearchSpace, TProblem> bestKnownProvider)
    {
        ChildOperator = childEvaluator;
        BestKnownProvider = bestKnownProvider;
    }

    public bool? Ensures(ICandidateInvariant<TCandidate> invariant) => OperatorContractComposition.Ensures([ChildOperator], invariant);

    protected override IEvaluationExecutor<TCandidate> CreateExecutor<TSearchState>(
        ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState> scope) =>
        new Executor(scope.Resolve(ChildOperator), scope.Problem, BestKnownProvider, ZeroBestKnownPolicy);

    private sealed class Executor : EvaluationExecutor<TCandidate>
    {
        private readonly IBestKnownObjectiveProvider<TCandidate, TSearchSpace, TProblem> bestKnownProvider;
        private readonly RelativeQualityZeroBestKnownPolicy zeroBestKnownPolicy;
        private readonly IEvaluationExecutor<TCandidate> childEvaluator;
        private readonly TProblem problem;
        private ObjectiveVector? bestKnown;
        private int bestKnownEpoch = -1;

        public Executor(IEvaluationExecutor<TCandidate> childEvaluator, TProblem problem, IBestKnownObjectiveProvider<TCandidate, TSearchSpace, TProblem> bestKnownProvider, RelativeQualityZeroBestKnownPolicy zeroBestKnownPolicy)
        {
            this.bestKnownProvider = bestKnownProvider;
            this.zeroBestKnownPolicy = zeroBestKnownPolicy;
            this.childEvaluator = childEvaluator;
            this.problem = problem;
        }

        public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random)
        {
            var objectiveVectors = childEvaluator.Evaluate(candidates, random);
            // A deferred batch update is applied inside Evaluate. Read its reference afterwards, while that
            // environment is still current, instead of retaining an event subscription to the problem.
            if (bestKnownEpoch != problem.CurrentEpoch)
            {
                bestKnown = bestKnownProvider.GetBestKnown(problem);
                bestKnownEpoch = problem.CurrentEpoch;
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
    public static DynamicRelativeQualityEvaluator<TCandidate, TSearchSpace, TProblem> Create<TCandidate, TSearchSpace, TProblem>(IOperator<Evaluation, TCandidate> childEvaluator, IBestKnownObjectiveProvider<TCandidate, TSearchSpace, TProblem> bestKnownProvider)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : DynamicProblem<TProblem, TCandidate, TSearchSpace> =>
        new(childEvaluator, bestKnownProvider);
}

public static class DynamicRelativeQualityEvaluatorExtensions
{
    extension<TCandidate, TSearchSpace, TProblem>(IOperator<Evaluation, TCandidate> evaluator)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : DynamicProblem<TProblem, TCandidate, TSearchSpace>
    {
        public DynamicRelativeQualityEvaluator<TCandidate, TSearchSpace, TProblem> ScaledToDynamicBestKnown(IBestKnownObjectiveProvider<TCandidate, TSearchSpace, TProblem> bestKnownProvider) =>
            new(evaluator, bestKnownProvider);

        public DynamicRelativeQualityEvaluator<TCandidate, TSearchSpace, TProblem> ScaledToDynamicBestKnown(IBestKnownObjectiveProvider<TCandidate, TSearchSpace, TProblem> bestKnownProvider, RelativeQualityZeroBestKnownPolicy zeroBestKnownPolicy) =>
            new(evaluator, bestKnownProvider) { ZeroBestKnownPolicy = zeroBestKnownPolicy };

        public DynamicRelativeQualityEvaluator<TCandidate, TSearchSpace, TProblem> ScaledToDynamicBestKnown(Func<TProblem, ObjectiveVector> getBestKnown) =>
            new(evaluator, new FuncBestKnownObjectiveProvider<TCandidate, TSearchSpace, TProblem>(getBestKnown));

        public DynamicRelativeQualityEvaluator<TCandidate, TSearchSpace, TProblem> ScaledToDynamicBestKnown(Func<TProblem, ObjectiveVector> getBestKnown, RelativeQualityZeroBestKnownPolicy zeroBestKnownPolicy) =>
            new(evaluator, new FuncBestKnownObjectiveProvider<TCandidate, TSearchSpace, TProblem>(getBestKnown)) { ZeroBestKnownPolicy = zeroBestKnownPolicy };
    }
}
