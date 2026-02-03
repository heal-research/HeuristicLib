namespace HEAL.HeuristicLib.Problems.Dynamic.MovingPeaks;

public readonly record struct MovingPeaksParameters(
  int Dimension,
  int NumberOfPeaks,
  double LowerBound,
  double UpperBound,
  double MinHeight,
  double MaxHeight,
  double MinWidth,
  double MaxWidth,
  double ShiftSeverity,
  double HeightSeverity,
  double WidthSeverity
);

public static class MovingPeaksDefaults
{

  public static int[] Scenario2_PeakCounts = [5, 10, 20, 50];

  public static int[] Scenario2_Dimensions = [5, 10, 20, 50];
  // Canonical “scenario 2”
  public static MovingPeaksParameters Scenario2() => new(
  5, 10,
  0, 100,
  30, 70,
  1, 12,
  1.0, 7.0, 1.0
  );

  // “Static landscape” control
  public static MovingPeaksParameters Scenario2_Static() => Scenario2() with {
    ShiftSeverity = 0.0,
    HeightSeverity = 0.0,
    WidthSeverity = 0.0
  };
}
