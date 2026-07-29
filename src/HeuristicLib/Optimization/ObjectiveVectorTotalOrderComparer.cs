namespace HEAL.HeuristicLib.Optimization;

public static class ObjectiveVectorTotalOrderComparer
{
    public static SingleObjectiveComparer CreateSingleObjectiveComparer(ObjectiveDirection objectiveDirection) => new(objectiveDirection);

    public static WeightedSumComparer CreateWeightedSumComparer(IReadOnlyList<ObjectiveDirection> objectives, IReadOnlyList<double>? weights = null) => new(objectives, weights);

    public static LexicographicComparer CreateLexicographicComparer(IReadOnlyList<ObjectiveDirection> objectives, IReadOnlyList<int>? order = null) => new(objectives, order);

    public static NoTotalOrderComparer CreateNoTotalOrderComparer(IReadOnlyList<ObjectiveDirection> objectives) => NoTotalOrderComparer.Instance;
}
