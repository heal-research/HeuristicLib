using System.Diagnostics;

namespace HEAL.HeuristicLib.OperatorExtensions.MeasuredOperators;

public readonly record struct OperatorMetric(int Count, TimeSpan Duration)
{
  public static OperatorMetric Aggregate(OperatorMetric left, OperatorMetric right) => new(left.Count + right.Count, left.Duration + right.Duration);

  public static OperatorMetric operator +(OperatorMetric left, OperatorMetric right) => Aggregate(left, right);

  public static OperatorMetric Zero => new(0, TimeSpan.Zero);

  public static OperatorMetric Measure(int count, Action action)
  {
    var start = Stopwatch.GetTimestamp();
    action();
    var end = Stopwatch.GetTimestamp();

    return new OperatorMetric(count, Stopwatch.GetElapsedTime(start, end));
  }

  public static OperatorMetric Measure(Action action) => Measure(1, action);
}
