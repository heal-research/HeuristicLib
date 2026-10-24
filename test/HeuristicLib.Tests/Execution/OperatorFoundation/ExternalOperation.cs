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

// This operation is compiled in the consumer, never in the backend.
public sealed class Checksum : IOperation<Checksum>
{
    static bool IOperation<Checksum>.IsExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>(IExecutor executor) =>
        executor is IChecksumExecutor<TCandidate, TSearchSpace, TProblem>;

    static IExecutor IOperation<Checksum>.Wrap<TCandidate, TSearchSpace, TProblem, TSearchState, TWrapper>(IExecutor executor, TWrapper wrapper) =>
        new WrappedExecutor<TCandidate, TSearchSpace, TProblem, TWrapper>(executor, wrapper);

    private readonly struct Invocation<TCandidate, TSearchSpace, TProblem>(IChecksumExecutor<TCandidate, TSearchSpace, TProblem> executor,
        IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) : IInvocation<int>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        private readonly IChecksumExecutor<TCandidate, TSearchSpace, TProblem> executor = executor;
        private readonly IRandomNumberGenerator random = random;

        public IRandomNumberGenerator Random => random;
        public ISearchSpace SearchSpace => searchSpace;
        public IProblem Problem => problem;

        public int Proceed(IExecutor? executor = null, IRandomNumberGenerator? random = null) =>
            ((IChecksumExecutor<TCandidate, TSearchSpace, TProblem>?)executor ?? this.executor).Read(random ?? this.random, searchSpace, problem);
    }

    private sealed class WrappedExecutor<TCandidate, TSearchSpace, TProblem, TWrapper>(IExecutor executor, TWrapper wrapper)
        : WrappingExecutor<IChecksumExecutor<TCandidate, TSearchSpace, TProblem>, TWrapper>(executor, wrapper), IChecksumExecutor<TCandidate, TSearchSpace, TProblem>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TWrapper : struct, IInvocationWrapper
    {
        public int Read(IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
            Wrapper.Invoke<Invocation<TCandidate, TSearchSpace, TProblem>, int>(new(Executor, random, searchSpace, problem));
    }
}

public static class ChecksumResolutionExtensions
{
    extension<TCandidate, TSearchSpace, TProblem>(ResolutionScope<TCandidate, TSearchSpace, TProblem> scope)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public IChecksumExecutor<TCandidate, TSearchSpace, TProblem> Resolve(IOperator<Checksum, TCandidate> source) =>
            (IChecksumExecutor<TCandidate, TSearchSpace, TProblem>)scope.ResolveOperator(source);
    }
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
    : IOperator<TOperation, TCandidate> where TOperation : IOperation<TOperation>
{
    public IOperatorContract<TCandidate>? CandidateContract => ChildOperator.CandidateContract;
    public bool Fits(ExecutionSignature execution) => ChildOperator.Fits(execution);
    public IExecutor CreateExecutor<TSearchSpace, TProblem, TSearchState>(ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState> scope)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState =>
        scope.ResolveOperator(ChildOperator);
}
