using HEAL.HeuristicLib.Analysis;

namespace HEAL.HeuristicLib.Operators.Terminators;

public record AfterOperatorCountTerminator<TGenotype> : StatelessTerminator<TGenotype>
{
    public AfterOperatorCountTerminator(InvocationCounter counter, int maximumCount)
    {
        this.counter = counter;
        this.maximumCount = maximumCount;
    }

    private readonly InvocationCounter counter;
    private readonly int maximumCount;

    public override bool IsTerminalState()
    {
        return counter.CurrentCount >= maximumCount;
    }
}
