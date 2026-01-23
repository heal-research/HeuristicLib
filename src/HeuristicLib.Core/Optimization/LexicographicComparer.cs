using HEAL.HeuristicLib.Genotypes.Vectors;

namespace HEAL.HeuristicLib.Optimization;

public class LexicographicComparer(ObjectiveDirection[] objectives, int[]? order = null) : IComparer<ObjectiveVector>
{
  private readonly Permutation order = order ?? Permutation.Range(objectives.Length);

  public int Compare(ObjectiveVector? x, ObjectiveVector? y)
  {
    if ((x is not null && x.Count != objectives.Length) || (y is not null && y.Count != objectives.Length)) {
      throw new ArgumentException("Fitness must have the same length as the objective");
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

    foreach (var dimension in order) {
      var comparison = x[dimension].CompareTo(y[dimension]);
      if (comparison != 0) {
        return objectives[dimension] switch {
          ObjectiveDirection.Minimize => +comparison,
          ObjectiveDirection.Maximize => -comparison,
          _ => throw new NotImplementedException()
        };
      }
    }

    return 0;
  }
}
