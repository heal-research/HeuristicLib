using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Tests.ExecutionInfrastructure.OperatorFoundation;

public interface IChecksumExecutor : IExecutor
{
    int Read(IRandomNumberGenerator random);
}

// This operation is compiled in the consumer, never in the backend.
public sealed class Checksum : IOperation<Checksum>
{
    static bool IOperation<Checksum>.IsExecutor<TCandidate, TSearchState>(IExecutor executor) =>
        executor is IChecksumExecutor;

    static IExecutor IOperation<Checksum>.Wrap<TCandidate, TSearchState, TWrapper>(IExecutor executor, TWrapper wrapper) =>
        new WrappedExecutor<TWrapper>(executor, wrapper);

    private readonly struct Invocation(IChecksumExecutor executor,
        IRandomNumberGenerator random) : IInvocation<int>
    {
        private readonly IChecksumExecutor executor = executor;
        private readonly IRandomNumberGenerator random = random;

        public IRandomNumberGenerator Random => random;

        public int Proceed(IExecutor? executor = null, IRandomNumberGenerator? random = null) =>
            ((IChecksumExecutor?)executor ?? this.executor).Read(random ?? this.random);
    }

    private sealed class WrappedExecutor<TWrapper>(IExecutor executor, TWrapper wrapper)
        : WrappingExecutor<IChecksumExecutor, TWrapper>(executor, wrapper), IChecksumExecutor
        where TWrapper : struct, IInvocationWrapper
    {
        public int Read(IRandomNumberGenerator random) =>
            Wrapper.Invoke<Invocation, int>(new(Executor, random));
    }
}

public static class ChecksumResolutionExtensions
{
    extension<TCandidate, TSearchSpace, TProblem>(ResolutionScope<TCandidate, TSearchSpace, TProblem> scope)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public IChecksumExecutor Resolve(IOperator<Checksum, TCandidate> source) =>
            (IChecksumExecutor)scope.ResolveOperator(source);
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
    private static Parameter<Forward<TOperation, TCandidate>, IOperator<TOperation, TCandidate>> ChildOperatorParameter { get; } =
        new(nameof(ChildOperator), static forward => forward.ChildOperator, static (forward, value) => forward with { ChildOperator = value });

    public IReadOnlyList<IParameter> GetParameters() => [ChildOperatorParameter];
    public IExecutor CreateExecutor<TSearchSpace, TProblem, TSearchState>(ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState> scope)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState =>
        scope.ResolveOperator(ChildOperator);
}
