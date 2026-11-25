namespace HEAL.HeuristicLib.Numerics;

public static class DoubleExtensions
{
    extension(double a)
    {
        public bool IsAlmost(double b, double tolerance = 1E-10) => Math.Abs(a - b) <= tolerance;
    }
}
