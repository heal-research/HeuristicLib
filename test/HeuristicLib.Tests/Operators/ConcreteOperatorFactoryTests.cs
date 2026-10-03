using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Operators.Refiners;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Operators;

public sealed class ConcreteOperatorFactoryTests
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
        ICreator<int> creator = new ConstantCreator();
        ICrossover<int> crossover = SelectFirstParentCrossover<int>.Instance;
        IMutator<int> mutator = new AddOneMutator();
        IRefiner<int> refiner = new SubtractOneRefiner();
        IEvaluator<int> evaluator = new ProblemEvaluator<int>();
        ISelector<int> female = new RangeSelector(0);
        ISelector<int> male = new RangeSelector(1);
        var transformedCreator = new TransformedCreator<int>(creator, mutator);
        var transformedCrossover = new TransformedCrossover<int>(crossover, mutator);
        var refiningEvaluator = new RefinementEvaluator<int>(refiner) { Evaluator = evaluator };
        var checkingRefiner = new ImprovementCheckingRefiner<int>(refiner) { Evaluator = evaluator };
        var eliteSelector = new EliteSelector<int>(female);
        var genderSelector = new GenderSpecificSelector<int>(female, male);
        var problem = FuncProblem.Create(static (int candidate) => candidate, DummySearchSpace<int>.Instance, SingleObjective.Minimize);
        var random = RandomNumberGenerator.Create(42);
        var population = new[] { 10, 20, 30 }.Select(candidate => EvaluatedCandidate.From(candidate, new ObjectiveVector(candidate))).ToArray();
        Action<ResolutionScope> invoke = kind switch
        {
            nameof(TransformedCreator<int>) => scope => scope.For<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>()
                .Resolve(transformedCreator).Create(1, random, problem.SearchSpace, problem).ShouldBe([11]),
            nameof(TransformedCrossover<int>) => scope => scope.For<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>()
                .Resolve(transformedCrossover).Cross([new Parents<int>(10, 20)], random, problem.SearchSpace, problem).ShouldBe([11]),
            nameof(RefinementEvaluator<int>) => scope => scope.For<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>()
                .Resolve(refiningEvaluator).Evaluate([10], random, problem.SearchSpace, problem).ShouldBe([new ObjectiveVector(9)]),
            nameof(ImprovementCheckingRefiner<int>) => scope => scope.For<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>()
                .Resolve(checkingRefiner).Refine([10], random, problem.SearchSpace, problem).ShouldBe([9]),
            nameof(EliteSelector<int>) => scope => scope.For<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>()
                .Resolve(eliteSelector).Select(population, problem.Objective, 2, random, problem.SearchSpace, problem)
                .Select(candidate => candidate.Candidate).ShouldBe([10, 10]),
            nameof(GenderSpecificSelector<int>) => scope => scope.For<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>()
                .Resolve(genderSelector).Select(population, problem.Objective, 3, random, problem.SearchSpace, problem)
                .Select(candidate => candidate.Candidate).ShouldBe([10, 20, 20]),
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
        int[] expectedCalls = kind switch
        {
            nameof(TransformedCreator<int>) => [1, 0, 1, 0, 0, 0, 0],
            nameof(TransformedCrossover<int>) => [0, 1, 1, 0, 0, 0, 0],
            nameof(RefinementEvaluator<int>) => [0, 0, 0, 1, 1, 0, 0],
            nameof(ImprovementCheckingRefiner<int>) => [0, 0, 0, 1, 2, 0, 0],
            nameof(EliteSelector<int>) => [0, 0, 0, 0, 0, 1, 0],
            nameof(GenderSpecificSelector<int>) => [0, 0, 0, 0, 0, 1, 1],
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
        var counters = Enumerable.Range(0, expectedCalls.Length).Select(_ => new CountAccumulator()).ToArray();
        var parent = ResolutionScope.Create();
        invoke(parent);
        var child = parent.CreateChildScope(builder =>
        {
            builder.Wrap(creator, original => original.CountCalls(counters[0]));
            builder.Wrap(crossover, original => original.CountCalls(counters[1]));
            builder.Wrap(mutator, original => original.CountCalls(counters[2]));
            builder.Wrap(refiner, original => original.CountCalls(counters[3]));
            builder.Wrap(evaluator, original => original.CountCalls(counters[4]));
            builder.Wrap(female, original => original.CountCalls(counters[5]));
            builder.Wrap(male, original => original.CountCalls(counters[6]));
        });

        invoke(child);
        counters.Select(counter => counter.CurrentCount).ShouldBe(expectedCalls);
        invoke(parent);
        counters.Select(counter => counter.CurrentCount).ShouldBe(expectedCalls);
        invoke(child);
        counters.Select(counter => counter.CurrentCount).ShouldBe(expectedCalls.Select(count => 2 * count));
        invoke(ResolutionScope.Create());
        counters.Select(counter => counter.CurrentCount).ShouldBe(expectedCalls.Select(count => 2 * count));
    }

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
