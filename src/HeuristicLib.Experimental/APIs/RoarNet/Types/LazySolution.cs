namespace HEAL.HeuristicLib.APIs.RoarNet;

public class LazySolution<T> : Solution
{
    private bool evaluated;
    private bool bounded;
    private double? quality;
    private double? lowerBound;
    public readonly T Genotype;
    private readonly IEvaluationContext<T> context;

    public LazySolution(T genotype, IEvaluationContext<T> context)
    {
        this.Genotype = genotype;
        this.context = context;
    }

    private LazySolution(LazySolution<T> other)
    {
        Genotype = other.Genotype;
        context = other.context;
        evaluated = other.evaluated;
        bounded = other.bounded;
        quality = other.quality;
        lowerBound = other.lowerBound;
    }

    public double? Quality()
    {
        if (evaluated)
            return quality;
        quality = context.Evaluate(Genotype, out var b, out var bound);
        if (b)
        {
            lowerBound = bound;
            bounded = true;
        }

        evaluated = true;
        return quality;
    }

    public double? LowerBound()
    {
        if (bounded)
            return lowerBound;
        lowerBound = context.LowerBound(Genotype, out var e, out var q);
        if (!e)
            return lowerBound;

        evaluated = true;
        quality = q;

        return lowerBound;
    }

    public LazySolution<T> Copy() => new(this);
}
