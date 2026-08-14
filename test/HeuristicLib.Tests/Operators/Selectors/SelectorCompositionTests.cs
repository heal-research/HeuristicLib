using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Operators.Selectors;

public class SelectorCompositionTests
{
    [Fact]
    public void NoSameMatesSelector_AcceptsDifferentMatesWithoutRetrying()
    {
        var counter = new ObservationCounter();
        var problem = CreateProblem();
        var selector = BestSelector.For(problem).CountSelectorCalls(counter).AvoidSameMates(maximumAttempts: 3);
        var instance = new ExecutionInstanceRegistry().Resolve(selector);
        var population = new[]
        {
            EvaluatedCandidate.From(1, new ObjectiveVector(1.0)),
            EvaluatedCandidate.From(2, new ObjectiveVector(2.0))
        };

        var selected = instance.Select(population, problem.Objective, 2, RandomNumberGenerator.Create(1), problem.SearchSpace, problem);

        selected.ShouldBe(population);
        counter.CurrentCount.ShouldBe(1);
    }

    [Fact]
    public void GenderSpecificSelector_AlignsFemaleAndMaleSelectionsAsPairs()
    {
        var femaleSelector = new RangeSelector(0);
        var maleSelector = new RangeSelector(2);
        var selector = GenderSpecificSelector.Create(femaleSelector, maleSelector);
        var instance = new ExecutionInstanceRegistry().Resolve(selector);
        var problem = CreateProblem();
        var population = CreatePopulation(1, 2, 3, 4);

        var selected = instance.Select(population, problem.Objective, 4, RandomNumberGenerator.Create(1), problem.SearchSpace, problem);

        selector.FemaleSelector.ShouldBeSameAs(femaleSelector);
        selector.MaleSelector.ShouldBeSameAs(maleSelector);
        selected.Select(candidate => candidate.Candidate).ShouldBe([1, 3, 2, 4]);
    }

    [Fact]
    public void GenderSpecificSelector_UsesFemaleSelectorForUnpairedFinalCandidate()
    {
        var selector = new RangeSelector(0).PairWith(new RangeSelector(2));
        var instance = new ExecutionInstanceRegistry().Resolve(selector);
        var problem = CreateProblem();

        var selected = instance.Select(CreatePopulation(1, 2, 3, 4), problem.Objective, 3, RandomNumberGenerator.Create(1), problem.SearchSpace, problem);

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
