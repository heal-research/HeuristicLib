using HEAL.HeuristicLib.Genotypes.SymbolicExpressions;

namespace HEAL.HeuristicLib.Problems.DataAnalysis.Regression;

public sealed class RegressionData
{
    private RegressionData(
        DataFrame trainingInputs,
        Series<double> trainingTarget,
        DataFrame? validationInputs,
        Series<double>? validationTarget,
        DataFrame? testInputs,
        Series<double>? testTarget)
    {
        TargetName = ValidateSplit(trainingInputs, trainingTarget, nameof(trainingInputs), nameof(trainingTarget));
        ValidateOptionalSplit(
            validationInputs,
            validationTarget,
            nameof(validationInputs),
            nameof(validationTarget),
            TargetName);
        ValidateOptionalSplit(testInputs, testTarget, nameof(testInputs), nameof(testTarget), TargetName);

        TrainingInputs = trainingInputs;
        TrainingTarget = trainingTarget;
        ValidationInputs = validationInputs;
        ValidationTarget = validationTarget;
        TestInputs = testInputs;
        TestTarget = testTarget;
    }

    public DataFrame TrainingInputs { get; }
    public Series<double> TrainingTarget { get; }
    public DataFrame? ValidationInputs { get; }
    public Series<double>? ValidationTarget { get; }
    public DataFrame? TestInputs { get; }
    public Series<double>? TestTarget { get; }
    public string TargetName { get; }

    public static RegressionData Training(DataFrame inputs, Series<double> target) =>
        new(inputs, target, validationInputs: null, validationTarget: null, testInputs: null, testTarget: null);

    public static RegressionData WithTrainingAndValidation(
        DataFrame trainingInputs,
        Series<double> trainingTarget,
        DataFrame validationInputs,
        Series<double> validationTarget) =>
        new(trainingInputs, trainingTarget, validationInputs, validationTarget, testInputs: null, testTarget: null);

    public static RegressionData WithTrainingValidationAndTest(
        DataFrame trainingInputs,
        Series<double> trainingTarget,
        DataFrame validationInputs,
        Series<double> validationTarget,
        DataFrame testInputs,
        Series<double> testTarget) =>
        new(trainingInputs, trainingTarget, validationInputs, validationTarget, testInputs, testTarget);

    private static void ValidateOptionalSplit(
        DataFrame? inputs,
        Series<double>? target,
        string inputsParameterName,
        string targetParameterName,
        string expectedTargetName)
    {
        if (inputs is null && target is null)
            return;

        if (inputs is null)
            throw new ArgumentException("Inputs must be supplied when a target is supplied.", inputsParameterName);

        if (target is null)
            throw new ArgumentException("Target must be supplied when inputs are supplied.", targetParameterName);

        var targetName = ValidateSplit(inputs, target, inputsParameterName, targetParameterName);
        if (!StringComparer.Ordinal.Equals(targetName, expectedTargetName))
        {
            throw new ArgumentException(
                $"Target series name '{targetName}' must match training target name '{expectedTargetName}'.",
                targetParameterName);
        }
    }

    private static string ValidateSplit(
        DataFrame inputs,
        Series<double> target,
        string inputsParameterName,
        string targetParameterName)
    {
        if (inputs is null)
            throw new ArgumentNullException(inputsParameterName);

        if (target is null)
            throw new ArgumentNullException(targetParameterName);

        if (inputs.RowCount != target.Count)
        {
            throw new ArgumentException(
                $"Input row count {inputs.RowCount} must match target row count {target.Count}.",
                targetParameterName);
        }

        if (string.IsNullOrWhiteSpace(target.Name))
            throw new ArgumentException("Regression target series must have a name.", targetParameterName);

        return target.Name;
    }
}
