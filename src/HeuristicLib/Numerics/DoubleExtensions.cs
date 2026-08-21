namespace HEAL.HeuristicLib.Numerics;

public static class DoubleExtensions
{
    public static bool IsAlmost(this double a, double b, double tolerance = 1E-10) => Math.Abs(a - b) <= tolerance;
}
