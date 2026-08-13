using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Replacers;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Operators.Replacers;

public class ReplacerConfigurationEqualityTests
{
    [Fact]
    public void WrappingReplacer_ExposesConfiguredChildReplacer()
    {
        var child = new OffsetReplacer(1);

        child.CountReplacerCalls(new ObservationCounter()).ChildReplacer.ShouldBeSameAs(child);
    }

    [Fact]
    public void MultiReplacer_ExposesAndSnapshotsConfiguredChildReplacers()
    {
        var first = new OffsetReplacer(1);
        var second = new OffsetReplacer(2);
        var children = new List<IReplacer<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>> { first, second };
        var replacer = new FirstOfReplacer(children);

        children.Clear();

        replacer.ChildReplacers.ShouldBe([first, second]);
    }

    [Fact]
    public void MultiReplacer_UsesOrderedStructuralEquality()
    {
        var left = ChooseOneReplacer.Create(new OffsetReplacer(1), new OffsetReplacer(2));
        var equal = ChooseOneReplacer.Create(new OffsetReplacer(1), new OffsetReplacer(2));
        var reordered = ChooseOneReplacer.Create(new OffsetReplacer(2), new OffsetReplacer(1));

        left.ShouldBe(equal);
        left.GetHashCode().ShouldBe(equal.GetHashCode());
        left.ShouldNotBe(reordered);
    }

    [Fact]
    public void ChooseOneReplacer_WithDifferentWeights_IsNotEqual()
    {
        IReplacer<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>[] children = [new OffsetReplacer(1), new OffsetReplacer(2)];
        var left = new ChooseOneReplacer<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>(children) { Weights = [1.0, 2.0] };
        var right = left with { Weights = [2.0, 1.0] };

        left.ShouldNotBe(right);
    }

    [Fact]
    public void WrappingConcerns_IncludeChildAndSettingsInEquality()
    {
        var counter = new ObservationCounter();
        var duration = new ObservationDuration();
        var left = new OffsetReplacer(1).CountReplacerCalls(counter);
        var equal = new OffsetReplacer(1).CountReplacerCalls(counter);
        var differentMetric = new OffsetReplacer(1).CountReplacementCandidates(counter);
        var measured = new OffsetReplacer(1).MeasureReplacerDuration(duration, TimeProvider.System);
        var measuredEqual = new OffsetReplacer(1).MeasureReplacerDuration(duration, TimeProvider.System);

        left.ShouldBe(equal);
        left.ShouldNotBe(differentMetric);
        measured.ShouldBe(measuredEqual);
    }

    [Fact]
    public void ObservableReplacer_SnapshotsObserversAndUsesTheirIdentityInEquality()
    {
        var observer = new ActionReplacerObserver<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>((_, _, _, _, _, _) => { });
        var observers = new List<IReplacerObserver<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>> { observer };
        var left = new ObservableReplacer<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>(new OffsetReplacer(1), observers);
        var equal = new ObservableReplacer<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>(new OffsetReplacer(1), observer);

        observers.Clear();

        left.Observers.ShouldBe([observer]);
        left.ShouldBe(equal);
    }

    [Fact]
    public void ParetoCrowdingReplacer_WithReconfiguredSetting_IsNotEqual()
    {
        var left = new ParetoCrowdingReplacer<int>(false);
        var right = left with { DominateOnEqualities = true };

        left.ShouldNotBe(right);
        right.DominateOnEqualities.ShouldBeTrue();
    }

    private sealed record OffsetReplacer(int Offset) : StatelessReplacer<int, DummySearchSpace<int>>
    {
        public override IReadOnlyList<EvaluatedCandidate<int>> Replace(IReadOnlyList<EvaluatedCandidate<int>> previousPopulation, IReadOnlyList<EvaluatedCandidate<int>> offspringPopulation, ObjectiveDirections objective, int count, IRandomNumberGenerator random, DummySearchSpace<int> searchSpace) =>
            previousPopulation.Concat(offspringPopulation).Skip(Offset).Take(count).ToArray();
    }

    private sealed record FirstOfReplacer
        : MultiReplacer<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>
    {
        public FirstOfReplacer(IReadOnlyList<IReplacer<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>> childReplacers)
            : base(childReplacers)
        {
        }

        protected override MultiReplacerInstance<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>> CreateExecutionInstance(ImmutableArray<IReplacerInstance<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>> childReplacers) =>
            new Instance(childReplacers);

        private sealed class Instance(ImmutableArray<IReplacerInstance<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>> childReplacers)
            : MultiReplacerInstance<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>(childReplacers)
        {
            public override IReadOnlyList<EvaluatedCandidate<int>> Replace(IReadOnlyList<EvaluatedCandidate<int>> previousPopulation, IReadOnlyList<EvaluatedCandidate<int>> offspringPopulation, ObjectiveDirections objective, int count, IRandomNumberGenerator random, DummySearchSpace<int> searchSpace, IProblem<int, DummySearchSpace<int>> problem) =>
                ChildReplacers[0].Replace(previousPopulation, offspringPopulation, objective, count, random, searchSpace, problem);
        }
    }
}
