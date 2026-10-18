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
        var scope = ResolutionScope.Create(Aspect.Create(Pointcut.Reference(source), counter));
        var adapter = ReadValue.CreateExecutorAdapter<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, ISearchState>();
        var bound = scope.ResolveOperator<ReadValue, RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem,
            ISearchState, IReadValueExecutor>(twice, adapter);

        bound.Executor.Read().ShouldBe(42);
        counter.Calls.ShouldBe(2);
        bound.ControlProvider.Get<IValueControl>().ShouldNotBeNull().Value = 43;
        bound.Executor.Read().ShouldBe(43);
        counter.Calls.ShouldBe(4);
    }

    [Fact]
    public void ExternalOperationAndRetryParticipateInGeneralAdviceAndForwardControls()
    {
        IOperator<ReadValue, RealVector> source = new ValueSource(42);
        var forwarded = new Forward<ReadValue, RealVector>(source).Retry(2);
        var counter = new Counter();
        var scope = ResolutionScope.Create(Aspect.Create(Pointcut.Reference(source), counter));

        var binding = ReadValue.CreateExecutorAdapter<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, ISearchState>();
        var bound = scope.ResolveOperator<ReadValue, RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem,
            ISearchState, IReadValueExecutor>(forwarded, binding);

        bound.Executor.Read().ShouldBe(42);
        counter.Calls.ShouldBe(1);
        bound.ControlProvider.Get<IValueControl>().ShouldNotBeNull().Value = 43;
        bound.Executor.Read().ShouldBe(43);
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

    private sealed class ReadValue : IOperationDescriptor<ReadValue>
    {
        public static OperationExecutorAdapter<ReadValue, TCandidate, TSearchSpace, TProblem, TRunSearchState>
            CreateExecutorAdapter<TCandidate, TSearchSpace, TProblem, TRunSearchState>()
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace>
            where TRunSearchState : class, ISearchState => new ExecutorAdapter<TCandidate, TSearchSpace, TProblem, TRunSearchState>();

        private sealed class ExecutorAdapter<TCandidate, TSearchSpace, TProblem, TRunSearchState>
            : OperationExecutorAdapter<ReadValue, TCandidate, TSearchSpace, TProblem, TRunSearchState, IReadValueExecutor>
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace>
            where TRunSearchState : class, ISearchState
        {
            public override bool SupportsCandidateWork => false;

            protected override IReadValueExecutor CreateInvocationWrapperExecutor(IReadValueExecutor child, IInvocationWrapper invocationWrapper) =>
                new InvocationWrapperExecutor(invocationWrapper.Wrap<ValueTuple, int>(_ => child.Read(), null));

            private sealed class InvocationWrapperExecutor(Func<ValueTuple, int> invoke) : IReadValueExecutor
            {
                public int Read() => invoke(default);
            }

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

    // This configuration and its call factory use only public APIs and enumerate no operations.
    private sealed record Twice<TOperation, TCandidate>(IOperator<TOperation, TCandidate> ChildOperator)
        : WrappingOperator<TOperation, TCandidate>(ChildOperator) where TOperation : IOperationDescriptor<TOperation>
    {
        protected override OperatorExecutorFactory CreateOperatorFactory() => new Factory(ChildOperator);

        private sealed class Factory(IOperator<TOperation, TCandidate> child)
            : OperatorExecutorFactory<TOperation, TCandidate>, IInvocationWrapper
        {
            public override ExecutorFactory GetExecutorFactory<TSearchSpace, TProblem, TRunSearchState>(
                OperationExecutorAdapter<TOperation, TCandidate, TSearchSpace, TProblem, TRunSearchState> adapter) =>
                adapter.Wrap(child, adapter.WrapInvocation(this));

            public Func<TArguments, TResult> Wrap<TArguments, TResult>(Func<TArguments, TResult> invokeChild,
            Func<TArguments, TResult, int>? countCandidateWork) => arguments =>
            {
                _ = invokeChild(arguments);
                return invokeChild(arguments);
            };
        }
    }

    private sealed record Forward<TOperation, TCandidate>(IOperator<TOperation, TCandidate> ChildOperator)
        : IOperator<TOperation, TCandidate> where TOperation : IOperationDescriptor<TOperation>
    {
        public bool Fits(ExecutionSignature execution) => ChildOperator.Fits(execution);
        public IOperatorContract<TCandidate>? CandidateContract => ChildOperator.CandidateContract;
        public OperatorExecutorFactory CreateExecutorFactory() => new Factory(ChildOperator);

        private sealed class Factory(IOperator<TOperation, TCandidate> child) : OperatorExecutorFactory
        {
            public override ExecutorFactory GetExecutorFactory<TBoundOperation, TBoundCandidate, TSearchSpace, TProblem, TRunSearchState>(
                OperationExecutorAdapter<TBoundOperation, TBoundCandidate, TSearchSpace, TProblem, TRunSearchState> adapter) =>
                child is IOperator<TBoundOperation, TBoundCandidate> typed
                    ? adapter.Forward(typed)
                    : throw new InvalidOperationException("Incompatible forwarding view.");
        }
    }

    private sealed class Counter : OperatorAdvice
    {
        public int Calls { get; private set; }
        public override OperatorAdviceWrapperFactory CreateWrapperFactory(IOperator source) => new WrapperFactory(this);

        private sealed class WrapperFactory(Counter counter) : OperatorAdviceWrapperFactory, IInvocationWrapper
        {
            public override WrapperExecutorFactory GetWrapperFactory<TOperation, TCandidate, TSearchSpace, TProblem, TRunSearchState>(
                IOperator<TOperation, TCandidate> source,
                OperationExecutorAdapter<TOperation, TCandidate, TSearchSpace, TProblem, TRunSearchState> adapter) => adapter.WrapInvocation(this);

            public Func<TArguments, TResult> Wrap<TArguments, TResult>(Func<TArguments, TResult> invokeChild,
                Func<TArguments, TResult, int>? countCandidateWork) => arguments =>
            {
                var result = invokeChild(arguments);
                counter.Calls++;
                return result;
            };
        }
    }
}
