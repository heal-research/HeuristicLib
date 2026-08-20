namespace HEAL.HeuristicLib.DataAnalysis;

public abstract class SupervisedData<TTarget>
    where TTarget : notnull
{
    protected SupervisedData(DataFrame inputs, Series<TTarget> target)
    {
        if (inputs.RowCount != target.Count)
            throw new ArgumentException($"Input row count {inputs.RowCount} must match target row count {target.Count}.", nameof(target));

        Inputs = inputs;
        Target = target;
    }

    public DataFrame Inputs { get; }
    public Series<TTarget> Target { get; }
    public int RowCount => Inputs.RowCount;
}
