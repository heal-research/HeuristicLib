namespace HEAL.HeuristicLib.Tests.Operators.Refiners;

public class ImprovementCriterionTests
{
    [Fact]
    public void StrictlyBetter_OnAMinimizedObjective_RequiresALowerValue()
    {
        var criterion = ImprovementChecking.StrictlyBetter;

        criterion.IsImprovement(Vector(1.0), Vector(2.0), SingleObjective.Minimize).ShouldBeTrue();
        criterion.IsImprovement(Vector(2.0), Vector(2.0), SingleObjective.Minimize).ShouldBeFalse();
        criterion.IsImprovement(Vector(3.0), Vector(2.0), SingleObjective.Minimize).ShouldBeFalse();
    }

    [Fact]
    public void StrictlyBetter_OnAMaximizedObjective_RequiresAHigherValue()
    {
        var criterion = ImprovementChecking.StrictlyBetter;

        criterion.IsImprovement(Vector(3.0), Vector(2.0), SingleObjective.Maximize).ShouldBeTrue();
        criterion.IsImprovement(Vector(2.0), Vector(2.0), SingleObjective.Maximize).ShouldBeFalse();
        criterion.IsImprovement(Vector(1.0), Vector(2.0), SingleObjective.Maximize).ShouldBeFalse();
    }

    [Fact]
    public void NotWorse_AcceptsAnEquallyGoodObjectiveVector()
    {
        var criterion = ImprovementChecking.NotWorse;

        criterion.IsImprovement(Vector(2.0), Vector(2.0), SingleObjective.Minimize).ShouldBeTrue();
        criterion.IsImprovement(Vector(3.0), Vector(2.0), SingleObjective.Minimize).ShouldBeFalse();
    }

    [Fact]
    public void Dominance_RequiresNoObjectiveToGetWorseAndOneToImprove()
    {
        var criterion = ImprovementChecking.Dominance;
        var directions = MultiObjective.Minimize(2);

        criterion.IsImprovement(Vector(1.0, 2.0), Vector(2.0, 2.0), directions).ShouldBeTrue();
        criterion.IsImprovement(Vector(2.0, 2.0), Vector(2.0, 2.0), directions).ShouldBeFalse();
        criterion.IsImprovement(Vector(1.0, 3.0), Vector(2.0, 2.0), directions).ShouldBeFalse();
    }

    [Fact]
    public void MinimumImprovement_OnAMinimizedObjective_MeasuresTheMarginDownwards()
    {
        var criterion = ImprovementChecking.MinimumImprovement(0.5);

        criterion.IsImprovement(Vector(1.4), Vector(2.0), SingleObjective.Minimize).ShouldBeTrue();
        criterion.IsImprovement(Vector(1.5), Vector(2.0), SingleObjective.Minimize).ShouldBeTrue();
        criterion.IsImprovement(Vector(1.6), Vector(2.0), SingleObjective.Minimize).ShouldBeFalse();
    }

    [Fact]
    public void MinimumImprovement_OnAMaximizedObjective_MeasuresTheMarginUpwards()
    {
        var criterion = ImprovementChecking.MinimumImprovement(0.5);

        criterion.IsImprovement(Vector(2.6), Vector(2.0), SingleObjective.Maximize).ShouldBeTrue();
        criterion.IsImprovement(Vector(2.5), Vector(2.0), SingleObjective.Maximize).ShouldBeTrue();
        criterion.IsImprovement(Vector(2.4), Vector(2.0), SingleObjective.Maximize).ShouldBeFalse();
        criterion.IsImprovement(Vector(1.4), Vector(2.0), SingleObjective.Maximize).ShouldBeFalse();
    }

    [Fact]
    public void MinimumImprovement_WithZeroDelta_AcceptsAnythingNotWorse()
    {
        var criterion = ImprovementChecking.MinimumImprovement(0.0);

        criterion.IsImprovement(Vector(2.0), Vector(2.0), SingleObjective.Minimize).ShouldBeTrue();
        criterion.IsImprovement(Vector(2.1), Vector(2.0), SingleObjective.Minimize).ShouldBeFalse();
    }

    [Fact]
    public void MinimumImprovement_WithNegativeDelta_ToleratesABoundedWorsening()
    {
        var criterion = ImprovementChecking.MinimumImprovement(-0.5);

        criterion.IsImprovement(Vector(2.4), Vector(2.0), SingleObjective.Minimize).ShouldBeTrue();
        criterion.IsImprovement(Vector(2.6), Vector(2.0), SingleObjective.Minimize).ShouldBeFalse();
    }

    [Fact]
    public void MinimumImprovement_RequiresEveryObjectiveToClearTheMargin()
    {
        var criterion = ImprovementChecking.MinimumImprovement(0.5);
        var directions = MultiObjective.Minimize(2);

        criterion.IsImprovement(Vector(1.0, 1.0), Vector(2.0, 2.0), directions).ShouldBeTrue();
        criterion.IsImprovement(Vector(1.0, 1.9), Vector(2.0, 2.0), directions).ShouldBeFalse();
    }

