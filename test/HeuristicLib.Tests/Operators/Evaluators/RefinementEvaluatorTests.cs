using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Operators.Refiners;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Operators.Evaluators;

/// <summary>
/// Pins the transient nature of refinement evaluation. The refined candidates exist only for the duration of one
/// evaluation, and the objective vectors they produce are returned positionally for the candidates that were supplied.
/// </summary>
public class RefinementEvaluatorTests
{
    [Fact]
    public void Evaluate_MeasuresTheRefinedCandidatesRatherThanTheSuppliedOnes()
    {
        var instance = CreateEvaluator().WithRefinement(new AddOffsetRefiner(10)).CreateExecutionInstance();
        var problem = CreateProblem();

        var objectiveVectors = instance.Evaluate([1, 2], RandomNumberGenerator.Create(1), problem.SearchSpace, problem);

        objectiveVectors.ShouldBe([new ObjectiveVector(11.0), new ObjectiveVector(12.0)]);
    }

    [Fact]
    public void Evaluate_ReturnsOneObjectiveVectorPerSuppliedCandidateInInputOrder()
    {
        var instance = CreateEvaluator().WithRefinement(new AddOffsetRefiner(10)).CreateExecutionInstance();
        var problem = CreateProblem();
        var candidates = new[] { 5, 1, 3 };

        var objectiveVectors = instance.Evaluate(candidates, RandomNumberGenerator.Create(1), problem.SearchSpace, problem);

        objectiveVectors.Count.ShouldBe(candidates.Length);
        objectiveVectors.ShouldBe([new ObjectiveVector(15.0), new ObjectiveVector(11.0), new ObjectiveVector(13.0)]);
    }

    /// <summary>
    /// The refined candidates are transient. Nothing in the evaluator contract can hand them back, which is what makes
    /// this composition Baldwinian: the caller keeps the candidates it supplied.
    /// </summary>
    [Fact]
    public void Evaluate_LeavesTheSuppliedCandidatesUntouched()
    {
        var instance = CreateEvaluator().WithRefinement(new AddOffsetRefiner(10)).CreateExecutionInstance();
        var problem = CreateProblem();
        var candidates = new[] { 1, 2 };

        instance.Evaluate(candidates, RandomNumberGenerator.Create(1), problem.SearchSpace, problem);

        candidates.ShouldBe([1, 2]);
    }

    [Fact]
    public void Evaluate_IssuesItsEvaluationsThroughTheChildEvaluator()
    {
        var counter = new ObservationCounter();
        var instance = CreateEvaluator().CountEvaluatedCandidates(counter).WithRefinement(new AddOffsetRefiner(10)).CreateExecutionInstance();
        var problem = CreateProblem();

        instance.Evaluate([1, 2, 3], RandomNumberGenerator.Create(1), problem.SearchSpace, problem);

        counter.CurrentCount.ShouldBe(3);
    }

    /// <summary>
    /// Evaluation accounting is decided by reference identity, so an algorithm sharing its evaluator configuration
    /// instance with a refinement evaluator resolves one execution instance and therefore one counter.
    /// </summary>
    [Fact]
    public void Evaluate_SharesOneCounterWithAnAlgorithmUsingTheSameEvaluatorInstance()
    {
        var counter = new ObservationCounter();
        var sharedEvaluator = CreateEvaluator().CountEvaluatedCandidates(counter);
        var registry = new ExecutionInstanceRegistry();
        var refining = registry.Resolve(sharedEvaluator.WithRefinement(new AddOffsetRefiner(10)));
        var direct = registry.Resolve(sharedEvaluator);
        var problem = CreateProblem();

        refining.Evaluate([1, 2], RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        direct.Evaluate([1, 2], RandomNumberGenerator.Create(2), problem.SearchSpace, problem);

        counter.CurrentCount.ShouldBe(4);
    }

    [Fact]
    public void RefinementEvaluator_RetainsBothConfiguredChildren()
    {
        var childEvaluator = CreateEvaluator();
        var refiner = new AddOffsetRefiner(10);

        var evaluator = childEvaluator.WithRefinement(refiner);

        evaluator.Evaluator.ShouldBeSameAs(childEvaluator);
        evaluator.Refiner.ShouldBeSameAs(refiner);
    }

    /// <summary>
    /// The evaluator is optional. Leaving it unset measures the refined candidates through an ordinary
    /// <c>ProblemEvaluator</c>, which is unwrapped and therefore invisible to budgets and analysis.
    /// </summary>
    [Fact]
    public void RefinementEvaluator_WithoutAConfiguredEvaluator_MeasuresThroughTheProblem()
    {
        var evaluator = RefinementEvaluator.Create<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(new AddOffsetRefiner(10));
        var problem = CreateProblem();

        evaluator.Evaluator.ShouldBe(new ProblemEvaluator<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>());

        var objectiveVectors = new ExecutionInstanceRegistry().Resolve(evaluator).Evaluate(
            [1, 2],
            RandomNumberGenerator.Create(1),
            problem.SearchSpace,
            problem);

        objectiveVectors.ShouldBe([new ObjectiveVector(11.0), new ObjectiveVector(12.0)]);
    }

    /// <summary>
    /// The inherited default evaluator is a value, so two separately created refinement evaluators over equal refiners
    /// remain structurally equal.
    /// </summary>
    [Fact]
    public void RefinementEvaluator_WithInheritedDefaultEvaluators_IsEqual()
    {
        var left = RefinementEvaluator.Create<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(new AddOffsetRefiner(10));
        var right = RefinementEvaluator.Create<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(new AddOffsetRefiner(10));

        left.ShouldBe(right);
        left.GetHashCode().ShouldBe(right.GetHashCode());
    }

    [Fact]
    public void RefinementEvaluator_WithEqualChildren_IsEqual()
    {
        var childEvaluator = CreateEvaluator();
        var left = childEvaluator.WithRefinement(new AddOffsetRefiner(10));
        var right = childEvaluator.WithRefinement(new AddOffsetRefiner(10));

        left.ShouldBe(right);
        left.GetHashCode().ShouldBe(right.GetHashCode());
        left.ShouldNotBe(childEvaluator.WithRefinement(new AddOffsetRefiner(20)));
        left.ShouldNotBe(RefinementEvaluator.Create<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(new AddOffsetRefiner(10)));
    }

    private static IEvaluator<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>> CreateEvaluator() =>
        new CandidateValueEvaluator();

    private static FuncProblem<int, DummySearchSpace<int>> CreateProblem() =>
        FuncProblem.Create(static (int candidate) => candidate, DummySearchSpace<int>.Instance, SingleObjective.Minimize);

    private sealed record CandidateValueEvaluator : SingleCandidateEvaluator<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>
    {
        public override ObjectiveVector EvaluateCandidate(int candidate, IRandomNumberGenerator random, DummySearchSpace<int> searchSpace, FuncProblem<int, DummySearchSpace<int>> problem) =>
            problem.Evaluate(candidate, random);
    }

    private sealed record AddOffsetRefiner(int Offset) : SingleCandidateRefiner<int, DummySearchSpace<int>>
    {
        public override int RefineCandidate(int candidate, IRandomNumberGenerator random, DummySearchSpace<int> searchSpace) => candidate + Offset;
    }
}
