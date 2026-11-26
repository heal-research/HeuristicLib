#region varying-objective-imports
using HEAL.HeuristicLib.Collections;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Random;
#endregion varying-objective-imports

namespace HEAL.HeuristicLib.Tests.Docs.Extending;

#region varying-objective-strategy
// The scalarization is a value strategy of the algorithm: immutable weights, no state.
public sealed record Tchebycheff(ValueArray<double> Weights)
{
    public double Scalarize(ObjectiveVector vector, ObjectiveVector idealPoint)
    {
        return Enumerable.Range(0, Weights.Count).Max(i => Weights[i] * Math.Abs(vector[i] - idealPoint[i]));
    }
}

// The ideal point is executor state: it moves whenever an evaluation improves an objective.
public sealed class IdealPoint(int objectives)
{
    public ObjectiveVector Value { get; private set; } = new(Enumerable.Repeat(double.PositiveInfinity, objectives));

    public void Observe(ObjectiveVector evaluated)
    {
        Value = new(Value.Zip(evaluated, Math.Min));
    }
}
#endregion varying-objective-strategy

public sealed class VaryingObjectiveDocumentationSpecs
{
    [Fact]
    public void ASelectionHelperRanksUnderTheObjectiveItIsGiven()
    {
        #region varying-objective-inputs
        // Two weights, a two-objective ideal point, and a neighbourhood of two evaluated candidates.
        var scalarization = new Tchebycheff([1.0, 1.0]);
        var ideal = new IdealPoint(2);
        var random = RandomNumberGenerator.Create(seed: 42);
        EvaluatedCandidate<RealVector> a = EvaluatedCandidate.From(new RealVector(0.0), new ObjectiveVector(2.0, 6.0));
        EvaluatedCandidate<RealVector> b = EvaluatedCandidate.From(new RealVector(1.0), new ObjectiveVector(5.0, 1.0));
        IReadOnlyList<EvaluatedCandidate<RealVector>> neighbourhood = [a, b];
        var directions = MultiObjective.Minimize(2).Directions;
        #endregion varying-objective-inputs

        #region varying-objective-usage
        // Rank under the scalarization of the current ideal point, and pass that objective to the static helper.
        ObjectiveDirections Current() => new(directions,
            Comparer<ObjectiveVector>.Create((x, y) =>
                scalarization.Scalarize(x, ideal.Value).CompareTo(scalarization.Scalarize(y, ideal.Value))));

        ideal.Observe(a.ObjectiveVector);
        ideal.Observe(b.ObjectiveVector);
        var beforeTheIdealPointMoves = TournamentSelector.Select(neighbourhood, Current(), count: 1, random, tournamentSize: 32);

        ideal.Observe(new ObjectiveVector(-2.0, 3.0));
        var afterTheIdealPointMoves = TournamentSelector.Select(neighbourhood, Current(), count: 1, random, tournamentSize: 32);
        #endregion varying-objective-usage

        ideal.Value.ShouldBe(new ObjectiveVector(-2.0, 1.0));
        beforeTheIdealPointMoves.ShouldBe([b]);
        afterTheIdealPointMoves.ShouldBe([a]);
    }
}
