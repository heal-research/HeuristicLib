namespace HEAL.HeuristicLib.Problems.TravelingSalesman;

public interface ITravelingSalesmanProblemData {
  int NumberOfCities { get; }
  double GetDistance(int fromCity, int toCity);
}
