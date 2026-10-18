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
    public static OperationExecutorAdapter CreateExecutorAdapter<TCandidate, TSearchSpace, TProblem, TSearchState>()
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState => ChecksumExecutorAdapter.Instance;
}

public sealed class ChecksumExecutorAdapter : OperationExecutorAdapter<IChecksumExecutor>
{
    public static ChecksumExecutorAdapter Instance { get; } = new();

    public override bool SupportsCandidateWork => false;

    protected override IChecksumExecutor CreateWrappedExecutor<TWrapper>(IChecksumExecutor executor, TWrapper wrapper) =>
        new WrappedExecutor<TWrapper>(executor, wrapper);

    private readonly struct Invocation(IChecksumExecutor executor) : IInvocation<int>
    {
        public int Proceed() => executor.Read();
        public int CountCandidateWork(int result) => throw new NotSupportedException();
    }

    private sealed class WrappedExecutor<TWrapper>(IChecksumExecutor executor, TWrapper wrapper)
        : IChecksumExecutor, IExecutorControlProvider where TWrapper : struct, IInvocationWrapper
    {
        public int Read() => wrapper.Invoke<Invocation, int>(new Invocation(executor));
        public TControl? Get<TControl>() where TControl : class => ExecutorControls.Find<TControl>(executor);
    }
}

public static class ChecksumResolutionExtensions
{
    public static IChecksumExecutor Resolve<TCandidate, TSearchSpace, TProblem>(
        this ResolutionScope<TCandidate, TSearchSpace, TProblem> scope, IOperator<Checksum, TCandidate> source)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        (IChecksumExecutor)scope.ResolveOperator(source);
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
    public OperatorExecutorFactory CreateExecutorFactory() => OperatorExecutorFactory.Forward(ChildOperator);
}
