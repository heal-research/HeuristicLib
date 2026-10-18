using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.Tests.ExecutionInfrastructure.OperatorFoundation.Fixtures;

namespace HEAL.HeuristicLib.Tests.ExecutionInfrastructure.OperatorFoundation;

public interface IChecksumExecutor : IExecutor
{
    int Read();
}

// This descriptor and its adapter are compiled in the consumer, never in the backend.
public sealed class Checksum : IOperationDescriptor<Checksum>
{
    public static OperationExecutorAdapter<Checksum, TCandidate, TSearchSpace, TProblem, TRunSearchState>
        CreateExecutorAdapter<TCandidate, TSearchSpace, TProblem, TRunSearchState>()
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TRunSearchState : class, ISearchState =>
        new ChecksumExecutorAdapter<TCandidate, TSearchSpace, TProblem, TRunSearchState>();
}

public sealed class ChecksumExecutorAdapter<TCandidate, TSearchSpace, TProblem, TRunSearchState>
    : OperationExecutorAdapter<Checksum, TCandidate, TSearchSpace, TProblem, TRunSearchState, IChecksumExecutor>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TRunSearchState : class, ISearchState
{
    public override bool SupportsCandidateWork => false;

    protected override IChecksumExecutor CreateInvocationWrapperExecutor(IChecksumExecutor child, IInvocationWrapper invocationWrapper) =>
        new InvocationWrapperExecutor(invocationWrapper.Wrap<ValueTuple, int>(_ => child.Read(), null));

    private sealed class InvocationWrapperExecutor(Func<ValueTuple, int> invoke) : IChecksumExecutor
    {
        public int Read() => invoke(default);
    }

}

public static class ChecksumResolutionExtensions
{
    public static IChecksumExecutor Resolve<TCandidate, TSearchSpace, TProblem>(
        this ResolutionScope<TCandidate, TSearchSpace, TProblem> scope, IOperator<Checksum, TCandidate> source)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        scope.Scope.ResolveOperator<Checksum, TCandidate, TSearchSpace, TProblem, ISearchState,
            IChecksumExecutor>(source, Checksum.CreateExecutorAdapter<TCandidate, TSearchSpace, TProblem, ISearchState>()).Executor;
}

public sealed class ChecksumRecommendation : IRecommends<IOperator<Checksum, RealVector>>
{
    public bool TryCreateRecommendedOperator(
        [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out IOperator<Checksum, RealVector>? recommendation)
    {
        recommendation = new SharedOperations(new ProbeLog());
        return true;
    }
}

// An external generic topology uses public preparation APIs without enumerating operations.
public sealed record Forward<TOperation, TCandidate>(IOperator<TOperation, TCandidate> ChildOperator)
    : IOperator<TOperation, TCandidate> where TOperation : IOperationDescriptor<TOperation>
{
    public IOperatorContract<TCandidate>? CandidateContract => ChildOperator.CandidateContract;
    public bool Fits(ExecutionSignature execution) => ChildOperator.Fits(execution);
    public OperatorExecutorFactory CreateExecutorFactory() => new Factory(ChildOperator);

    private sealed class Factory(IOperator<TOperation, TCandidate> child) : OperatorExecutorFactory
    {
        public override ExecutorFactory GetExecutorFactory<TBoundOperation, TBoundCandidate, TSearchSpace, TProblem, TRunSearchState>(
            OperationExecutorAdapter<TBoundOperation, TBoundCandidate, TSearchSpace, TProblem, TRunSearchState> adapter) =>
            child is IOperator<TBoundOperation, TBoundCandidate> typed
                ? adapter.Forward(typed)
                : throw new InvalidOperationException("Forward requested with a different candidate or operation.");
    }
}
