using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Operators.Refiners;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Operators.Evaluators;

public class RefinementEvaluatorTests
{
    [Fact]
    public void Evaluate_MeasuresTheRefinedCandidatesRatherThanTheSuppliedOnes()
    {
        var problem = CreateProblem();
        var executor = ResolutionScope.Create().Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(CreateEvaluator().AppliedAfterRefinement(new AddOffsetRefiner(10)), problem);

        var objectiveVectors = executor.Evaluate([1, 2], RandomNumberGenerator.Create(1));

        objectiveVectors.ShouldBe([new ObjectiveVector(11.0), new ObjectiveVector(12.0)]);
    }

    [Fact]
    public void Evaluate_ReturnsOneObjectiveVectorPerSuppliedCandidateInInputOrder()
    {
        var problem = CreateProblem();
        var executor = ResolutionScope.Create().Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(CreateEvaluator().AppliedAfterRefinement(new AddOffsetRefiner(10)), problem);
        var candidates = new[] { 5, 1, 3 };

        var objectiveVectors = executor.Evaluate(candidates, RandomNumberGenerator.Create(1));

        objectiveVectors.Count.ShouldBe(candidates.Length);
        objectiveVectors.ShouldBe([new ObjectiveVector(15.0), new ObjectiveVector(11.0), new ObjectiveVector(13.0)]);
    }

    // Nothing in the evaluator contract can hand the refined candidates back, which is what makes this composition
    // Baldwinian.
    [Fact]
    public void Evaluate_LeavesTheSuppliedCandidatesUntouched()
    {
        var problem = CreateProblem();
        var executor = ResolutionScope.Create().Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(CreateEvaluator().AppliedAfterRefinement(new AddOffsetRefiner(10)), problem);
        var candidates = new[] { 1, 2 };

        executor.Evaluate(candidates, RandomNumberGenerator.Create(1));

        candidates.ShouldBe([1, 2]);
    }

    [Fact]
    public void Evaluate_IssuesItsEvaluationsThroughTheChildEvaluator()
    {
        var observed = CreateEvaluator();
        var scope = ResolutionScope.Create();
        var problem = CreateProblem();
        var executor = scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(
            observed.AppliedAfterRefinement(new AddOffsetRefiner(10)).Counting(observed, out var counter, OperatorCountMetric.Candidates), problem);

        executor.Evaluate([1, 2, 3], RandomNumberGenerator.Create(1));

        scope.Results.Result(counter).CurrentCount.ShouldBe(3);
    }

    // Accounting follows reference identity, so sharing one evaluator configuration instance shares one counter.
    [Fact]
    public void Evaluate_SharesOneCounterWithAnAlgorithmUsingTheSameEvaluatorExecution()
    {
        var counter = new Counter();
        var sharedEvaluator = CreateEvaluator().Counted(counter, OperatorCountMetric.Candidates);
        var scope = ResolutionScope.Create();
        var problem = CreateProblem();
        var refining = scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(sharedEvaluator.AppliedAfterRefinement(new AddOffsetRefiner(10)), problem);
        var direct = scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(sharedEvaluator, problem);

        refining.Evaluate([1, 2], RandomNumberGenerator.Create(1));
        direct.Evaluate([1, 2], RandomNumberGenerator.Create(2));

        scope.Results.Result(counter).CurrentCount.ShouldBe(4);
    }

    // Refiners may resize a population, but an evaluator owes one objective vector per supplied candidate, so this
    // composition requires the size to be preserved.
    [Fact]
    public void Evaluate_WithARefinerThatResizesThePopulation_Throws()
    {
        var problem = CreateProblem();
        var executor = ResolutionScope.Create().Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(CreateEvaluator().AppliedAfterRefinement(new DropLastRefiner()), problem);

        Should.Throw<InvalidOperationException>(() => executor.Evaluate([1, 2, 3], RandomNumberGenerator.Create(1)));
    }

    [Fact]
    public void RefinementEvaluator_RetainsBothConfiguredChildren()
    {
        var childEvaluator = CreateEvaluator();
        var refiner = new AddOffsetRefiner(10);

        var evaluator = childEvaluator.AppliedAfterRefinement(refiner);

        evaluator.Evaluator.ShouldBeSameAs(childEvaluator);
        evaluator.Refiner.ShouldBeSameAs(refiner);
    }

    // The unwrapped default measures through the problem and is therefore invisible to budgets and analysis.
    [Fact]
    public void RefinementEvaluator_WithoutAConfiguredEvaluator_MeasuresThroughTheProblem()
    {
        var evaluator = RefinementEvaluator.Create(new AddOffsetRefiner(10));
        var problem = CreateProblem();

        evaluator.Evaluator.ShouldBe(new ProblemEvaluator<int>());

        var objectiveVectors = ResolutionScope.Create().Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(evaluator, problem).Evaluate(
            [1, 2],
            RandomNumberGenerator.Create(1));

        objectiveVectors.ShouldBe([new ObjectiveVector(11.0), new ObjectiveVector(12.0)]);
    }

    // The inherited default evaluator is a value, so separately created evaluators over equal refiners stay equal.
    [Fact]
    public void RefinementEvaluator_WithInheritedDefaultEvaluators_IsEqual()
    {
        var left = RefinementEvaluator.Create(new AddOffsetRefiner(10));
        var right = RefinementEvaluator.Create(new AddOffsetRefiner(10));

        left.ShouldBe(right);
        left.GetHashCode().ShouldBe(right.GetHashCode());
    }

    [Fact]
    public void RefinementEvaluator_WithEqualChildren_IsEqual()
    {
        var childEvaluator = CreateEvaluator();
        var left = childEvaluator.AppliedAfterRefinement(new AddOffsetRefiner(10));
        var right = childEvaluator.AppliedAfterRefinement(new AddOffsetRefiner(10));

        left.ShouldBe(right);
        left.GetHashCode().ShouldBe(right.GetHashCode());
        left.ShouldNotBe(childEvaluator.AppliedAfterRefinement(new AddOffsetRefiner(20)));
        left.ShouldNotBe(RefinementEvaluator.Create(new AddOffsetRefiner(10)));
    }

    private static IOperator<Evaluation, int> CreateEvaluator() =>
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
