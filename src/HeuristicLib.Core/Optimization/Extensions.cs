namespace HEAL.HeuristicLib.Optimization;

public static class Extensions
{
  // ToDo: pack the extensions to the actual classes they belong to, not in a global extensions class.
  extension<TGenotype>(IReadOnlyList<TGenotype> parents)
  {
    public IReadOnlyList<IParents<TGenotype>> ToParentPairs()
    {
      var offspringCount = parents.Count / 2;
      var parentPairs = new IParents<TGenotype>[offspringCount];
      for (int i = 0, j = 0; i < offspringCount; i++, j += 2) {
        var p1 = parents[j];
        var p2 = parents[j + 1];
        parentPairs[i] = new Parents<TGenotype>(p1, p2);
      }

      return parentPairs;
    }
  }

  extension<TGenotype>(IReadOnlyList<ISolution<TGenotype>> parents)
  {
    public IParents<TGenotype>[] ToParents(Objective? objective = null)
    {
      var offspringCount = parents.Count / 2;
      var parentPairs = new IParents<TGenotype>[offspringCount];
      for (int i = 0, j = 0; i < offspringCount; i++, j += 2) {
        var p1 = parents[j];
        var p2 = parents[j + 1];
        if (objective is not null
            && objective.TotalOrderComparer is not NoTotalOrderComparer
            && objective.TotalOrderComparer.Compare(p1.ObjectiveVector, p2.ObjectiveVector) > 0)
          (p1, p2) = (p2, p1);
        parentPairs[i] = new Parents<TGenotype>(p1.Genotype, p2.Genotype);
      }

      return parentPairs;
    }

    public (ISolution<TGenotype>, ISolution<TGenotype>)[] ToSolutionPairs()
    {
      var offspringCount = parents.Count / 2;
      var parentPairs = new (ISolution<TGenotype>, ISolution<TGenotype>)[offspringCount];
      for (int i = 0, j = 0; i < offspringCount; i++, j += 2) {
        parentPairs[i] = (parents[j], parents[j + 1]);
      }

      return parentPairs;
    }
  }

  public static bool IsAlmost(this double a, double b, double tolerance = 1E-10) => Math.Abs(a - b) <= tolerance;

  // Convenience overload using default comparer for T2

  public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, TValue defaultValue)
  {
    if (dict.TryGetValue(key, out var v)) {
      return v;
    }

    dict[key] = defaultValue;

    return defaultValue;
  }
}
