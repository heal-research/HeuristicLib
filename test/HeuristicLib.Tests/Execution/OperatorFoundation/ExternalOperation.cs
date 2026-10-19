using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.Tests.ExecutionInfrastructure.OperatorFoundation.Fixtures;

namespace HEAL.HeuristicLib.Tests.ExecutionInfrastructure.OperatorFoundation;

public interface IChecksumExecutor<TCandidate, in TSearchSpace, in TProblem> : IExecutor
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    int Read(IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
}

// This descriptor and its adapter are compiled in the consumer, never in the backend.
public sealed class Checksum : IOperationDescriptor<Checksum>
{
    public static OperationExecutorAdapter CreateExecutorAdapter<TCandidate, TSearchSpace, TProblem, TSearchState>()
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState => new ChecksumExecutorAdapter<TCandidate, TSearchSpace, TProblem>();
}

public sealed class ChecksumExecutorAdapter<TCandidate, TSearchSpace, TProblem> : OperationExecutorAdapter<IChecksumExecutor<TCandidate, TSearchSpace, TProblem>>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected override IChecksumExecutor<TCandidate, TSearchSpace, TProblem> CreateWrappedExecutor<TWrapper>(
        IChecksumExecutor<TCandidate, TSearchSpace, TProblem> executor, TWrapper wrapper) =>
        new WrappedExecutor<TWrapper>(executor, wrapper);

    private readonly struct Invocation(IChecksumExecutor<TCandidate, TSearchSpace, TProblem> executor,
        IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) : IInvocation<int>
    {
        private readonly IChecksumExecutor<TCandidate, TSearchSpace, TProblem> executor = executor;
        private readonly IRandomNumberGenerator random = random;

        public IRandomNumberGenerator Random => random;
        public int Proceed() => executor.Read(random, searchSpace, problem);
        public int ProceedWith(IExecutor? executor = null, IRandomNumberGenerator? random = null) =>
            ((IChecksumExecutor<TCandidate, TSearchSpace, TProblem>?)executor ?? this.executor).Read(random ?? this.random, searchSpace, problem);
    }

    private sealed class WrappedExecutor<TWrapper>(IChecksumExecutor<TCandidate, TSearchSpace, TProblem> executor, TWrapper wrapper)
        : IChecksumExecutor<TCandidate, TSearchSpace, TProblem>, IExecutorControlProvider where TWrapper : struct, IInvocationWrapper
    {
        public int Read(IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
            wrapper.Invoke<Invocation, int>(new Invocation(executor, random, searchSpace, problem));
        public TControl? Get<TControl>() where TControl : class => ExecutorControls.Find<TControl, TWrapper>(executor, wrapper);
    }
}

public static class ChecksumResolutionExtensions
{
    public static IChecksumExecutor<TCandidate, TSearchSpace, TProblem> Resolve<TCandidate, TSearchSpace, TProblem>(
        this ResolutionScope<TCandidate, TSearchSpace, TProblem> scope, IOperator<Checksum, TCandidate> source)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        (IChecksumExecutor<TCandidate, TSearchSpace, TProblem>)scope.ResolveOperator(source);
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
