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

    // Nothing in the evaluator contract can hand the refined candidates back, which is what makes this composition
    // Baldwinian.
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

    // Accounting follows reference identity, so sharing one evaluator configuration instance shares one counter.
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

    // Refiners may resize a population, but an evaluator owes one objective vector per supplied candidate, so this
    // composition requires the size to be preserved.
    [Fact]
    public void Evaluate_WithARefinerThatResizesThePopulation_Throws()
    {
        var instance = CreateEvaluator().WithRefinement(new DropLastRefiner()).CreateExecutionInstance();
        var problem = CreateProblem();

        Should.Throw<InvalidOperationException>(() => instance.Evaluate([1, 2, 3], RandomNumberGenerator.Create(1), problem.SearchSpace, problem));
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

    // The unwrapped default measures through the problem and is therefore invisible to budgets and analysis.
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

    // The inherited default evaluator is a value, so separately created evaluators over equal refiners stay equal.
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

    private sealed record DropLastRefiner : StatelessRefiner<int, DummySearchSpace<int>>
    {
        public override IReadOnlyList<int> Refine(IReadOnlyList<int> candidates, IRandomNumberGenerator random, DummySearchSpace<int> searchSpace) =>
            [.. candidates.Take(Math.Max(0, candidates.Count - 1))];
    }
}
