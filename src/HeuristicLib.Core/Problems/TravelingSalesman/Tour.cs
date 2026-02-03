namespace HEAL.HeuristicLib.Problems.TravelingSalesman;

public class Tour(IEnumerable<int> cities) {
  public IReadOnlyList<int> Cities { get; } = cities.ToList();
}
