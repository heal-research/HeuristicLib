using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Tests.ApiUsageSpecs.Execution;

public sealed partial class OperatorPreparationSpecs
{
    [Fact]
    public void AnExternalInvocationWrapperSupportsABuiltInOperationWithoutChangingIt()
    {
        IOperator<Mutation, RealVector> source = new IdentityMutation();
        var twice = new Twice<Mutation, RealVector>(source);
        var childCalls = source.Invocations.Count();
        var wrapperCalls = twice.Invocations.Count();
        var scope = ResolutionScope.Create();
        var problem = new TestFunctionProblem(new SphereFunction(1));
        var executor = scope
            .For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(problem).Resolve(twice.AnalyzedBy(childCalls, wrapperCalls));
        IReadOnlyList<RealVector> parents = [new(1.0)];

        executor.Mutate(parents, RandomNumberGenerator.Create(1)).ShouldBeSameAs(parents);
        scope.Results.Result(childCalls).CurrentCount.ShouldBe(2);
        scope.Results.Result(wrapperCalls).CurrentCount.ShouldBe(1);
    }

    [Fact]
    public void TheSameExternalInvocationWrapperSupportsAnExternalOperation()
    {
        var source = new ValueSource(42);
        var calls = new CallCount();
        var twice = new Twice<ReadValue, RealVector>(new CountingCalls<ReadValue, RealVector>(source, calls));
        var problem = new TestFunctionProblem(new SphereFunction(1));
        var scope = ResolutionScope.Create()
            .For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(problem);
        var executor = (IReadValueExecutor)scope.ResolveOperator(twice);

        executor.Read(RandomNumberGenerator.Create(1)).ShouldBe(42);
        calls.Invocations.ShouldBe(2);
    }

    [Fact]
    public void ExternalOperationAndRetryAreReachedBySubstitution()
    {
        IOperator<ReadValue, RealVector> source = new ValueSource(42);
        var forwarded = new Forward<ReadValue, RealVector>(source).Retry(2);
        var calls = new CallCount();
        var counted = forwarded.Substitute(source, child => new CountingCalls<ReadValue, RealVector>(child, calls));
        var problem = new TestFunctionProblem(new SphereFunction(1));
        var scope = ResolutionScope.Create()
            .For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(problem);

        var executor = (IReadValueExecutor)scope.ResolveOperator(counted);

        executor.Read(RandomNumberGenerator.Create(1)).ShouldBe(42);
        calls.Invocations.ShouldBe(1);
    }

    // An external operation ends with the call's random number generator, like built-in ones.
    private interface IReadValueExecutor : IExecutor
    {
        int Read(IRandomNumberGenerator random);
    }

    // An external operation names its executor interface and wraps an executor with one invocation, like built-in ones.
    private sealed class ReadValue : IOperation<ReadValue>
    {
        static bool IOperation<ReadValue>.IsExecutor<TCandidate, TSearchState>(IExecutor executor) =>
            executor is IReadValueExecutor;

        static IExecutor IOperation<ReadValue>.Wrap<TCandidate, TSearchState, TWrapper>(IExecutor executor, TWrapper wrapper) =>
            new WrappedExecutor<TWrapper>(executor, wrapper);

        private readonly struct Invocation(IReadValueExecutor executor,
            IRandomNumberGenerator random) : IInvocation<int>
        {
            private readonly IReadValueExecutor executor = executor;
            private readonly IRandomNumberGenerator random = random;

            public IRandomNumberGenerator Random => random;

            public int Proceed(IExecutor? executor = null, IRandomNumberGenerator? random = null) =>
                ((IReadValueExecutor?)executor ?? this.executor).Read(random ?? this.random);
        }

        private sealed class WrappedExecutor<TWrapper>(IExecutor executor, TWrapper wrapper)
            : WrappingExecutor<IReadValueExecutor, TWrapper>(executor, wrapper), IReadValueExecutor
            where TWrapper : struct, IInvocationWrapper
        {
            public int Read(IRandomNumberGenerator random) =>
                Wrapper.Invoke<Invocation, int>(new(Executor, random));
        }
    }

    private sealed record ValueSource(int Value) : IOperator<ReadValue, RealVector>
    {
        IExecutor IOperator<RealVector>.CreateExecutor<TSearchSpace, TProblem, TSearchState>(
            ResolutionScope<RealVector, TSearchSpace, TProblem, TSearchState> scope) => new Executor(Value);

        private sealed class Executor(int value)
            : IReadValueExecutor
        {
            public int Read(IRandomNumberGenerator random) => value;
        }
    }

    private sealed record IdentityMutation : StatelessMutator<RealVector>
    {
        public override IReadOnlyList<RealVector> Mutate(IReadOnlyList<RealVector> parents, IRandomNumberGenerator random) => parents;
    }

    // This configuration and its call wrapper use only public APIs and enumerate no operations.
    private sealed record Twice<TOperation, TCandidate>(IOperator<TOperation, TCandidate> ChildOperator)
        : WrappingOperator<TOperation, TCandidate>(ChildOperator) where TOperation : IOperation<TOperation>
    {
        protected override IExecutor CreateExecutor<TSearchSpace, TProblem, TSearchState>(ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState> scope) =>
            TOperation.Wrap<TCandidate, TSearchState, CallTwice>(scope.ResolveOperator(ChildOperator), new CallTwice());
    }

    private readonly struct CallTwice : IInvocationWrapper
    {
        public TResult Invoke<TInvocation, TResult>(TInvocation invocation) where TInvocation : struct, IInvocation<TResult>
        {
            _ = invocation.Proceed();
            return invocation.Proceed();
        }
    }

    private sealed record Forward<TOperation, TCandidate>(IOperator<TOperation, TCandidate> ChildOperator)
        : IOperator<TOperation, TCandidate> where TOperation : IOperation<TOperation>
    {
        private static Parameter<Forward<TOperation, TCandidate>, IOperator<TOperation, TCandidate>> ChildOperatorParameter { get; } =
            new(nameof(ChildOperator), static forward => forward.ChildOperator, static (forward, value) => forward with { ChildOperator = value });

        public IReadOnlyList<IParameter> GetParameters() => [ChildOperatorParameter];
        public IOperatorContract<TCandidate>? CandidateContract => ChildOperator.CandidateContract;
        IExecutor IOperator<TCandidate>.CreateExecutor<TSearchSpace, TProblem, TSearchState>(
            ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState> scope) => scope.ResolveOperator(ChildOperator);
    }

    private sealed class CallCount
    {
        public int Invocations { get; set; }
    }

    // A general wrapper for any operation, built from public APIs: it counts the successful calls of its child.
    private sealed record CountingCalls<TOperation, TCandidate>(IOperator<TOperation, TCandidate> ChildOperator, CallCount Calls)
        : WrappingOperator<TOperation, TCandidate>(ChildOperator) where TOperation : IOperation<TOperation>
    {
        protected override IExecutor CreateExecutor<TSearchSpace, TProblem, TSearchState>(ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState> scope) =>
            TOperation.Wrap<TCandidate, TSearchState, Counting>(scope.ResolveOperator(ChildOperator), new Counting(Calls));

        private readonly struct Counting(CallCount calls) : IInvocationWrapper
        {
            public TResult Invoke<TInvocation, TResult>(TInvocation invocation) where TInvocation : struct, IInvocation<TResult>
            {
                var result = invocation.Proceed();
                calls.Invocations++;
                return result;
            }
        }
    }
}
