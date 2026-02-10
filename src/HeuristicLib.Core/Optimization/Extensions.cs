namespace HEAL.HeuristicLib.Optimization;

public static class Extensions
{

  // ToDo: pack the extensions to the actual classes they belong to, not in a global extensions class.
  extension<TGenotype>(IReadOnlyList<ISolution<TGenotype>> parents)
  {
    public IParents<TGenotype>[] ToGenotypePairs()
    {
      var offspringCount = parents.Count / 2;
      var parentPairs = new IParents<TGenotype>[offspringCount];
      for (int i = 0, j = 0; i < offspringCount; i++, j += 2) {
        parentPairs[i] = new Parents<TGenotype>(parents[j].Genotype, parents[j + 1].Genotype);
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
