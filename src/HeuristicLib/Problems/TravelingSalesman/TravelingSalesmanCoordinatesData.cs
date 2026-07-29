namespace HEAL.HeuristicLib.Problems.TravelingSalesman;

public class TravelingSalesmanCoordinatesData : ITravelingSalesmanProblemData
{

    public TravelingSalesmanCoordinatesData(IReadOnlyList<(double X, double Y)> coordinates, DistanceMeasure measure = DistanceMeasure.Euclidean)
    {
        if (coordinates.Count < 1)
            throw new ArgumentException("The coordinates must have at least one city.");

        Coordinates = coordinates.ToImmutableArray();
        DistanceMeasure = measure;
    }

    public TravelingSalesmanCoordinatesData(double[,] coordinates, DistanceMeasure measure = DistanceMeasure.Euclidean)
    {
        if (coordinates.GetLength(1) != 2)
            throw new ArgumentException("The coordinates must have two columns.");
        if (coordinates.GetLength(0) < 1)
            throw new ArgumentException("The coordinates must have at least one city.");

        var data = new (double X, double Y)[coordinates.GetLength(0)];
        for (var i = 0; i < coordinates.GetLength(0); i++)
        {
            data[i] = (coordinates[i, 0], coordinates[i, 1]);
        }

        Coordinates = data.ToImmutableArray();
        DistanceMeasure = measure;
    }
    public ImmutableArray<(double X, double Y)> Coordinates { get; }
    public DistanceMeasure DistanceMeasure { get; }
    public int NumberOfCities => Coordinates.Length;

    public double GetDistance(int fromCity, int toCity)
    {
        var (x1, y1) = Coordinates[fromCity];
        var (x2, y2) = Coordinates[toCity];

        return DistanceHelper.GetDistance(DistanceMeasure, x1, y1, x2, y2);
    }
}
