namespace HEAL.HeuristicLib.Problems.Dynamic.MovingPeaks;

public readonly record struct MovingPeaksParameters(
  int Dimension,
  int NumberOfPeaks,
  double LowerBound,
  double UpperBound,

  // Peak value shape / bounds
  double MinHeight,
  double MaxHeight,
  double MinWidth,
  double MaxWidth,

  // Dynamics (part of the same pack)
  double ShiftSeverity,
  double HeightSeverity,
  double WidthSeverity);