    [Fact]
    public void MinimumImprovement_WithNaN_IsNeverAnImprovement()
    {
        var criterion = ImprovementChecking.MinimumImprovement(0.5);

        criterion.IsImprovement(Vector(double.NaN), Vector(2.0), SingleObjective.Minimize).ShouldBeFalse();
        criterion.IsImprovement(Vector(1.0), Vector(double.NaN), SingleObjective.Minimize).ShouldBeFalse();
    }

    [Fact]
    public void MinimumRelativeImprovement_ScalesTheMarginWithTheOriginalMagnitude()
    {
        var criterion = ImprovementChecking.MinimumRelativeImprovement(0.1);

        criterion.IsImprovement(Vector(89.0), Vector(100.0), SingleObjective.Minimize).ShouldBeTrue();
        criterion.IsImprovement(Vector(91.0), Vector(100.0), SingleObjective.Minimize).ShouldBeFalse();
        criterion.IsImprovement(Vector(111.0), Vector(100.0), SingleObjective.Maximize).ShouldBeTrue();
        criterion.IsImprovement(Vector(109.0), Vector(100.0), SingleObjective.Maximize).ShouldBeFalse();
    }

    [Fact]
    public void MinimumRelativeImprovement_WithANegativeOriginalValue_UsesItsMagnitude()
    {
        var criterion = ImprovementChecking.MinimumRelativeImprovement(0.1);

        criterion.IsImprovement(Vector(-111.0), Vector(-100.0), SingleObjective.Minimize).ShouldBeTrue();
        criterion.IsImprovement(Vector(-109.0), Vector(-100.0), SingleObjective.Minimize).ShouldBeFalse();
    }

    [Fact]
    public void MinimumRelativeImprovement_WithAZeroOriginalValue_RequiresOnlyThatItIsNotWorse()
    {
        var criterion = ImprovementChecking.MinimumRelativeImprovement(0.1);

        criterion.IsImprovement(Vector(0.0), Vector(0.0), SingleObjective.Minimize).ShouldBeTrue();
        criterion.IsImprovement(Vector(-1.0), Vector(0.0), SingleObjective.Minimize).ShouldBeTrue();
        criterion.IsImprovement(Vector(1.0), Vector(0.0), SingleObjective.Minimize).ShouldBeFalse();
    }

    [Fact]
    public void Default_OnASingleObjectiveProblem_RequiresAStrictlyBetterObjectiveVector()
    {
        var criterion = ImprovementChecking.Default;

        criterion.IsImprovement(Vector(1.0), Vector(2.0), SingleObjective.Minimize).ShouldBeTrue();
        criterion.IsImprovement(Vector(2.0), Vector(2.0), SingleObjective.Minimize).ShouldBeFalse();
    }

    [Fact]
    public void Default_OnAParetoMultiObjectiveProblem_FallsBackToDominance()
    {
        var criterion = ImprovementChecking.Default;
        var directions = MultiObjective.Minimize(2);

        criterion.IsImprovement(Vector(1.0, 2.0), Vector(2.0, 2.0), directions).ShouldBeTrue();
        criterion.IsImprovement(Vector(1.0, 3.0), Vector(2.0, 2.0), directions).ShouldBeFalse();
    }

    [Fact]
    public void Default_OnAProblemWithAConfiguredTotalOrder_UsesThatOrder()
    {
        var criterion = ImprovementChecking.Default;
        var directions = MultiObjective.WeightedSum([ObjectiveDirection.Minimize, ObjectiveDirection.Minimize], [1.0, 1.0]);

        criterion.IsImprovement(Vector(1.0, 3.0), Vector(2.0, 3.0), directions).ShouldBeTrue();
        ImprovementChecking.Dominance.IsImprovement(Vector(0.0, 3.5), Vector(2.0, 2.0), directions).ShouldBeFalse();
        criterion.IsImprovement(Vector(0.0, 3.5), Vector(2.0, 2.0), directions).ShouldBeTrue();
    }

    [Fact]
    public void StrictlyBetter_WithoutATotalObjectiveOrder_Throws()
    {
        var criterion = ImprovementChecking.StrictlyBetter;
        var directions = MultiObjective.Minimize(2);

        Should.Throw<InvalidOperationException>(() =>
            criterion.IsImprovement(Vector(1.0, 1.0), Vector(2.0, 2.0), directions));
    }

