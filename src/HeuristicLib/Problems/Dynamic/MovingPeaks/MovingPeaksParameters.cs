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

public static class MovingPeaksDefaults {
  // Canonical “scenario 2”
  public static MovingPeaksParameters Scenario2() => new(
    Dimension: 5, NumberOfPeaks: 10,
    LowerBound: 0, UpperBound: 100,
    MinHeight: 30, MaxHeight: 70,
    MinWidth: 1, MaxWidth: 12,
    ShiftSeverity: 1.0, HeightSeverity: 7.0, WidthSeverity: 1.0
  );

  public static int[] Scenario2_PeakCounts = [5, 10, 20, 50];
  public static int[] Scenario2_Dimensions = [5, 10, 20, 50];

  // “Static landscape” control
  public static MovingPeaksParameters Scenario2_Static() => Scenario2() with {
    ShiftSeverity = 0.0,
    HeightSeverity = 0.0,
    WidthSeverity = 0.0
  };
}
