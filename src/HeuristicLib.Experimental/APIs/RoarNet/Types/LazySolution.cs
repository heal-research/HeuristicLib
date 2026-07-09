namespace HEAL.HeuristicLib.APIs.RoarNet;

public class LazySolution<T> : Solution
{
    private bool evaluated;
    private bool bounded;
    private double? quality;
    private double? lowerBound;
    public readonly T Candidate;
    private readonly IEvaluationContext<T> context;

    public LazySolution(T candidate, IEvaluationContext<T> context)
    {
        this.Candidate = candidate;
        this.context = context;
    }

    private LazySolution(LazySolution<T> other)
    {
        Candidate = other.Candidate;
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
        quality = context.Evaluate(Candidate, out var b, out var bound);
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
        lowerBound = context.LowerBound(Candidate, out var e, out var q);
        if (!e)
            return lowerBound;

        evaluated = true;
        quality = q;

        return lowerBound;
    }

    public LazySolution<T> Copy() => new(this);
}
