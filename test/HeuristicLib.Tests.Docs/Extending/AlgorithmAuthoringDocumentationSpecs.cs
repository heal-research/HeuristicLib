#region iterative-algorithm-imports
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Random;
#endregion iterative-algorithm-imports
using HEAL.HeuristicLib.Problems.TestFunctions;

namespace HEAL.HeuristicLib.Tests.Docs.Extending;

#region iterative-algorithm-type
public sealed partial record SingleCreateAlgorithm
    : IterativeAlgorithm<SingleCreateAlgorithm, RealVector, SingleSolutionState<RealVector>>
{
    public required IOperator<Creation, RealVector> Creator { get; init; }

    public IOperator<Evaluation, RealVector> Evaluator { get; init; } = new ProblemEvaluator<RealVector>();

    protected override IterativeAlgorithmExecutor<SingleSolutionState<RealVector>> CreateExecutor<TSearchSpace, TProblem>(
        ResolutionScope<RealVector, TSearchSpace, TProblem, SingleSolutionState<RealVector>> scope)
    {
        var interceptor = scope.ResolveOptional(Interceptor);
        var creator = scope.Resolve(Creator);
        var evaluator = scope.Resolve(Evaluator);
        return new Executor(interceptor, creator, evaluator);
    }

    private sealed class Executor(
        IInterceptionExecutor<SingleSolutionState<RealVector>>? interceptor,
        ICreationExecutor<RealVector> creator,
        IEvaluationExecutor<RealVector> evaluator)
        : IterativeAlgorithmExecutor<SingleSolutionState<RealVector>>(interceptor)
    {
        protected override SingleSolutionState<RealVector> ExecuteStep(
            SingleSolutionState<RealVector>? previousState,
            IRandomNumberGenerator random)
        {
            var candidate = creator.Create(1, random)[0];
            var objective = evaluator.Evaluate([candidate], random)[0];

            return SingleSolutionState.From(candidate, objective);
        }
    }
}
#endregion iterative-algorithm-type

#region iterative-algorithm-parameters
public sealed partial record SingleCreateAlgorithm
{
    public static Parameter<SingleCreateAlgorithm, IOperator<Creation, RealVector>> CreatorParameter { get; } =
        new(nameof(Creator), static algorithm => algorithm.Creator, static (algorithm, value) => algorithm with { Creator = value });

    public static Parameter<SingleCreateAlgorithm, IOperator<Evaluation, RealVector>> EvaluatorParameter { get; } =
        new(nameof(Evaluator), static algorithm => algorithm.Evaluator, static (algorithm, value) => algorithm with { Evaluator = value });

    public new static IReadOnlyList<IParameter> Parameters { get; } = [CreatorParameter, EvaluatorParameter];

    public override IReadOnlyList<IParameter> GetParameters() => [.. base.GetParameters(), .. Parameters];
}
#endregion iterative-algorithm-parameters

public sealed class AlgorithmAuthoringDocumentationSpecs
{
    [Fact]
    public async Task TheIterativeAlgorithmCreatesAndEvaluatesOneCandidate()
    {
        var problem = new TestFunctionProblem(new SphereFunction(1));
        var algorithm = new SingleCreateAlgorithm { Creator = new UniformDistributedCreator() }
            .TerminatedAfterIterations(1);

        var state = await algorithm.RunToCompletionAsync(problem, RandomNumberGenerator.Create(42),
            ct: TestContext.Current.CancellationToken);

        var evaluated = state.EvaluatedCandidate;
        problem.SearchSpace.Contains(evaluated.Candidate).ShouldBeTrue();
        evaluated.ObjectiveVector[0].ShouldBe(evaluated.Candidate[0] * evaluated.Candidate[0]);
    }
}
