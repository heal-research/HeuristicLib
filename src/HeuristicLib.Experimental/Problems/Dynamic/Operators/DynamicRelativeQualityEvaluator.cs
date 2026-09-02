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
/// dynamic problem, refreshing that reference whenever the problem's epoch changes.
/// </summary>
/// <remarks>
/// This wrapper reads its problem, which <see cref="WrappingEvaluator{TCandidate}"/> cannot express, so it implements
/// the role directly and reconciles the run's types with a type test.
/// </remarks>
public sealed record DynamicRelativeQualityEvaluator<TCandidate, TSearchSpace, TProblem>
    : WrappingEvaluator<TCandidate>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : DynamicProblem<TProblem, TCandidate, TSearchSpace>
{
    /// <summary>
    /// Gets the dynamic problem this evaluator is bound to. Execution instances observe its epoch clock and can only evaluate this problem.
    /// </summary>
    public TProblem SourceProblem { get; init; }

    /// <summary>
    /// Gets the provider that supplies the best-known objective vector for the current epoch.
    /// </summary>
    public IBestKnownObjectiveProvider<TCandidate, TSearchSpace, TProblem> BestKnownProvider { get; init; }

    /// <summary>
    /// Gets the policy applied when a best-known objective value is zero.
    /// </summary>
    public RelativeQualityZeroBestKnownPolicy ZeroBestKnownPolicy { get; init; } = RelativeQualityZeroBestKnownPolicy.SignedInfinity;

    public DynamicRelativeQualityEvaluator(IEvaluator<TCandidate> childEvaluator, TProblem problem, IBestKnownObjectiveProvider<TCandidate, TSearchSpace, TProblem> bestKnownProvider)
        : base(childEvaluator)
    {
        SourceProblem = problem;
        BestKnownProvider = bestKnownProvider;
    }

    /// <remarks>
    /// This wrapper reads its problem, so it binds it on its own type and reconciles with the run's here — the same
    /// shape <c>ObservableEvaluator</c> uses for its observers. The base stays agnostic, and this override is where
    /// the two meet.
    /// </remarks>
    protected override IEvaluatorInstance<TCandidate, TRunSearchSpace, TRunProblem> WrapExecutionInstance<TRunSearchSpace, TRunProblem>(IEvaluatorInstance<TCandidate, TRunSearchSpace, TRunProblem> childEvaluator)
    {
        if (childEvaluator is not IEvaluatorInstance<TCandidate, TSearchSpace, TProblem> boundChild
            || new Instance(boundChild, SourceProblem, BestKnownProvider, ZeroBestKnownPolicy) is not IEvaluatorInstance<TCandidate, TRunSearchSpace, TRunProblem> typed)
        {
            throw new InvalidOperationException(
                $"{GetType().Name} reads {typeof(TProblem).Name} and cannot run over {typeof(TRunProblem).Name}.");
        }

        return typed;
    }

    private sealed class Instance : WrappingEvaluatorInstance<TCandidate, TSearchSpace, TProblem>, IDisposable
    {
        private readonly TProblem sourceProblem;
        private readonly IBestKnownObjectiveProvider<TCandidate, TSearchSpace, TProblem> bestKnownProvider;
        private readonly RelativeQualityZeroBestKnownPolicy zeroBestKnownPolicy;
        private ObjectiveVector? bestKnown;

        public Instance(IEvaluatorInstance<TCandidate, TSearchSpace, TProblem> childEvaluator, TProblem sourceProblem, IBestKnownObjectiveProvider<TCandidate, TSearchSpace, TProblem> bestKnownProvider, RelativeQualityZeroBestKnownPolicy zeroBestKnownPolicy)
            : base(childEvaluator)
        {
            this.sourceProblem = sourceProblem;
            this.bestKnownProvider = bestKnownProvider;
            this.zeroBestKnownPolicy = zeroBestKnownPolicy;
            RefreshBestKnown(sourceProblem);
            sourceProblem.EpochClock.OnEpochChange += OnEpochChange;
        }

        public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            if (!ReferenceEquals(problem, sourceProblem))
                throw new InvalidOperationException("Dynamic relative quality evaluator instances can only evaluate the dynamic problem they were created for.");

            var currentBestKnown = bestKnown ?? throw new InvalidOperationException("No best-known objective vector is available.");

            return ChildEvaluator.Evaluate(candidates, random, searchSpace, problem)
                .Select(objectiveVector => RelativeQuality.Normalize(objectiveVector, currentBestKnown, zeroBestKnownPolicy))
                .ToArray();
        }

        public void Dispose() => sourceProblem.EpochClock.OnEpochChange -= OnEpochChange;

        private void OnEpochChange(object? sender, int epoch) => RefreshBestKnown(sourceProblem);

        private void RefreshBestKnown(TProblem problem) => bestKnown = bestKnownProvider.GetBestKnown(problem);
    }
}

public static class DynamicRelativeQualityEvaluator
{
    public static DynamicRelativeQualityEvaluator<TCandidate, TSearchSpace, TProblem> Create<TCandidate, TSearchSpace, TProblem>(IEvaluator<TCandidate> childEvaluator, TProblem problem, IBestKnownObjectiveProvider<TCandidate, TSearchSpace, TProblem> bestKnownProvider)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : DynamicProblem<TProblem, TCandidate, TSearchSpace> =>
        new(childEvaluator, problem, bestKnownProvider);
}

public static class DynamicRelativeQualityEvaluatorExtensions
{
    extension<TCandidate, TSearchSpace, TProblem>(IEvaluator<TCandidate> evaluator)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : DynamicProblem<TProblem, TCandidate, TSearchSpace>
    {
        public DynamicRelativeQualityEvaluator<TCandidate, TSearchSpace, TProblem> WithDynamicRelativeQuality(TProblem problem, IBestKnownObjectiveProvider<TCandidate, TSearchSpace, TProblem> bestKnownProvider) =>
            new(evaluator, problem, bestKnownProvider);

        public DynamicRelativeQualityEvaluator<TCandidate, TSearchSpace, TProblem> WithDynamicRelativeQuality(TProblem problem, IBestKnownObjectiveProvider<TCandidate, TSearchSpace, TProblem> bestKnownProvider, RelativeQualityZeroBestKnownPolicy zeroBestKnownPolicy) =>
            new(evaluator, problem, bestKnownProvider) { ZeroBestKnownPolicy = zeroBestKnownPolicy };

        public DynamicRelativeQualityEvaluator<TCandidate, TSearchSpace, TProblem> WithDynamicRelativeQuality(TProblem problem, Func<TProblem, ObjectiveVector> getBestKnown) =>
            new(evaluator, problem, new FuncBestKnownObjectiveProvider<TCandidate, TSearchSpace, TProblem>(getBestKnown));

        public DynamicRelativeQualityEvaluator<TCandidate, TSearchSpace, TProblem> WithDynamicRelativeQuality(TProblem problem, Func<TProblem, ObjectiveVector> getBestKnown, RelativeQualityZeroBestKnownPolicy zeroBestKnownPolicy) =>
            new(evaluator, problem, new FuncBestKnownObjectiveProvider<TCandidate, TSearchSpace, TProblem>(getBestKnown)) { ZeroBestKnownPolicy = zeroBestKnownPolicy };
    }
}
