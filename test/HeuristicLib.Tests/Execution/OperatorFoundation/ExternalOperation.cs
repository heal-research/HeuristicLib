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

    protected override IChecksumExecutor CreateSuccessCallbackExecutor<TCallback>(IChecksumExecutor child, TCallback callback) =>
        new SuccessCallbackExecutor<TCallback>(child, callback);

    protected override IChecksumExecutor CreateCallCallbacksExecutor<TCallbacks, TCallData>(IChecksumExecutor child, TCallbacks callbacks) =>
        new CallCallbacksExecutor<TCallbacks, TCallData>(child, callbacks);

    protected override IChecksumExecutor CreatePolicyExecutor(ImmutableArray<IChecksumExecutor> children, SynchronousPolicy policy) =>
        new PolicyExecutor(children, policy);

    private sealed class PolicyExecutor(ImmutableArray<IChecksumExecutor> children, SynchronousPolicy policy) : IChecksumExecutor
    {
        public int Read() => policy.Execute(children, default(ValueTuple), null,
            static (child, _) => new OperationCallResult<int>(child.Read(), 0)).Value;
    }

    private sealed class CallCallbacksExecutor<TCallbacks, TCallData>(IChecksumExecutor child, TCallbacks callbacks) : IChecksumExecutor
        where TCallbacks : struct, IOperationCallCallbacks<TCallData>
    {
        public int Read()
        {
            var notifier = new OperationCallNotifier<TCallbacks, TCallData>(callbacks);
            try
            {
                int result;
                try
                { result = child.Read(); }
                catch (Exception failure)
                {
                    notifier.NotifyFailed(failure);
                    throw;
                }
                notifier.NotifySucceeded(0);
                return result;
            }
            finally { notifier.Finish(); }
        }
    }

    private sealed class SuccessCallbackExecutor<TCallback>(IChecksumExecutor child, TCallback callback) : IChecksumExecutor
        where TCallback : struct, IOperationSuccessCallback
    {
        public int Read()
        {
            var result = child.Read();
            callback.OnSucceeded(0);
            return result;
        }
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
