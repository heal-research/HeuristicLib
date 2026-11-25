#region agnostic-wrapper-imports
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
#endregion agnostic-wrapper-imports

namespace HEAL.HeuristicLib.Tests.Docs.Extending;

#region agnostic-wrapper-type
public interface IHasKnownOptimum
{
    double Optimum { get; }
}

public sealed record OptimumGapEvaluator<TCandidate> : WrappingOperator<Evaluation, TCandidate>
{
    public OptimumGapEvaluator(IOperator<Evaluation, TCandidate> childEvaluator)
        : base(childEvaluator)
    {
    }

    public override bool Supports(ExecutionSignature execution) =>
        base.Supports(execution)
        && execution.Problem.IsAssignableTo(typeof(IHasKnownOptimum));

    protected override IExecutor CreateExecutor<TSearchSpace, TProblem, TSearchState>(
        ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState> scope)
    {
        // Supports has already accepted the run's problem, so it has a known optimum.
        if (scope.Problem is not IHasKnownOptimum problem)
            throw ExecutionSignature.Mismatch(this,
                ExecutionSignature.Describe(typeof(ISearchSpace<TCandidate>), typeof(IHasKnownOptimum)),
                ExecutionSignature.Describe(typeof(TSearchSpace), typeof(TProblem), typeof(TSearchState)));

        // The known optimum is one number, so the wrapper serves single-objective problems only.
        if (scope.Problem.Objective.Directions.Length != 1)
            throw new InvalidOperationException(
                $"{nameof(OptimumGapEvaluator<TCandidate>)} subtracts one known optimum, so it needs a single-objective problem.");

        return new Executor(scope.Resolve(ChildOperator), problem);
    }

    private sealed class Executor(IEvaluationExecutor<TCandidate> child, IHasKnownOptimum problem)
        : EvaluationExecutor<TCandidate>
    {
        public override IReadOnlyList<ObjectiveVector> Evaluate(
            IReadOnlyList<TCandidate> candidates,
            IRandomNumberGenerator random) =>
            [.. child.Evaluate(candidates, random).Select(objective => new ObjectiveVector(objective[0] - problem.Optimum))];
    }
}
#endregion agnostic-wrapper-type

#region agnostic-wrapper-problem
public sealed class ShiftedSphereProblem()
    : Problem<ShiftedSphereProblem, RealVector, BoundedRealVectorSearchSpace>(
        SingleObjective.Minimize, new BoundedRealVectorSearchSpace(1, -10.0, 10.0)),
    IHasKnownOptimum
{
    public double Optimum => 1.0;

    public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<RealVector> candidates, IRandomNumberGenerator random) =>
        [.. candidates.Select(candidate => new ObjectiveVector((candidate[0] * candidate[0]) + Optimum))];
}
#endregion agnostic-wrapper-problem

public sealed class TwoObjectiveShiftedProblem()
    : Problem<TwoObjectiveShiftedProblem, RealVector, BoundedRealVectorSearchSpace>(
        MultiObjective.Minimize(2), new BoundedRealVectorSearchSpace(1, -10.0, 10.0)),
    IHasKnownOptimum
{
    public double Optimum => 1.0;

    public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<RealVector> candidates, IRandomNumberGenerator random) =>
        [.. candidates.Select(candidate => new ObjectiveVector((candidate[0] * candidate[0]) + Optimum, candidate[0]))];
}

public sealed class AgnosticWrapperDocumentationSpecs
{
    [Fact]
    public async Task TheWrapperReadsTheInterfaceOfTheRunsProblem()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        #region agnostic-wrapper-run
        var problem = new ShiftedSphereProblem();
        var algorithm = new GeneticAlgorithm<RealVector>
        {
            Creator = new UniformDistributedCreator(),
            Crossover = SelectFirstParentCrossover<RealVector>.Instance,
            Mutator = new NoChangeMutator<RealVector>(),
            Evaluator = new OptimumGapEvaluator<RealVector>(new ProblemEvaluator<RealVector>()),
            PopulationSize = 4,
            MaximumGenerations = 2
        };

        var finalState = await algorithm.RunToCompletionAsync(problem, RandomNumberGenerator.Create(seed: 42),
            ct: cancellationToken);
        #endregion agnostic-wrapper-run

        finalState.Population.EvaluatedCandidates.ShouldNotBeEmpty();
        foreach (var evaluated in finalState.Population.EvaluatedCandidates)
            evaluated.ObjectiveVector[0].ShouldBe(evaluated.Candidate[0] * evaluated.Candidate[0], 1e-9);
    }

    [Fact]
    public void ARunTypedByTheProblemInterfaceHidesTheCapability()
    {
        var algorithm = new GeneticAlgorithm<RealVector>
        {
            Creator = new UniformDistributedCreator(),
            Crossover = SelectFirstParentCrossover<RealVector>.Instance,
            Mutator = new NoChangeMutator<RealVector>(),
            Evaluator = new OptimumGapEvaluator<RealVector>(new ProblemEvaluator<RealVector>())
        };
        IProblem<RealVector, BoundedRealVectorSearchSpace> viaInterface = new ShiftedSphereProblem();

        Should.Throw<InvalidOperationException>(() => algorithm.CreateRun(viaInterface, RandomNumberGenerator.Create(seed: 42)));
    }

    [Fact]
    public void ARunWhoseProblemLacksTheInterfaceIsNotSupported()
    {
        var wrapper = new OptimumGapEvaluator<RealVector>(new ProblemEvaluator<RealVector>());
        var withOptimum = ExecutionSignature.For<BoundedRealVectorSearchSpace, ShiftedSphereProblem, PopulationState<RealVector>>();
        var withoutOptimum = ExecutionSignature.For<
            BoundedRealVectorSearchSpace,
            FuncProblem<RealVector, BoundedRealVectorSearchSpace>,
            PopulationState<RealVector>>();

        wrapper.Supports(withOptimum).ShouldBeTrue();
        wrapper.Supports(withoutOptimum).ShouldBeFalse();
    }

    [Fact]
    public async Task AProblemWithSeveralObjectivesIsRejectedWhenTheExecutorIsBuilt()
    {
        var algorithm = new GeneticAlgorithm<RealVector>
        {
            Creator = new UniformDistributedCreator(),
            Crossover = SelectFirstParentCrossover<RealVector>.Instance,
            Mutator = new NoChangeMutator<RealVector>(),
            Evaluator = new OptimumGapEvaluator<RealVector>(new ProblemEvaluator<RealVector>()),
            PopulationSize = 4,
            MaximumGenerations = 2
        };
        var problem = new TwoObjectiveShiftedProblem();

        // The problem implements the interface, so the run passes preflight; the objective count is a value, not a type.
        var exception = await Should.ThrowAsync<InvalidOperationException>(() =>
            algorithm.RunToCompletionAsync(problem, RandomNumberGenerator.Create(seed: 42), ct: TestContext.Current.CancellationToken));

        exception.Message.ShouldContain("single-objective");
    }
}
