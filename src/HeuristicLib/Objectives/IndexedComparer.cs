namespace HEAL.HeuristicLib.Objectives;

/// <summary>
/// Orders population indices by one objective value. <see cref="double.NaN"/> sorts last.
/// </summary>
public class IndexedComparer : IComparer<int>
{
    private readonly IReadOnlyList<ObjectiveVector> population;
    private readonly int dimension;

    public IndexedComparer(IReadOnlyList<ObjectiveVector> population, int dimension)
    {
        this.population = population;
        this.dimension = dimension;
    }

    // Ascending by value with NaN last, which is the ordering ObjectiveValue.Compare applies when minimizing.
    public int Compare(int x, int y) =>
        ObjectiveValue.Compare(population[x][dimension], population[y][dimension], ObjectiveDirection.Minimize);
}
