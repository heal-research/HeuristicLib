using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.MachineLearning;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Problems.Dynamic.Regression;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Problems.Dynamic;

/// <summary>
/// Supplies the ground-truth regressor for the current epoch of a dynamic regression problem.
/// </summary>
public interface IGroundTruthRegressorProvider<in TProblem>
{
    IRegressor GetGroundTruth(TProblem problem);
}

/// <summary>Normalizes regression loss against the ground-truth regressor on the current epoch's data.</summary>
public sealed record DynamicRelativeQualityRegressionEvaluator<TCandidate, TSearchSpace, TProblem> : WrappingEvaluator<TCandidate>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : DynamicRegressionProblem<TProblem, TCandidate, TSearchSpace>
{
    public TProblem SourceProblem { get; init; }
    public IGroundTruthRegressorProvider<TProblem> GroundTruthProvider { get; init; }
    public RelativeQualityZeroBestKnownPolicy ZeroGroundTruthLossPolicy { get; init; } = RelativeQualityZeroBestKnownPolicy.SignedInfinity;

    public DynamicRelativeQualityRegressionEvaluator(IEvaluator<TCandidate> childEvaluator, TProblem problem, IGroundTruthRegressorProvider<TProblem> groundTruthProvider) : base(childEvaluator)
    {
        SourceProblem = problem;
        GroundTruthProvider = groundTruthProvider;
    }

    protected override WrapperExecutionFactory<IEvaluatorExecution<TCandidate, TRunSearchSpace, TRunProblem>> CreateWrapperFactory<TRunSearchSpace, TRunProblem>()
    {
        if (SourceProblem.PredictionMetrics.Length != SourceProblem.Objective.Directions.Length)
            throw new InvalidOperationException("Dynamic relative regression quality requires every objective to be a regression prediction metric.");
        var state = new ExecutionState();
        return child => new Execution<TRunSearchSpace, TRunProblem>(child, SourceProblem, GroundTruthProvider, ZeroGroundTruthLossPolicy, state);
    }

    private sealed class ExecutionState
    {
        public int Epoch { get; set; } = -1;
        public ObjectiveVector? GroundTruthLoss { get; set; }
    }

    private sealed class Execution<TRunSearchSpace, TRunProblem>(
        IEvaluatorExecution<TCandidate, TRunSearchSpace, TRunProblem> child,
        TProblem sourceProblem,
        IGroundTruthRegressorProvider<TProblem> groundTruthProvider,
        RelativeQualityZeroBestKnownPolicy policy,
        ExecutionState state) : WrappingEvaluatorExecution<TCandidate, TRunSearchSpace, TRunProblem>(child)
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, HEAL.HeuristicLib.Problems.IProblem<TCandidate, TRunSearchSpace>
    {
        public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, TRunSearchSpace searchSpace, TRunProblem problem)
        {
            if (!ReferenceEquals(problem, sourceProblem))
                throw new InvalidOperationException("Dynamic relative regression quality evaluator executions can only evaluate the problem they were created for.");
            var objectives = ChildEvaluator.Evaluate(candidates, random, searchSpace, problem);
            if (state.Epoch != sourceProblem.CurrentEpoch)
            {
                state.GroundTruthLoss = EvaluateGroundTruthLoss();
                state.Epoch = sourceProblem.CurrentEpoch;
            }
            return objectives.Select(objective => RelativeQuality.Normalize(objective, state.GroundTruthLoss!, policy)).ToArray();
        }

        private ObjectiveVector EvaluateGroundTruthLoss()
        {
            var data = sourceProblem.CurrentData;
            var predictions = new double[data.RowCount];
            groundTruthProvider.GetGroundTruth(sourceProblem).Predict(data.Inputs, predictions);
            var values = new double[sourceProblem.PredictionMetrics.Length];
            for (var i = 0; i < values.Length; i++)
                values[i] = sourceProblem.PredictionMetrics[i].Evaluate(predictions, data.Target.Values.Span);
            return new ObjectiveVector(values);
        }
    }
}

public static class DynamicRelativeQualityRegressionEvaluator
{
    public static DynamicRelativeQualityRegressionEvaluator<TCandidate, TSearchSpace, TProblem> Create<TCandidate, TSearchSpace, TProblem>(IEvaluator<TCandidate> childEvaluator, DynamicRegressionProblem<TProblem, TCandidate, TSearchSpace> problem, IGroundTruthRegressorProvider<TProblem> groundTruthProvider)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : DynamicRegressionProblem<TProblem, TCandidate, TSearchSpace> => new(childEvaluator, (TProblem)problem, groundTruthProvider);
}

public static class DynamicRelativeQualityRegressionEvaluatorExtensions
{
    public static DynamicRelativeQualityRegressionEvaluator<TCandidate, TSearchSpace, TProblem> WithDynamicRelativeRegressionQuality<TCandidate, TSearchSpace, TProblem>(this IEvaluator<TCandidate> evaluator, DynamicRegressionProblem<TProblem, TCandidate, TSearchSpace> problem, IGroundTruthRegressorProvider<TProblem> groundTruthProvider)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : DynamicRegressionProblem<TProblem, TCandidate, TSearchSpace> => new(evaluator, (TProblem)problem, groundTruthProvider);

    public static DynamicRelativeQualityRegressionEvaluator<TCandidate, TSearchSpace, TProblem> WithDynamicRelativeRegressionQuality<TCandidate, TSearchSpace, TProblem>(this IEvaluator<TCandidate> evaluator, DynamicRegressionProblem<TProblem, TCandidate, TSearchSpace> problem, IGroundTruthRegressorProvider<TProblem> groundTruthProvider, RelativeQualityZeroBestKnownPolicy zeroGroundTruthLossPolicy)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : DynamicRegressionProblem<TProblem, TCandidate, TSearchSpace> => new(evaluator, (TProblem)problem, groundTruthProvider) { ZeroGroundTruthLossPolicy = zeroGroundTruthLossPolicy };
}
