#region improvement-criterion-imports
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
#endregion improvement-criterion-imports

namespace HEAL.HeuristicLib.Tests.Docs.Extending;

#region improvement-criterion-type
public sealed record FirstObjectiveImprovement(double Margin) : IImprovementCriterion
{
    public bool IsImprovement(
        ObjectiveVector refined,
        ObjectiveVector original,
        ObjectiveDirections objectiveDirections) =>
        objectiveDirections.Directions[0] == ObjectiveDirection.Minimize
            ? original[0] - refined[0] >= Margin
            : refined[0] - original[0] >= Margin;
}
#endregion improvement-criterion-type

public sealed class ImprovementCriterionDocumentationSpecs
{
    [Fact]
    public void ACustomCriterionAppliesItsMarginInTheObjectiveDirection()
    {
        IOperator<Refinement, RealVector> parameterFitting = new NoChangeRefiner<RealVector>();

        #region improvement-criterion-usage
        var criterion = new FirstObjectiveImprovement(Margin: 0.01);
        var checkedFitting = parameterFitting.CheckedForImprovement(criterion);

        // Only 0.98 improves on 1.0 by at least the required margin.
        var original = new ObjectiveVector(1.0);
        var kept = criterion.IsImprovement(
            new ObjectiveVector(0.98), original, SingleObjective.Minimize);
        var rejected = criterion.IsImprovement(
            new ObjectiveVector(0.995), original, SingleObjective.Minimize);
        #endregion improvement-criterion-usage

        checkedFitting.Criterion.ShouldBeSameAs(criterion);
        kept.ShouldBeTrue();
        rejected.ShouldBeFalse();
        criterion.IsImprovement(
            new ObjectiveVector(1.02), original, SingleObjective.Maximize).ShouldBeTrue();
    }
}
