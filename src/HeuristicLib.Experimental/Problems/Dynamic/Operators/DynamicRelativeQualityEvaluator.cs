using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Problems.Dynamic.Operators;

public interface IBestKnownObjectiveProvider<TCandidate, TSearchSpace, in TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : DynamicProblem<TCandidate, TSearchSpace>
{
    ObjectiveVector GetBestKnown(TProblem problem);
}

public sealed class FuncBestKnownObjectiveProvider<TCandidate, TSearchSpace, TProblem>(
    Func<TProblem, ObjectiveVector> getBestKnown)
    : IBestKnownObjectiveProvider<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : DynamicProblem<TCandidate, TSearchSpace>
{
    public ObjectiveVector GetBestKnown(TProblem problem) => getBestKnown(problem);
}

public record DynamicRelativeQualityEvaluator<TCandidate, TSearchSpace, TProblem>
    : WrappingEvaluator<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : DynamicProblem<TCandidate, TSearchSpace>
{
    private readonly TProblem sourceProblem;
    private readonly IBestKnownObjectiveProvider<TCandidate, TSearchSpace, TProblem> bestKnownProvider;
    private readonly RelativeQualityZeroBestKnownPolicy zeroBestKnownPolicy;

    public DynamicRelativeQualityEvaluator(IEvaluator<TCandidate, TSearchSpace, TProblem> evaluator,
                                           TProblem problem,
                                           IBestKnownObjectiveProvider<TCandidate, TSearchSpace, TProblem> bestKnownProvider,
                                           RelativeQualityZeroBestKnownPolicy zeroBestKnownPolicy =
                                               RelativeQualityZeroBestKnownPolicy.SignedInfinity)
        : base(evaluator)
    {
        sourceProblem = problem;
        this.bestKnownProvider = bestKnownProvider;
        this.zeroBestKnownPolicy = zeroBestKnownPolicy;
    }

    protected override WrappingEvaluatorInstance<TCandidate, TSearchSpace, TProblem> CreateEvaluatorInstance(IEvaluatorInstance<TCandidate, TSearchSpace, TProblem> innerEvaluator) =>
        new Instance(innerEvaluator, sourceProblem, bestKnownProvider, zeroBestKnownPolicy);

    private sealed class Instance : WrappingEvaluatorInstance<TCandidate, TSearchSpace, TProblem>, IDisposable
    {
        private readonly TProblem sourceProblem;
        private readonly IBestKnownObjectiveProvider<TCandidate, TSearchSpace, TProblem> bestKnownProvider;
        private readonly RelativeQualityZeroBestKnownPolicy zeroBestKnownPolicy;
        private ObjectiveVector? bestKnown;

        public Instance(IEvaluatorInstance<TCandidate, TSearchSpace, TProblem> innerEvaluator,
                        TProblem sourceProblem,
                        IBestKnownObjectiveProvider<TCandidate, TSearchSpace, TProblem> bestKnownProvider,
                        RelativeQualityZeroBestKnownPolicy zeroBestKnownPolicy)
            : base(innerEvaluator)
        {
            this.sourceProblem = sourceProblem;
            this.bestKnownProvider = bestKnownProvider;
            this.zeroBestKnownPolicy = zeroBestKnownPolicy;
            RefreshBestKnown(sourceProblem);
            sourceProblem.EpochClock.OnEpochChange += OnEpochChange;
        }

        public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates,
                                                                IRandomNumberGenerator random,
                                                                TSearchSpace searchSpace,
                                                                TProblem problem)
        {
            if (!ReferenceEquals(problem, sourceProblem))
            {
                throw new InvalidOperationException("Dynamic relative quality evaluator instances can only evaluate the dynamic problem they were created for.");
            }

            var currentBestKnown = bestKnown ?? throw new InvalidOperationException("No best-known objective vector is available.");

            return InnerEvaluator.Evaluate(candidates, random, searchSpace, problem)
                                 .Select(objective => RelativeQuality.Normalize(objective, currentBestKnown, zeroBestKnownPolicy))
                                 .ToArray();
        }

        public void Dispose() => sourceProblem.EpochClock.OnEpochChange -= OnEpochChange;

        private void OnEpochChange(object? sender, int epoch) => RefreshBestKnown(sourceProblem);

        private void RefreshBestKnown(TProblem problem) => bestKnown = bestKnownProvider.GetBestKnown(problem);
    }
}

public static class DynamicRelativeQualityEvaluatorExtensions
{
    extension<TCandidate, TSearchSpace, TProblem>(IEvaluator<TCandidate, TSearchSpace, TProblem> evaluator)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : DynamicProblem<TCandidate, TSearchSpace>
    {
        public DynamicRelativeQualityEvaluator<TCandidate, TSearchSpace, TProblem> WithDynamicRelativeQuality(
            TProblem problem,
            IBestKnownObjectiveProvider<TCandidate, TSearchSpace, TProblem> bestKnownProvider,
            RelativeQualityZeroBestKnownPolicy zeroBestKnownPolicy =
                RelativeQualityZeroBestKnownPolicy.SignedInfinity)
            => new(evaluator, problem, bestKnownProvider, zeroBestKnownPolicy);

        public DynamicRelativeQualityEvaluator<TCandidate, TSearchSpace, TProblem> WithDynamicRelativeQuality(
            TProblem problem,
            Func<TProblem, ObjectiveVector> getBestKnown,
            RelativeQualityZeroBestKnownPolicy zeroBestKnownPolicy =
                RelativeQualityZeroBestKnownPolicy.SignedInfinity)
            => new(evaluator, problem,
                new FuncBestKnownObjectiveProvider<TCandidate, TSearchSpace, TProblem>(getBestKnown),
                zeroBestKnownPolicy);
    }
}
