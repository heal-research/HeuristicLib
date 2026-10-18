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
    public void AnExternalInvocationWrapperSupportsABuiltInOperationWithoutChangingItsAdapter()
    {
        IOperator<Mutation, RealVector> source = new IdentityMutation();
        var twice = new Twice<Mutation, RealVector>(source);
        var childCalls = new CountAccumulator();
        var wrapperCalls = new CountAccumulator();
        var executor = ResolutionScope.Create(Aspect.CountCalls(source, childCalls), Aspect.CountCalls(twice, wrapperCalls))
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
        var executor = (IReadValueExecutor)scope.ResolveOperator(twice);

        executor.Read().ShouldBe(42);
        counter.Calls.ShouldBe(2);
        ExecutorControls.Find<IValueControl>(executor).ShouldNotBeNull().Value = 43;
        executor.Read().ShouldBe(43);
        counter.Calls.ShouldBe(4);
    }

    [Fact]
    public void ExternalOperationAndRetryParticipateInGeneralAdviceAndForwardControls()
    {
        IOperator<ReadValue, RealVector> source = new ValueSource(42);
        var forwarded = new Forward<ReadValue, RealVector>(source).Retry(2);
        var counter = new Counter();
        var scope = ResolutionScope.Create(Aspect.Create(Pointcut.Reference(source), counter))
            .For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>();

        var executor = (IReadValueExecutor)scope.ResolveOperator(forwarded);

        executor.Read().ShouldBe(42);
        counter.Calls.ShouldBe(1);
        ExecutorControls.Find<IValueControl>(executor).ShouldNotBeNull().Value = 43;
        executor.Read().ShouldBe(43);
        counter.Calls.ShouldBe(2);
    }

    private interface IValueControl
    {
        int Value { get; set; }
    }

    private interface IReadValueExecutor : IExecutor
    {
        int Read();
    }

    // An external operation supplies one adapter, one invocation and one wrapping executor.
    private sealed class ReadValue : IOperationDescriptor<ReadValue>
    {
        public static OperationExecutorAdapter CreateExecutorAdapter<TCandidate, TSearchSpace, TProblem, TSearchState>()
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace>
            where TSearchState : class, ISearchState => new ExecutorAdapter();

        private sealed class ExecutorAdapter : OperationExecutorAdapter<IReadValueExecutor>
        {
            public override bool SupportsCandidateWork => false;

            protected override IReadValueExecutor CreateWrappedExecutor<TWrapper>(IReadValueExecutor executor, TWrapper wrapper) =>
                new WrappedExecutor<TWrapper>(executor, wrapper);
        }

        private readonly struct Invocation(IReadValueExecutor executor) : IInvocation<int>
        {
            public int Proceed() => executor.Read();
            public int CountCandidateWork(int result) => throw new NotSupportedException();
        }

        private sealed class WrappedExecutor<TWrapper>(IReadValueExecutor executor, TWrapper wrapper)
            : IReadValueExecutor, IExecutorControlProvider where TWrapper : struct, IInvocationWrapper
        {
            public int Read() => wrapper.Invoke<Invocation, int>(new Invocation(executor));
            public TControl? Get<TControl>() where TControl : class => ExecutorControls.Find<TControl>(executor);
        }
    }

    private sealed record ValueSource(int Value) : IOperator<ReadValue, RealVector>
    {
        public OperatorExecutorFactory CreateExecutorFactory() => OperatorExecutorFactory.FromFactory<IReadValueExecutor>(_ => new Executor(Value));

        private sealed class Executor(int value) : IReadValueExecutor, IValueControl
        {
            public int Value { get; set; } = value;
            public int Read() => Value;
        }
    }

    private sealed record IdentityMutation : StatelessMutator<RealVector>
    {
        public override IReadOnlyList<RealVector> Mutate(IReadOnlyList<RealVector> parents, IRandomNumberGenerator random) => parents;
    }

    // This configuration and its call wrapper use only public APIs and enumerate no operations.
    private sealed record Twice<TOperation, TCandidate>(IOperator<TOperation, TCandidate> ChildOperator)
        : WrappingOperator<TOperation, TCandidate>(ChildOperator) where TOperation : IOperationDescriptor<TOperation>
    {
        protected override OperatorExecutorFactory CreateOperatorFactory() =>
            OperatorExecutorFactory.WrapInvocation(ChildOperator, new CallTwice());
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
        : IOperator<TOperation, TCandidate> where TOperation : IOperationDescriptor<TOperation>
    {
        public bool Fits(ExecutionSignature execution) => ChildOperator.Fits(execution);
        public IOperatorContract<TCandidate>? CandidateContract => ChildOperator.CandidateContract;
        public OperatorExecutorFactory CreateExecutorFactory() => OperatorExecutorFactory.Forward(ChildOperator);
    }

    private sealed class Counter : OperatorAdvice
    {
        public int Calls { get; private set; }

        public override IExecutor Wrap(IExecutor executor, OperationExecutorAdapter operation, IOperator source) =>
            operation.WrapInvocation(executor, new Counting(this));

        private readonly struct Counting(Counter counter) : IInvocationWrapper
        {
            public TResult Invoke<TInvocation, TResult>(TInvocation invocation) where TInvocation : struct, IInvocation<TResult>
            {
                var result = invocation.Proceed();
                counter.Calls++;
                return result;
            }
        }
    }
}
