namespace HEAL.HeuristicLib.Optimization;

public class IndexedComparer : IComparer<int>
{
  private readonly IReadOnlyList<ObjectiveVector> population;
  private readonly int dimension;

  public IndexedComparer(IReadOnlyList<ObjectiveVector> population, int dimension)
  {
    this.population = population;
    this.dimension = dimension;
  }

  public int Compare(int x, int y)
  {
    return population[x][dimension].CompareTo(population[y][dimension]);
  }
}
