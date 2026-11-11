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
        var executor = scope
            .For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>().Resolve(twice.AnalyzedBy(childCalls, wrapperCalls));
        var problem = new TestFunctionProblem(new SphereFunction(1));
        IReadOnlyList<RealVector> parents = [new(1.0)];

        executor.Mutate(parents, RandomNumberGenerator.Create(1), problem.SearchSpace, problem).ShouldBeSameAs(parents);
        scope.Results.Result(childCalls).CurrentCount.ShouldBe(2);
        scope.Results.Result(wrapperCalls).CurrentCount.ShouldBe(1);
    }

    [Fact]
    public void TheSameExternalInvocationWrapperSupportsAnExternalOperationAndForwardsItsControls()
    {
        var source = new ValueSource(42);
        var calls = new CallCount();
        var twice = new Twice<ReadValue, RealVector>(new CountingCalls<ReadValue, RealVector>(source, calls));
        var scope = ResolutionScope.Create()
            .For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>();
        var executor = (IReadValueExecutor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>)scope.ResolveOperator(twice);
        var problem = new TestFunctionProblem(new SphereFunction(1));

        executor.Read(RandomNumberGenerator.Create(1), problem.SearchSpace, problem).ShouldBe(42);
        calls.Invocations.ShouldBe(2);
        ExecutorControls.Find<IValueControl>(executor).ShouldNotBeNull().Value = 43;
        executor.Read(RandomNumberGenerator.Create(1), problem.SearchSpace, problem).ShouldBe(43);
        calls.Invocations.ShouldBe(4);
    }

    [Fact]
    public void ExternalOperationAndRetryAreReachedBySubstitutionAndForwardControls()
    {
        IOperator<ReadValue, RealVector> source = new ValueSource(42);
        var forwarded = new Forward<ReadValue, RealVector>(source).Retry(2);
        var calls = new CallCount();
        var counted = forwarded.Substitute(source, child => new CountingCalls<ReadValue, RealVector>(child, calls));
        var scope = ResolutionScope.Create()
            .For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>();
        var problem = new TestFunctionProblem(new SphereFunction(1));

        var executor = (IReadValueExecutor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>)scope.ResolveOperator(counted);

        executor.Read(RandomNumberGenerator.Create(1), problem.SearchSpace, problem).ShouldBe(42);
        calls.Invocations.ShouldBe(1);
        ExecutorControls.Find<IValueControl>(executor).ShouldNotBeNull().Value = 43;
        executor.Read(RandomNumberGenerator.Create(1), problem.SearchSpace, problem).ShouldBe(43);
        calls.Invocations.ShouldBe(2);
    }

    private interface IValueControl
    {
        int Value { get; set; }
    }

    // An external operation ends with the call's random number generator, search space and problem, like built-in ones.
    private interface IReadValueExecutor<TCandidate, in TSearchSpace, in TProblem> : IExecutor
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        int Read(IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
    }

    // An external operation names its executor interface and wraps an executor with one invocation, like built-in ones.
    private sealed class ReadValue : IOperation<ReadValue>
    {
        static bool IOperation<ReadValue>.IsExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>(IExecutor executor) =>
            executor is IReadValueExecutor<TCandidate, TSearchSpace, TProblem>;

        static IExecutor IOperation<ReadValue>.Wrap<TCandidate, TSearchSpace, TProblem, TSearchState, TWrapper>(IExecutor executor, TWrapper wrapper) =>
            new WrappedExecutor<TCandidate, TSearchSpace, TProblem, TWrapper>(executor, wrapper);

        private readonly struct Invocation<TCandidate, TSearchSpace, TProblem>(IReadValueExecutor<TCandidate, TSearchSpace, TProblem> executor,
            IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) : IInvocation<int>
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace>
        {
            private readonly IReadValueExecutor<TCandidate, TSearchSpace, TProblem> executor = executor;
            private readonly IRandomNumberGenerator random = random;

            public IRandomNumberGenerator Random => random;
            public ISearchSpace SearchSpace => searchSpace;
            public IProblem Problem => problem;

            public int Proceed(IExecutor? executor = null, IRandomNumberGenerator? random = null) =>
                ((IReadValueExecutor<TCandidate, TSearchSpace, TProblem>?)executor ?? this.executor).Read(random ?? this.random, searchSpace, problem);
        }

        private sealed class WrappedExecutor<TCandidate, TSearchSpace, TProblem, TWrapper>(IExecutor executor, TWrapper wrapper)
            : WrappingExecutor<IReadValueExecutor<TCandidate, TSearchSpace, TProblem>, TWrapper>(executor, wrapper), IReadValueExecutor<TCandidate, TSearchSpace, TProblem>
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace>
            where TWrapper : struct, IInvocationWrapper
        {
            public int Read(IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
                Wrapper.Invoke<Invocation<TCandidate, TSearchSpace, TProblem>, int>(new(Executor, random, searchSpace, problem));
        }
    }

    private sealed record ValueSource(int Value) : IOperator<ReadValue, RealVector>
    {
        IExecutor IOperator<RealVector>.CreateExecutor<TSearchSpace, TProblem, TSearchState>(
            ResolutionScope<RealVector, TSearchSpace, TProblem, TSearchState> scope) => new Executor(Value);

        private sealed class Executor(int value)
            : IReadValueExecutor<RealVector, ISearchSpace<RealVector>, IProblem<RealVector, ISearchSpace<RealVector>>>, IValueControl
        {
            public int Value { get; set; } = value;
            public int Read(IRandomNumberGenerator random, ISearchSpace<RealVector> searchSpace, IProblem<RealVector, ISearchSpace<RealVector>> problem) => Value;
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
            TOperation.Wrap<TCandidate, TSearchSpace, TProblem, TSearchState, CallTwice>(scope.ResolveOperator(ChildOperator), new CallTwice());
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
            TOperation.Wrap<TCandidate, TSearchSpace, TProblem, TSearchState, Counting>(scope.ResolveOperator(ChildOperator), new Counting(Calls));

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
