using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Operators.Refiners;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Operators;

public sealed class ConcreteOperatorResolutionTests
{
    [Theory]
    [InlineData(nameof(TransformedCreator<int>))]
    [InlineData(nameof(TransformedCrossover<int>))]
    [InlineData(nameof(RefinementEvaluator<int>))]
    [InlineData(nameof(ImprovementCheckingRefiner<int>))]
    [InlineData(nameof(EliteSelector<int>))]
    [InlineData(nameof(GenderSpecificSelector<int>))]
    public void Rebinding_ObservesEachChildOnlyInItsRequestingContext(string kind)
    {
        IOperator<Creation, int> creator = new ConstantCreator();
        IOperator<Crossover, int> crossover = SelectFirstParentCrossover<int>.Instance;
        IOperator<Mutation, int> mutator = new AddOneMutator();
        IOperator<Refinement, int> refiner = new SubtractOneRefiner();
        IOperator<Evaluation, int> evaluator = new ProblemEvaluator<int>();
        IOperator<Selection, int> female = new RangeSelector(0);
        IOperator<Selection, int> male = new RangeSelector(1);
        var transformedCreator = new TransformedCreator<int>(creator, mutator);
        var transformedCrossover = new TransformedCrossover<int>(crossover, mutator);
        var refiningEvaluator = new RefinementEvaluator<int>(refiner) { Evaluator = evaluator };
        var checkingRefiner = new ImprovementCheckingRefiner<int>(refiner) { Evaluator = evaluator };
        var eliteSelector = new EliteSelector<int>(female);
        var genderSelector = new GenderSpecificSelector<int>(female, male);
        var counters = Enumerable.Range(0, 7).Select(_ => new Counter()).ToArray();
        ISubstitution[] counting =
        [
            CountingIfHeld(creator, counters[0]), CountingIfHeld(crossover, counters[1]), CountingIfHeld(mutator, counters[2]),
            CountingIfHeld(refiner, counters[3]), CountingIfHeld(evaluator, counters[4]), CountingIfHeld(female, counters[5]),
            CountingIfHeld(male, counters[6])
        ];
        var problem = FuncProblem.Create(static (int candidate) => candidate, DummySearchSpace<int>.Instance, SingleObjective.Minimize);
        var random = RandomNumberGenerator.Create(42);
        var population = new[] { 10, 20, 30 }.Select(candidate => EvaluatedCandidate.From(candidate, new ObjectiveVector(candidate))).ToArray();
        var countedTransformedCreator = transformedCreator.Substitute(counting);
        var countedTransformedCrossover = transformedCrossover.Substitute(counting);
        var countedRefiningEvaluator = refiningEvaluator.Substitute(counting);
        var countedCheckingRefiner = checkingRefiner.Substitute(counting);
        var countedEliteSelector = eliteSelector.Substitute(counting);
        var countedGenderSelector = genderSelector.Substitute(counting);
        Action<ResolutionScope, bool> invoke = kind switch
        {
            nameof(TransformedCreator<int>) => (scope, counted) => scope.For<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(problem)
                .Resolve(counted ? countedTransformedCreator : transformedCreator).Create(1, random).ShouldBe([11]),
            nameof(TransformedCrossover<int>) => (scope, counted) => scope.For<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(problem)
                .Resolve(counted ? countedTransformedCrossover : transformedCrossover).Cross([new Parents<int>(10, 20)], random).ShouldBe([11]),
            nameof(RefinementEvaluator<int>) => (scope, counted) => scope.For<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(problem)
                .Resolve(counted ? countedRefiningEvaluator : refiningEvaluator).Evaluate([10], random).ShouldBe([new ObjectiveVector(9)]),
            nameof(ImprovementCheckingRefiner<int>) => (scope, counted) => scope.For<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(problem)
                .Resolve(counted ? countedCheckingRefiner : checkingRefiner).Refine([10], random).ShouldBe([9]),
            nameof(EliteSelector<int>) => (scope, counted) => scope.For<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(problem)
                .Resolve(counted ? countedEliteSelector : eliteSelector).Select(population, 2, random)
                .Select(candidate => candidate.Candidate).ShouldBe([10, 10]),
            nameof(GenderSpecificSelector<int>) => (scope, counted) => scope.For<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(problem)
                .Resolve(counted ? countedGenderSelector : genderSelector).Select(population, 3, random)
                .Select(candidate => candidate.Candidate).ShouldBe([10, 20, 20]),
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
        long[] expectedInvocations = kind switch
        {
            nameof(TransformedCreator<int>) => [1, 0, 1, 0, 0, 0, 0],
            nameof(TransformedCrossover<int>) => [0, 1, 1, 0, 0, 0, 0],
            nameof(RefinementEvaluator<int>) => [0, 0, 0, 1, 1, 0, 0],
            nameof(ImprovementCheckingRefiner<int>) => [0, 0, 0, 1, 2, 0, 0],
            nameof(EliteSelector<int>) => [0, 0, 0, 0, 0, 1, 0],
            nameof(GenderSpecificSelector<int>) => [0, 0, 0, 0, 0, 1, 1],
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
        var parent = ResolutionScope.Create();
        invoke(parent, false);
        var child = parent.CreateChildScope();

        invoke(child, true);
        counters.Select(counter => parent.Results.Result(counter).CurrentCount).ShouldBe(expectedInvocations);
        invoke(parent, false);
        counters.Select(counter => parent.Results.Result(counter).CurrentCount).ShouldBe(expectedInvocations);
        invoke(child, true);
        counters.Select(counter => parent.Results.Result(counter).CurrentCount).ShouldBe(expectedInvocations.Select(count => 2 * count));
        invoke(ResolutionScope.Create(), false);
        counters.Select(counter => parent.Results.Result(counter).CurrentCount).ShouldBe(expectedInvocations.Select(count => 2 * count));
    }

    // Counts the target wherever a configuration holds it, and leaves a configuration that does not hold it unchanged.
    private static Substitution CountingIfHeld<TOperation>(IOperator<TOperation, int> target, Counter counter)
        where TOperation : IOperation<TOperation> =>
        Substitution.Of<IOperator<TOperation, int>>(node => ReferenceEquals(node, target), node => node.Counted(counter));

    private sealed record ConstantCreator : SingleCandidateCreator<int>
    {
        public override int CreateCandidate(IRandomNumberGenerator random) => 10;
    }

    private sealed record AddOneMutator : SingleCandidateMutator<int>
    {
        public override int MutateCandidate(int parent, IRandomNumberGenerator random) => parent + 1;
    }

    private sealed record SubtractOneRefiner : StatelessRefiner<int>
    {
        public override IReadOnlyList<int> Refine(IReadOnlyList<int> candidates, IRandomNumberGenerator random) =>
            candidates.Select(candidate => candidate - 1).ToArray();
    }

    private sealed record RangeSelector(int Offset) : StatelessSelector<int>
    {
        public override IReadOnlyList<EvaluatedCandidate<int>> Select(IReadOnlyList<EvaluatedCandidate<int>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random) =>
            population.Skip(Offset).Take(count).ToArray();
    }
}
