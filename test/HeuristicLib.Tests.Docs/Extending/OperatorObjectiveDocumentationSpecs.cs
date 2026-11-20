#region operator-objective-imports
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
#endregion operator-objective-imports

namespace HEAL.HeuristicLib.Tests.Docs.Extending;

#region operator-objective-problem
public sealed class ProblemWithObjective<TCandidate, TSearchSpace>(
    IProblem<TCandidate, TSearchSpace> problem,
    ObjectiveDirections objective)
    : IProblem<TCandidate, TSearchSpace>
    where TSearchSpace : class, ISearchSpace<TCandidate>
{
    public TSearchSpace SearchSpace => problem.SearchSpace;

    public ObjectiveDirections Objective => objective;

    public IReadOnlyList<ObjectiveVector> Evaluate(
        IReadOnlyList<TCandidate> candidates,
        IRandomNumberGenerator random) =>
        problem.Evaluate(candidates, random);
}
#endregion operator-objective-problem

public sealed record WorstOfSampleAlgorithm
    : IterativeAlgorithm<
        WorstOfSampleAlgorithm,
        RealVector,
        BoundedRealVectorSearchSpace,
        IProblem<RealVector, BoundedRealVectorSearchSpace>,
        SingleSolutionState<RealVector>>
{
    public required IOperator<Creation, RealVector> Creator { get; init; }

    public IOperator<Evaluation, RealVector> Evaluator { get; init; } = new ProblemEvaluator<RealVector>();

    public IOperator<Selection, RealVector> Selector { get; init; } = new BestSelector<RealVector>();

    public int SampleSize { get; init; } = 5;

    public static Parameter<WorstOfSampleAlgorithm, IOperator<Creation, RealVector>> CreatorParameter { get; } =
        new(nameof(Creator), static algorithm => algorithm.Creator, static (algorithm, value) => algorithm with { Creator = value });

    public static Parameter<WorstOfSampleAlgorithm, IOperator<Evaluation, RealVector>> EvaluatorParameter { get; } =
        new(nameof(Evaluator), static algorithm => algorithm.Evaluator, static (algorithm, value) => algorithm with { Evaluator = value });

    public static Parameter<WorstOfSampleAlgorithm, int> SampleSizeParameter { get; } =
        new(nameof(SampleSize), static algorithm => algorithm.SampleSize, static (algorithm, value) => algorithm with { SampleSize = value });

    #region operator-objective-resolution
    public static Parameter<WorstOfSampleAlgorithm, IOperator<Selection, RealVector>> SelectorParameter { get; } =
        new(nameof(Selector), static algorithm => algorithm.Selector, static (algorithm, value) => algorithm with { Selector = value })
        {
            // The selector is resolved under the delegating problem below, so preflight must ask it about that problem.
            ChildExecution = static _ => ExecutionSignature.For<
                BoundedRealVectorSearchSpace,
                ProblemWithObjective<RealVector, BoundedRealVectorSearchSpace>,
                SingleSolutionState<RealVector>>()
        };

    protected override IterativeAlgorithmExecutor<SingleSolutionState<RealVector>> CreateExecutor(
        ResolutionScope<RealVector, BoundedRealVectorSearchSpace, IProblem<RealVector, BoundedRealVectorSearchSpace>, SingleSolutionState<RealVector>> scope)
    {
        var interceptor = scope.ResolveOptional(Interceptor);
        var creator = scope.Resolve(Creator);
        var evaluator = scope.Resolve(Evaluator);

        // One problem object per CreateExecutor: it is the objective of everything resolved under it.
        var maximizing = new ProblemWithObjective<RealVector, BoundedRealVectorSearchSpace>(scope.Problem, SingleObjective.Maximize);
        var selector = scope
            .For<RealVector, BoundedRealVectorSearchSpace, ProblemWithObjective<RealVector, BoundedRealVectorSearchSpace>, SingleSolutionState<RealVector>>(maximizing)
            .Resolve(Selector);

        return new Executor(interceptor, creator, evaluator, selector, SampleSize);
    }
    #endregion operator-objective-resolution

    public new static IReadOnlyList<IParameter> Parameters { get; } = [CreatorParameter, EvaluatorParameter, SelectorParameter, SampleSizeParameter];

    public override IReadOnlyList<IParameter> GetParameters() => [.. base.GetParameters(), .. Parameters];

    private sealed class Executor(
        IInterceptionExecutor<SingleSolutionState<RealVector>>? interceptor,
        ICreationExecutor<RealVector> creator,
        IEvaluationExecutor<RealVector> evaluator,
        ISelectionExecutor<RealVector> selector,
        int sampleSize)
        : IterativeAlgorithmExecutor<SingleSolutionState<RealVector>>(interceptor)
    {
        protected override SingleSolutionState<RealVector> ExecuteStep(
            SingleSolutionState<RealVector>? previousState,
            IRandomNumberGenerator random)
        {
            var candidates = creator.Create(sampleSize, random);
            var objectives = evaluator.Evaluate(candidates, random);
            var sample = candidates.Zip(objectives, (candidate, objective) => EvaluatedCandidate.From(candidate, objective)).ToArray();

            return SingleSolutionState.From(selector.Select(sample, 1, random)[0]);
        }
    }
}

