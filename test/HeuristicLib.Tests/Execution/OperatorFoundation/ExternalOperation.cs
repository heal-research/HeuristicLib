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
    public static OperationExecutorAdapter<TCandidate, Checksum, TSearchSpace, TProblem, TRunSearchState>
        CreateExecutorAdapter<TCandidate, TSearchSpace, TProblem, TRunSearchState>()
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TRunSearchState : class, ISearchState =>
        new ChecksumExecutorAdapter<TCandidate, TSearchSpace, TProblem, TRunSearchState>();
}

public sealed class ChecksumExecutorAdapter<TCandidate, TSearchSpace, TProblem, TRunSearchState>
    : OperationExecutorAdapter<TCandidate, Checksum, TSearchSpace, TProblem, TRunSearchState, IChecksumExecutor>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TRunSearchState : class, ISearchState
{
    public override bool SupportsCandidateWork => false;

    protected override IChecksumExecutor CreateSuccessCallbackExecutor<TCallback>(IChecksumExecutor child, TCallback callback) =>
        new SuccessCallbackExecutor<TCallback>(child, callback);

    protected override IChecksumExecutor CreateCallCallbacksExecutor<TCallbacks, TCallData>(IChecksumExecutor child, TCallbacks callbacks) =>
        new CallCallbacksExecutor<TCallbacks, TCallData>(child, callbacks);

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
        this ResolutionScope<TCandidate, TSearchSpace, TProblem> scope, IOperator<TCandidate, Checksum> source)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        scope.Scope.ResolveOperator<TCandidate, Checksum, TSearchSpace, TProblem, ISearchState,
            IChecksumExecutor>(source, Checksum.CreateExecutorAdapter<TCandidate, TSearchSpace, TProblem, ISearchState>()).Executor;
}

public sealed class ChecksumRecommendation : IRecommends<IOperator<RealVector, Checksum>>
{
    public bool TryCreateRecommendedOperator(
        [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out IOperator<RealVector, Checksum>? recommendation)
    {
        recommendation = new SharedOperations(new ProbeLog());
        return true;
    }
}

// An external generic topology uses public preparation APIs without enumerating operations.
public sealed record Forward<TCandidate, TOperation>(IOperator<TCandidate, TOperation> ChildOperator)
    : IOperator<TCandidate, TOperation> where TOperation : IOperationDescriptor<TOperation>
{
    public IOperatorContract<TCandidate>? CandidateContract => ChildOperator.CandidateContract;
    public bool Fits(ExecutionSignature execution) => ChildOperator.Fits(execution);
    public OperatorExecutorFactory CreateExecutorFactory() => new Factory(ChildOperator);

    private sealed class Factory(IOperator<TCandidate, TOperation> child) : OperatorExecutorFactory
    {
        public override ExecutorFactory GetExecutorFactory<TBoundCandidate, TBoundOperation, TSearchSpace, TProblem, TRunSearchState>(
            OperationExecutorAdapter<TBoundCandidate, TBoundOperation, TSearchSpace, TProblem, TRunSearchState> adapter) =>
            child is IOperator<TBoundCandidate, TBoundOperation> typed
                ? adapter.Forward(typed)
                : throw new InvalidOperationException("Forward requested with a different candidate or operation.");
    }
}
