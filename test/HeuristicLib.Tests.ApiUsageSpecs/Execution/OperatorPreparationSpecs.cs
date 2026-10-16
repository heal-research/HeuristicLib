using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Tests.ApiUsageSpecs.Execution;

public sealed class OperatorPreparationSpecs
{
    [Fact]
    public void ExternalOperationAndGenericTopologyParticipateInGeneralAdvice()
    {
        IOperator<RealVector, ReadValue> source = new ValueSource(42);
        var forwarded = new Forward<RealVector, ReadValue>(source);
        var counter = new Counter();
        var scope = ResolutionScope.Create(Aspect.Create(Pointcut.Reference(source), counter));

        var binding = ReadValue.CreateExecutorAdapter<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, ISearchState>();
        var bound = scope.ResolveOperator<RealVector, ReadValue, BoundedRealVectorSearchSpace, TestFunctionProblem,
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
        public static OperationExecutorAdapter<TCandidate, ReadValue, TSearchSpace, TProblem, TRunSearchState>
            CreateExecutorAdapter<TCandidate, TSearchSpace, TProblem, TRunSearchState>()
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace>
            where TRunSearchState : class, ISearchState => new ExecutorAdapter<TCandidate, TSearchSpace, TProblem, TRunSearchState>();

        private sealed class ExecutorAdapter<TCandidate, TSearchSpace, TProblem, TRunSearchState>
            : OperationExecutorAdapter<TCandidate, ReadValue, TSearchSpace, TProblem, TRunSearchState, IReadValueExecutor>
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace>
            where TRunSearchState : class, ISearchState
        {
            public override bool SupportsCandidateWork => false;

            protected override IReadValueExecutor CreateSuccessCallbackExecutor<TCallback>(IReadValueExecutor child, TCallback callback) =>
                new SuccessfulRead<TCallback>(child, callback);

            protected override IReadValueExecutor CreateCallCallbacksExecutor<TCallbacks, TCallData>(IReadValueExecutor child, TCallbacks callbacks) =>
                new LifecycleRead<TCallbacks, TCallData>(child, callbacks);
        }

        private sealed class SuccessfulRead<TCallback>(IReadValueExecutor child, TCallback callback) : IReadValueExecutor
            where TCallback : struct, IOperationSuccessCallback
        {
            public int Read()
            {
                var value = child.Read();
                callback.OnSucceeded(0);
                return value;
            }
        }

        private sealed class LifecycleRead<TCallbacks, TCallData>(IReadValueExecutor child, TCallbacks callbacks) : IReadValueExecutor
            where TCallbacks : struct, IOperationCallCallbacks<TCallData>
        {
            public int Read()
            {
                var call = new OperationCallNotifier<TCallbacks, TCallData>(callbacks);
                try
                {
                    int result;
                    try
                    { result = child.Read(); }
                    catch (Exception failure) { call.NotifyFailed(failure); throw; }
                    call.NotifySucceeded(0);
                    return result;
                }
                finally { call.Finish(); }
            }
        }
    }

    private sealed record ValueSource(int Value) : IOperator<RealVector, ReadValue>
    {
        public OperatorExecutorFactory CreateExecutorFactory() => OperatorExecutorFactory.FromFactory<IReadValueExecutor>(_ => new Executor(Value));

        private sealed class Executor(int value) : IReadValueExecutor, IValueControl
        {
            public int Value { get; set; } = value;
            public int Read() => Value;
        }
    }

    private sealed record Forward<TCandidate, TOperation>(IOperator<TCandidate, TOperation> ChildOperator)
        : IOperator<TCandidate, TOperation> where TOperation : IOperationDescriptor<TOperation>
    {
        public bool Fits(ExecutionSignature execution) => ChildOperator.Fits(execution);
        public IOperatorContract<TCandidate>? CandidateContract => ChildOperator.CandidateContract;
        public OperatorExecutorFactory CreateExecutorFactory() => new Factory(ChildOperator);

        private sealed class Factory(IOperator<TCandidate, TOperation> child) : OperatorExecutorFactory
        {
            public override ExecutorFactory GetExecutorFactory<TBoundCandidate, TBoundOperation, TSearchSpace, TProblem, TRunSearchState>(
                OperationExecutorAdapter<TBoundCandidate, TBoundOperation, TSearchSpace, TProblem, TRunSearchState> adapter) =>
                child is IOperator<TBoundCandidate, TBoundOperation> typed
                    ? adapter.Forward(typed)
                    : throw new InvalidOperationException("Incompatible forwarding view.");
        }
    }

    private sealed class Counter : OperatorAdvice
    {
        public int Calls { get; private set; }
        public override OperatorAdviceWrapperFactory CreateWrapperFactory(IOperator source) => new WrapperFactory(this);

        private sealed class WrapperFactory(Counter counter) : OperatorAdviceWrapperFactory
        {
            public override WrapperExecutorFactory GetWrapperFactory<TCandidate, TOperation, TSearchSpace, TProblem, TRunSearchState>(
                IOperator<TCandidate, TOperation> source,
                OperationExecutorAdapter<TCandidate, TOperation, TSearchSpace, TProblem, TRunSearchState> adapter) =>
                adapter.Success(new Hook(counter));
        }

        private readonly struct Hook(Counter counter) : IOperationSuccessCallback
        {
            public void OnSucceeded(int candidateWork) { counter.Calls++; }
        }
    }
}
