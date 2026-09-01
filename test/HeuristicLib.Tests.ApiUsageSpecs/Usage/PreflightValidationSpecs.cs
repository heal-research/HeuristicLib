using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Encodings.BoolVectors;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems.TravelingSalesman;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Tests.ApiUsageSpecs.Usage;

/// <summary>
/// Validating a configuration before a run starts. The walk reaches every operator a run would reach, including
/// operators nested inside composed ones, and reports each incompatibility with the path that leads to it.
/// </summary>
public class PreflightValidationSpecs
{
    private static FixedCardinalityBoolVectorSearchSpace Constrained => new(length: 4, cardinality: 2);

    private static BoolVectorSearchSpace Unconstrained => new(Length: 4);

    /// <summary>
    /// An operator nested inside a composition is found, and the diagnostic says which one and why.
    /// </summary>
    [Fact]
    public void ValidatingAComposedOperator_ReportsTheNestedOperatorThatCannotBeUsed()
    {
        var pipeline = PipelineMutator.Create(new BitSwapMutator(), new FlipOneBitMutator());

        var report = SearchConfigurationValidation.Validate(pipeline, Constrained);

        report.IsValid.ShouldBeFalse();
        var diagnostic = report.Diagnostics.ShouldHaveSingleItem();
        diagnostic.Path.ShouldContain("ChildMutators[1]");
        diagnostic.Message.ShouldContain(nameof(FlipOneBitMutator));
        diagnostic.Message.ShouldContain("Cardinality(2)");
    }

    /// <summary>
    /// The same configuration is valid over the wider space, because there the invariant it fails to preserve is not
    /// required of members.
    /// </summary>
    [Fact]
    public void TheSameConfiguration_IsValidOverTheWiderSpace()
    {
        var pipeline = PipelineMutator.Create(new BitSwapMutator(), new FlipOneBitMutator());

        SearchConfigurationValidation.Validate(pipeline, Unconstrained).IsValid.ShouldBeTrue();
    }

    /// <summary>
    /// Every failure is reported, not only the first, so one validation pass tells a user everything to fix.
    /// </summary>
    [Fact]
    public void ValidationReportsEveryFailure_AndThrowingListsThemAll()
    {
        var pipeline = PipelineMutator.Create(new FlipOneBitMutator(), new SwapSecondTrueMutator());

        var report = SearchConfigurationValidation.Validate(pipeline, Unconstrained);

        var diagnostic = report.Diagnostics.ShouldHaveSingleItem();
        diagnostic.Path.ShouldEndWith("ChildMutators[1]");
        diagnostic.Message.ShouldContain(nameof(SwapSecondTrueMutator));

        var exception = Should.Throw<InvalidOperationException>(report.ThrowIfInvalid);
        exception.Message.ShouldContain("AtLeastSet(2)");
    }

    /// <summary>
    /// A search space that states no invariants checks nothing, so every configuration written before invariants
    /// existed validates unchanged. Adopting invariant checking is a per space decision.
    /// </summary>
    [Fact]
    public void AnAlgorithmOverASpaceWithoutInvariants_ValidatesUnchanged()
    {
        var problem = new TravelingSalesmanProblem();
        var algorithm = GeneticAlgorithm.For(problem, populationSize: 20, maximumGenerations: 5);

        algorithm.Validate(problem).IsValid.ShouldBeTrue();
        Should.NotThrow(() => algorithm.ValidateAndThrow(problem));
    }
}
