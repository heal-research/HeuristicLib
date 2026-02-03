using System.Diagnostics.CodeAnalysis;

namespace HEAL.HeuristicLib.Problems.TravelingSalesman.InstanceLoading;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public enum TSPLIBEdgeWeightTypes
{
  Unknown = 0,
  Explicit = 1,
  EUC_2D = 2,
  EUC_3D = 3,
  MAX_2D = 4,
  MAX_3D = 5,
  MAN_2D = 6,
  MAN_3D = 7,
  CEIL_2D = 8,
  Geo = 9,
  Att = 10,
  Xray1 = 11,
  Xray2 = 12,
  Special = 13
}
