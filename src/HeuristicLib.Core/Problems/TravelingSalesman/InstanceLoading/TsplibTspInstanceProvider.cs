namespace HEAL.HeuristicLib.Problems.TravelingSalesman.InstanceLoading;

public static class TsplibTspInstanceProvider
{
  public const string FileExtension = "tsp";

  public static TspData LoadData(string tspFile, string? tourFile = null, double? bestQuality = null)
  {
    var parser = new TsplibParser(tspFile);
    parser.Parse();
    if (parser.FixedEdges != null) {
      throw new InvalidDataException("TSP instance " + parser.Name + " contains fixed edges which are not supported by HeuristicLab.");
    }

    var data = new TspData {
      Dimension = parser.Dimension,
      Coordinates = parser.Vertices ?? parser.DisplayVertices,
      Distances = parser.Distances,
      DistanceMeasure = parser.EdgeWeightType switch {
        TSPLIBEdgeWeightTypes.Att => DistanceMeasure.Att,
        TSPLIBEdgeWeightTypes.CEIL_2D => DistanceMeasure.UpperEuclidean,
        TSPLIBEdgeWeightTypes.EUC_2D => DistanceMeasure.RoundedEuclidean,
        TSPLIBEdgeWeightTypes.EUC_3D => throw new InvalidDataException("3D coordinates are not supported."),
        TSPLIBEdgeWeightTypes.Explicit => DistanceMeasure.Direct,
        TSPLIBEdgeWeightTypes.Geo => DistanceMeasure.Geo,
        TSPLIBEdgeWeightTypes.MAN_2D => DistanceMeasure.Manhattan,
        TSPLIBEdgeWeightTypes.MAN_3D => throw new InvalidDataException("3D coordinates are not supported."),
        TSPLIBEdgeWeightTypes.MAX_2D => DistanceMeasure.Maximum,
        TSPLIBEdgeWeightTypes.MAX_3D => throw new InvalidDataException("3D coordinates are not supported."),
        TSPLIBEdgeWeightTypes.Unknown => throw new InvalidDataException("The given edge weight is not supported by HeuristicLab."),
        TSPLIBEdgeWeightTypes.Xray1 => throw new InvalidDataException("The given edge weight is not supported by HeuristicLab."),
        TSPLIBEdgeWeightTypes.Xray2 => throw new InvalidDataException("The given edge weight is not supported by HeuristicLab."),
        TSPLIBEdgeWeightTypes.Special => throw new InvalidDataException("The given edge weight is not supported by HeuristicLab."),
        _ => throw new InvalidDataException("The given edge weight is not supported by HeuristicLab.")
      },
      Name = parser.Name
    };
    if (!string.IsNullOrEmpty(tourFile)) {
      var tourParser = new TsplibParser(tourFile);
      tourParser.Parse();
      data.BestKnownTour = tourParser.Tour.FirstOrDefault();
    }

    if (bestQuality.HasValue) {
      data.BestKnownQuality = bestQuality.Value;
    }

    return data;
  }
}
