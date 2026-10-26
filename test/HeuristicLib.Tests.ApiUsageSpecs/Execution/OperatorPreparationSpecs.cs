using HEAL.HeuristicLib.Algorithms;
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

public sealed class OperatorPreparationSpecs
{
    [Fact]
    public void AnExternalInvocationWrapperSupportsABuiltInOperationWithoutChangingIt()
    {
        IOperator<Mutation, RealVector> source = new IdentityMutation();
        var twice = new Twice<Mutation, RealVector>(source);
        var childCalls = new CountAccumulator();
        var wrapperCalls = new CountAccumulator();
        var executor = ResolutionScope.Create(Aspect.CountInvocations(source, childCalls), Aspect.CountInvocations(twice, wrapperCalls))
            .For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>().Resolve(twice);
        var problem = new TestFunctionProblem(new SphereFunction(1));
        IReadOnlyList<RealVector> parents = [new(1.0)];

        executor.Mutate(parents, RandomNumberGenerator.Create(1), problem.SearchSpace, problem).ShouldBeSameAs(parents);
        childCalls.CurrentCount.ShouldBe(2);
        wrapperCalls.CurrentCount.ShouldBe(1);
    }

    [Fact]
    public void TheSameExternalInvocationWrapperSupportsAnExternalOperationAndForwardsItsControls()
    {
        var source = new ValueSource(42);
        var twice = new Twice<ReadValue, RealVector>(source);
        var counter = new Counter();
        var scope = ResolutionScope.Create(Aspect.Create(Pointcut.Reference(source), counter))
            .For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>();
        var executor = (IReadValueExecutor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>)scope.ResolveOperator(twice);
        var problem = new TestFunctionProblem(new SphereFunction(1));

        executor.Read(RandomNumberGenerator.Create(1), problem.SearchSpace, problem).ShouldBe(42);
        counter.Invocations.ShouldBe(2);
        ExecutorControls.Find<IValueControl>(executor).ShouldNotBeNull().Value = 43;
        executor.Read(RandomNumberGenerator.Create(1), problem.SearchSpace, problem).ShouldBe(43);
        counter.Invocations.ShouldBe(4);
    }

    [Fact]
    public void ExternalOperationAndRetryParticipateInGeneralAdviceAndForwardControls()
    {
        IOperator<ReadValue, RealVector> source = new ValueSource(42);
        var forwarded = new Forward<ReadValue, RealVector>(source).Retry(2);
        var counter = new Counter();
        var scope = ResolutionScope.Create(Aspect.Create(Pointcut.Reference(source), counter))
            .For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>();
        var problem = new TestFunctionProblem(new SphereFunction(1));

        var executor = (IReadValueExecutor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>)scope.ResolveOperator(forwarded);

        executor.Read(RandomNumberGenerator.Create(1), problem.SearchSpace, problem).ShouldBe(42);
        counter.Invocations.ShouldBe(1);
        ExecutorControls.Find<IValueControl>(executor).ShouldNotBeNull().Value = 43;
        executor.Read(RandomNumberGenerator.Create(1), problem.SearchSpace, problem).ShouldBe(43);
        counter.Invocations.ShouldBe(2);
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
        IExecutor IOperator<ReadValue, RealVector>.CreateExecutor<TSearchSpace, TProblem, TSearchState>(
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
        public bool Fits(ExecutionSignature execution) => ChildOperator.Fits(execution);
        public IOperatorContract<TCandidate>? CandidateContract => ChildOperator.CandidateContract;
        IExecutor IOperator<TOperation, TCandidate>.CreateExecutor<TSearchSpace, TProblem, TSearchState>(
            ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState> scope) => scope.ResolveOperator(ChildOperator);
    }

    private sealed class Counter : OperatorAdvice
    {
        public int Invocations { get; private set; }

        public override IExecutor Wrap(IExecutor executor, OperationInfo operation, IOperator source) =>
            operation.Wrap(executor, new Counting(this));

        private readonly struct Counting(Counter counter) : IInvocationWrapper
        {
            public TResult Invoke<TInvocation, TResult>(TInvocation invocation) where TInvocation : struct, IInvocation<TResult>
            {
                var result = invocation.Proceed();
                counter.Invocations++;
                return result;
            }
        }
    }
}
