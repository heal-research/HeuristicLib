using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Tests.Operators;

public class AdaptationTests
{
    [Fact]
    public void ScaleStartsAtOneAndMultipliesTheBase()
    {
        var scale = new AdaptedScale();
        scale.Factor.ShouldBe(1.0);
        scale.Apply(3.0).ShouldBe(3.0);
        scale.Factor *= 2.0;
        scale.Factor *= -0.5;
        scale.Factor.ShouldBe(-1.0);
        scale.Apply(3.0).ShouldBe(-3.0);
    }

    [Fact]
    public void OffsetStartsAtZeroAndAddsToTheBase()
    {
        var offset = new AdaptedOffset();
        offset.Offset.ShouldBe(0.0);
        offset.Apply(3.0).ShouldBe(3.0);
        offset.Offset += 2.0;
        offset.Offset += -0.5;
        offset.Offset.ShouldBe(1.5);
        offset.Apply(3.0).ShouldBe(4.5);
    }

    [Fact]
    public void MutatorEqualityUsesBaseValuesAndAdaptationReferences()
    {
        var first = new GaussianMutator(0.5, 2.0);
        first.ShouldBe(new GaussianMutator(0.5, 2.0));
        var adaptation = new ScaleAdaptation();
        var linked = first with { MutationStrengthAdaptation = adaptation };
        linked.ShouldBe(first with { MutationStrengthAdaptation = adaptation });
        linked.ShouldNotBe(first);
        linked.ShouldNotBe(first with { MutationStrengthAdaptation = new ScaleAdaptation() });
        adaptation.ShouldNotBe(new ScaleAdaptation());
    }

    [Fact]
    public void AdaptationIsADeclaredChildAndSubstitutionKeepsItsSharedReference()
    {
        var original = new ScaleAdaptation();
        var replacement = new ScaleAdaptation();
        var mutator = new GaussianMutator(0.5, 2.0) { MutationStrengthAdaptation = original };
        var substituted = mutator.Substitute(original, _ => replacement);
        substituted.MutationStrengthAdaptation.ShouldBeSameAs(replacement);
        GaussianMutator.MutationStrengthParameter.Get(mutator).ShouldBe(2.0);
    }
}
