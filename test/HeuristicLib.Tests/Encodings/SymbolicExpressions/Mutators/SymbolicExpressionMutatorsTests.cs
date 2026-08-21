using DefaultMutators = HEAL.HeuristicLib.Encodings.SymbolicExpressions.SymbolicExpressionMutators;

namespace HEAL.HeuristicLib.Tests.Operators.Mutators.SymbolicExpressionMutators;

public sealed class SymbolicExpressionMutatorsTests
{
    [Fact]
    public void Default_ContainsTheStandardMutatorsInOrder()
    {
        DefaultMutators.Default.Length.ShouldBe(5);
        DefaultMutators.Default[0].ShouldBeOfType<NodeReplacementMutator>();

        var perturbAll = DefaultMutators.Default[1].ShouldBeOfType<LocalPerturbationMutator>();
        perturbAll.TargetSelection.ShouldBeSameAs(LocalPerturbationTargets.All);

        var perturbOne = DefaultMutators.Default[2].ShouldBeOfType<LocalPerturbationMutator>();
        perturbOne.TargetSelection.ShouldBeSameAs(LocalPerturbationTargets.One);

        DefaultMutators.Default[3].ShouldBeOfType<ShrinkSubtreeMutator>();
        DefaultMutators.Default[4].ShouldBeOfType<SubtreeMutator>();
    }
}