// A selector written for the delegating problem alone, as one reading something only that problem has would be.
public sealed record DelegatedProblemSelector
    : StatelessSelector<RealVector, BoundedRealVectorSearchSpace, ProblemWithObjective<RealVector, BoundedRealVectorSearchSpace>>
{
    public override IReadOnlyList<EvaluatedCandidate<RealVector>> Select(
        IReadOnlyList<EvaluatedCandidate<RealVector>> population,
        ObjectiveDirections objective,
        int count,
        IRandomNumberGenerator random,
        BoundedRealVectorSearchSpace searchSpace,
        ProblemWithObjective<RealVector, BoundedRealVectorSearchSpace> problem) =>
        BestSelector.Select(population, objective, count);
}

public sealed class OperatorObjectiveDocumentationSpecs
{
    private static readonly IReadOnlyList<RealVector> Sample = [new RealVector(1.0), new RealVector(3.0), new RealVector(2.0)];

    private static FuncProblem<RealVector, BoundedRealVectorSearchSpace> MinimizingProblem() =>
        FuncProblem.Create(
            (RealVector candidate) => candidate[0] * candidate[0],
            new BoundedRealVectorSearchSpace(1, -10.0, 10.0),
            SingleObjective.Minimize);

    [Fact]
    public async Task TheSelectorRanksUnderTheObjectiveOfTheProblemItIsResolvedUnder()
    {
        var problem = MinimizingProblem();
        var algorithm = new WorstOfSampleAlgorithm
        {
            Creator = PredefinedCandidatesCreator.Create(Sample, new UniformDistributedCreator()),
            SampleSize = Sample.Count
        }.TerminatedAfterIterations(1);

        var state = await algorithm.RunToCompletionAsync(problem, RandomNumberGenerator.Create(42),
            ct: TestContext.Current.CancellationToken);

        // The problem minimizes, so the sample's best is 1.0. The selector ranks under the maximizing problem it was resolved under.
        state.EvaluatedCandidate.Candidate.ShouldBe(new RealVector(3.0));
        problem.Objective.ShouldBe(SingleObjective.Minimize);
    }

    [Fact]
    public async Task ASelectorWrittenForTheDelegatingProblemPassesPreflightThroughTheParameterMapping()
    {
        var problem = MinimizingProblem();
        var algorithm = new WorstOfSampleAlgorithm
        {
            Creator = PredefinedCandidatesCreator.Create(Sample, new UniformDistributedCreator()),
            Selector = new DelegatedProblemSelector(),
            SampleSize = Sample.Count
        }.TerminatedAfterIterations(1);

        var state = await algorithm.RunToCompletionAsync(problem, RandomNumberGenerator.Create(42),
            ct: TestContext.Current.CancellationToken);

        state.EvaluatedCandidate.Candidate.ShouldBe(new RealVector(3.0));
    }

    [Fact]
    public void ThatSelectorWouldBeRejectedUnderTheSignatureTheAlgorithmDeclares()
    {
        var selector = new DelegatedProblemSelector();
        var declaredByTheAlgorithm = ExecutionSignature.For<
            BoundedRealVectorSearchSpace,
            IProblem<RealVector, BoundedRealVectorSearchSpace>,
            SingleSolutionState<RealVector>>();
        var mappedBySelectorParameter = WorstOfSampleAlgorithm.SelectorParameter
            .ChildExecution!(declaredByTheAlgorithm);

        // Without the mapping, preflight hands the selector the declared signature, which does not fit it.
        declaredByTheAlgorithm.Supports(selector).ShouldBeFalse();
        mappedBySelectorParameter.Supports(selector).ShouldBeTrue();
    }
}
