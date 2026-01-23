#pragma warning disable S2368

namespace HEAL.HeuristicLib.Problems.TestFunctions.BBoB;

public record FAttractiveSectorData(double[] XOpt);

public record FBentCigarGeneralizedVersatileData(int ProportionLongAxesDenom);

public record FDiscusGeneralizedVersatileData(int ProportionShortAxesDenom);

public record FGallagherData(long RandomSeed, double[] XOpt, double[][] Rotation, double[][] XLocal, double[][] ArrScales, int NumberOfPeaks, double[] PeakValues);

public record FLunacekBiRastriginData(double[] XHat, double[] Z, double[] XOpt, double[][] Rot1, double[][] Rot2);

public record FSharpRidgeGeneralizedVersatileData(int ProportionOfLinearDims);

public record FStepEllipsoidData(
  double[] XOpt, // length = n
  double FOpt,
  double[][] Rot1, // [n][n]
  double[][] Rot2 // [n][n]
);

public class FWeierstrassData {
  public double F0 { get; set; }
  public double[] Ak { get; } = new double[12];
  public double[] Bk { get; } = new double[12];
}
