namespace HEAL.HeuristicLib.Optimization;

public readonly struct IndexedComparer(IReadOnlyList<ObjectiveVector> population, int dimension) : IComparer<int> {
  public int Compare(int x, int y) => population[x][dimension].CompareTo(population[y][dimension]);
}