    [Fact]
    public void MinimumImprovement_WithMismatchedObjectiveCounts_Throws()
    {
        var criterion = ImprovementChecking.MinimumImprovement(0.5);

        Should.Throw<ArgumentException>(() =>
            criterion.IsImprovement(Vector(1.0, 1.0), Vector(2.0), MultiObjective.Minimize(2)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EveryCriterion_RejectsANaNRefinement(bool maximize)
    {
        var directions = maximize ? SingleObjective.Maximize : SingleObjective.Minimize;
        var nan = Vector(double.NaN);
        var finite = Vector(2.0);

        ImprovementChecking.StrictlyBetter.IsImprovement(nan, finite, directions).ShouldBeFalse();
        ImprovementChecking.NotWorse.IsImprovement(nan, finite, directions).ShouldBeFalse();
        ImprovementChecking.Dominance.IsImprovement(nan, finite, directions).ShouldBeFalse();
        ImprovementChecking.Default.IsImprovement(nan, finite, directions).ShouldBeFalse();
        ImprovementChecking.MinimumImprovement(0.0).IsImprovement(nan, finite, directions).ShouldBeFalse();
        ImprovementChecking.MinimumRelativeImprovement(0.0).IsImprovement(nan, finite, directions).ShouldBeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OrderBasedCriteria_AcceptARefinementAwayFromNaN(bool maximize)
    {
        var directions = maximize ? SingleObjective.Maximize : SingleObjective.Minimize;
        var finite = Vector(2.0);
        var nan = Vector(double.NaN);

        ImprovementChecking.StrictlyBetter.IsImprovement(finite, nan, directions).ShouldBeTrue();
        ImprovementChecking.NotWorse.IsImprovement(finite, nan, directions).ShouldBeTrue();
        ImprovementChecking.Dominance.IsImprovement(finite, nan, directions).ShouldBeTrue();
        ImprovementChecking.Default.IsImprovement(finite, nan, directions).ShouldBeTrue();
    }

    [Fact]
    public void OrderBasedCriteria_TreatTwoNaNValuesAsEqual()
    {
        ImprovementChecking.StrictlyBetter.IsImprovement(Vector(double.NaN), Vector(double.NaN), SingleObjective.Minimize).ShouldBeFalse();
        ImprovementChecking.NotWorse.IsImprovement(Vector(double.NaN), Vector(double.NaN), SingleObjective.Minimize).ShouldBeTrue();
    }

    [Theory]
    [InlineData(1.0, 2.0)]
    [InlineData(2.0, 2.0)]
    [InlineData(3.0, 2.0)]
    public void OnASingleObjectiveProblem_DominanceAndStrictOrderAgree(double refined, double original)
    {
        foreach (var directions in new[] { SingleObjective.Minimize, SingleObjective.Maximize })
        {
            ImprovementChecking.Dominance.IsImprovement(Vector(refined), Vector(original), directions)
                .ShouldBe(ImprovementChecking.StrictlyBetter.IsImprovement(Vector(refined), Vector(original), directions));
        }
    }

    [Fact]
    public void OnAnIncomparablePair_DominanceRejectsWhileATotalOrderStillDecides()
    {
        var directions = MultiObjective.WeightedSum([ObjectiveDirection.Minimize, ObjectiveDirection.Minimize], [1.0, 1.0]);
        var refined = Vector(1.0, 5.0);
        var original = Vector(5.0, 1.0);

        ImprovementChecking.Dominance.IsImprovement(refined, original, directions).ShouldBeFalse();
        ImprovementChecking.StrictlyBetter.IsImprovement(refined, original, directions).ShouldBeFalse();
        ImprovementChecking.NotWorse.IsImprovement(refined, original, directions).ShouldBeTrue();
    }

    [Fact]
    public void OnALexicographicOrder_ATradeOffIsAcceptedThatDominanceRejects()
    {
        var directions = MultiObjective.Lexicographic([ObjectiveDirection.Minimize, ObjectiveDirection.Minimize], [0, 1]);
        var refined = Vector(1.0, 100.0);
        var original = Vector(2.0, 0.0);

        ImprovementChecking.StrictlyBetter.IsImprovement(refined, original, directions).ShouldBeTrue();
        ImprovementChecking.Dominance.IsImprovement(refined, original, directions).ShouldBeFalse();
    }

    [Fact]
    public void WithANegativeWeight_DominanceAcceptsWhatTheTotalOrderRejects()
    {
        var directions = MultiObjective.WeightedSum([ObjectiveDirection.Minimize, ObjectiveDirection.Minimize], [1.0, -1.0]);
        var refined = Vector(1.0, 1.0);
        var original = Vector(2.0, 2.0);

        ImprovementChecking.Dominance.IsImprovement(refined, original, directions).ShouldBeTrue();
        ImprovementChecking.StrictlyBetter.IsImprovement(refined, original, directions).ShouldBeFalse();
    }

    [Fact]
    public void Criteria_WithEqualSettings_AreStructurallyEqual()
    {
        ImprovementChecking.MinimumImprovement(0.5).ShouldBe(ImprovementChecking.MinimumImprovement(0.5));
        ImprovementChecking.MinimumImprovement(0.5).ShouldNotBe(ImprovementChecking.MinimumImprovement(0.25));
        ImprovementChecking.MinimumRelativeImprovement(0.1).ShouldBe(ImprovementChecking.MinimumRelativeImprovement(0.1));
        ImprovementChecking.StrictlyBetter.ShouldBe(new StrictlyBetterCriterion());
        ImprovementChecking.Dominance.ShouldBe(new DominanceCriterion());
    }

    private static ObjectiveVector Vector(params double[] values) => new(values);
}
