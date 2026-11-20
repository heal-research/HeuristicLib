using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Operators.Selectors;

public class SelectorCompositionTests
{
    [Fact]
    public void NoSameMatesSelector_AcceptsDifferentMatesWithoutRetrying()
    {
        var problem = CreateProblem();
        var observed = BestSelector.For(problem);
        var selector = observed.AvoidSameMates(maximumAttempts: 3);
        var scope = ResolutionScope.Create();
        var executor = scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(selector.Counting(observed, out var counted), problem);
        var counter = scope.Results.Result(counted);
        var population = new[]
        {
            EvaluatedCandidate.From(1, new ObjectiveVector(1.0)),
            EvaluatedCandidate.From(2, new ObjectiveVector(2.0))
        };

        var selected = executor.Select(population, 2, RandomNumberGenerator.Create(1));

        selected.ShouldBe(population);
        counter.CurrentCount.ShouldBe(1);
    }

    [Fact]
    public void GenderSpecificSelector_AlignsFemaleAndMaleSelectionsAsPairs()
    {
        var femaleSelector = new RangeSelector(0);
        var maleSelector = new RangeSelector(2);
        var selector = GenderSpecificSelector.Create(femaleSelector, maleSelector);
        var problem = CreateProblem();
        var executor = ResolutionScope.Create().Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(selector, problem);
        var population = CreatePopulation(1, 2, 3, 4);

        var selected = executor.Select(population, 4, RandomNumberGenerator.Create(1));

        selector.FemaleSelector.ShouldBeSameAs(femaleSelector);
        selector.MaleSelector.ShouldBeSameAs(maleSelector);
        selected.Select(candidate => candidate.Candidate).ShouldBe([1, 3, 2, 4]);
    }

    [Fact]
    public void GenderSpecificSelector_UsesFemaleSelectorForUnpairedFinalCandidate()
    {
        var selector = new RangeSelector(0).PairWith(new RangeSelector(2));
        var problem = CreateProblem();
        var executor = ResolutionScope.Create().Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(selector, problem);

        var selected = executor.Select(CreatePopulation(1, 2, 3, 4), 3, RandomNumberGenerator.Create(1));

        selected.Select(candidate => candidate.Candidate).ShouldBe([1, 3, 2]);
    }

    private static FuncProblem<int, DummySearchSpace<int>> CreateProblem() =>
        FuncProblem.Create(static (int candidate) => candidate, DummySearchSpace<int>.Instance, SingleObjective.Minimize);

    private static IReadOnlyList<EvaluatedCandidate<int>> CreatePopulation(params int[] candidates) =>
        candidates.Select(candidate => EvaluatedCandidate.From(candidate, new ObjectiveVector(candidate))).ToArray();

    private sealed record RangeSelector(int Offset) : StatelessSelector<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>
    {
        public override IReadOnlyList<EvaluatedCandidate<int>> Select(IReadOnlyList<EvaluatedCandidate<int>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random, DummySearchSpace<int> searchSpace, FuncProblem<int, DummySearchSpace<int>> problem) =>
            population.Skip(Offset).Take(count).ToArray();
    }
}
