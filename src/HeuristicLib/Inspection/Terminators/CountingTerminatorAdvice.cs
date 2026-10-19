using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Terminators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Inspection;

public sealed class CountingTerminatorAdvice<TCandidate> : TerminatorAdvice<TCandidate>
{
    public CountAccumulator Counter { get; }

    public CountingTerminatorAdvice(CountAccumulator counter)
    {
        Counter = counter;
    }

    public override WrapperExecutorFactory<ITerminationExecutor<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState>> CreateWrapperFactory<TRunSearchSpace, TRunProblem, TRunSearchState>(ITerminator<TCandidate> terminator) =>
        childTerminator => new Executor<TRunSearchSpace, TRunProblem, TRunSearchState>(childTerminator, Counter);

    private sealed class Executor<TSearchSpace, TProblem, TSearchState>(ITerminationExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> childTerminator, CountAccumulator counter)
        : WrappingTerminationExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>(childTerminator)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState
    {
        public override bool IsTerminalState(TSearchState state, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            var result = ChildTerminator.IsTerminalState(state, random, searchSpace, problem);
            counter.IncrementBy(1);
            return result;
        }
    }
}

public sealed record TerminatorCounting<TCandidate> : IOperatorCounting<ITerminator<TCandidate>>
{
    public Advice<ITerminator<TCandidate>> CreateAdvice(CountAccumulator counter) =>
        new CountingTerminatorAdvice<TCandidate>(counter);
}

public static class TerminatorCounterExtensions
{
    extension(Aspect)
    {
        public static Aspect CountCalls<TCandidate>(ITerminator<TCandidate> terminator, CountAccumulator counter) =>
            Aspect.Create(Pointcut.Reference(terminator), new CountingTerminatorAdvice<TCandidate>(counter));
    }
}
