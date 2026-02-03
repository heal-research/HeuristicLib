namespace HEAL.HeuristicLib.Optimization;

public class SingleObjectiveComparer(ObjectiveDirection objectiveDirection) : IComparer<ObjectiveVector>
{
  public int Compare(ObjectiveVector? x, ObjectiveVector? y)
  {
    if (x is not null && !x.IsSingleObjective || y is not null && !y.IsSingleObjective) {
      throw new ArgumentException("Fitness must be single-objective");
    }

    if (x is null && y is null) {
      return 0;
    }

    if (x is null) {
      return -1;
    }

    if (y is null) {
      return +1;
    }

    if (ReferenceEquals(x, y)) {
      return 0;
    }

    return objectiveDirection switch {
      ObjectiveDirection.Minimize => x[0].CompareTo(y[0]),
      ObjectiveDirection.Maximize => y[0].CompareTo(x[0]),
      _ => throw new NotImplementedException()
    };
  }
}
