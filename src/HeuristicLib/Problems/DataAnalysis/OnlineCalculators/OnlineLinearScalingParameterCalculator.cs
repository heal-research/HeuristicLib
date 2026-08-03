using HEAL.HeuristicLib.DataAnalysis;
using HEAL.HeuristicLib.Optimization;

#pragma warning disable S2178
namespace HEAL.HeuristicLib.Problems.DataAnalysis.OnlineCalculators.Legacy;

public class OnlineLinearScalingParameterCalculator
{
    private readonly RunningCovariance statistics;
    private OnlineCalculatorError errorState;

    public OnlineLinearScalingParameterCalculator()
    {
        statistics = new RunningCovariance();
        Reset();
    }

    /// <summary>
    ///   Additive offset
    /// </summary>
    public double Alpha => statistics.MeanY - Beta * statistics.MeanX;

    /// <summary>
    ///   Multiplicative factor
    /// </summary>
    public double Beta => statistics.PopulationVarianceX.IsAlmost(0.0) ? 1 : statistics.PopulationCovariance / statistics.PopulationVarianceX;

    public OnlineCalculatorError ErrorState => errorState;

    public void Reset()
    {
        statistics.Reset();
        errorState = OnlineCalculatorError.InsufficientElementsAdded;
    }

    /// <summary>
    ///   Calculates linear scaling parameters in one pass.
    ///   The formulas to calculate the scaling parameters were taken from Scaled Symblic Regression by Maarten Keijzer.
    ///   http://www.springerlink.com/content/x035121165125175/
    /// </summary>
    public void Add(double original, double target)
    {
        if (!double.IsFinite(original) || !double.IsFinite(target) || (errorState & OnlineCalculatorError.InvalidValueAdded) > 0)
        {
            errorState |= OnlineCalculatorError.InvalidValueAdded;
            return;
        }

        statistics.Add(original, target);
        errorState &= ~OnlineCalculatorError.InsufficientElementsAdded;
    }

    /// <summary>
    ///   Calculates alpha and beta parameters to linearly scale elements of original to the scale and location of target
    ///   original[i] * beta + alpha
    /// </summary>
    /// <param name="original">Values that should be scaled</param>
    /// <param name="target">Target values to which the original values should be scaled</param>
    /// <param name="alpha">Additive offset for the linear scaling</param>
    /// <param name="beta">Multiplicative factor for the linear scaling</param>
    /// <param name="errorState">Flag that indicates if errors occurred in the calculation of the linea scaling parameters.</param>
    public static void Calculate(IEnumerable<double> original, IEnumerable<double> target, out double alpha, out double beta, out OnlineCalculatorError errorState)
    {
        var calculator = new OnlineLinearScalingParameterCalculator();
        using var originalEnumerator = original.GetEnumerator();
        using var targetEnumerator = target.GetEnumerator();

        // always move forward both enumerators (do not use short-circuit evaluation!)
        while (originalEnumerator.MoveNext() & targetEnumerator.MoveNext())
        {
            var originalElement = originalEnumerator.Current;
            var targetElement = targetEnumerator.Current;
            calculator.Add(originalElement, targetElement);
            if (calculator.ErrorState != OnlineCalculatorError.None)
            {
                break;
            }
        }

        // check if both enumerators are at the end to make sure both enumerations have the same length
        if (calculator.ErrorState == OnlineCalculatorError.None &&
          (originalEnumerator.MoveNext() || targetEnumerator.MoveNext()))
        {
            throw new ArgumentException("Number of elements in original and target enumeration do not match.");
        }

        errorState = calculator.ErrorState;
        alpha = calculator.Alpha;
        beta = calculator.Beta;
    }
}
