using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Execution;

public interface IControls
{
    TControl? Get<TControl>() where TControl : class;
}

public sealed class ExecutionControls(object target) : IControls
{
    public TControl? Get<TControl>() where TControl : class => target as TControl;
}

public interface IBoundExecution<out TExecution> : IExecutionNode
    where TExecution : class, IExecutionNode
{
    TExecution Operation { get; }
    IControls Controls { get; }
}

public sealed class BoundExecution<TExecution>(TExecution operation, IControls controls) : IBoundExecution<TExecution>
    where TExecution : class, IExecutionNode
{
    public TExecution Operation { get; } = operation;
    public IControls Controls { get; } = controls;
}

public sealed class PreparedFactory
{
    private readonly Delegate factory;

    private PreparedFactory(Delegate factory) => this.factory = factory;

    public static PreparedFactory Raw<TExecution>(ExecutionFactory<TExecution> factory)
        where TExecution : class, IExecutionNode =>
        Bound<TExecution>(scope =>
        {
            var operation = factory(scope);
            return new BoundExecution<TExecution>(operation, new ExecutionControls(operation));
        });

    public static PreparedFactory Bound<TExecution>(ExecutionFactory<IBoundExecution<TExecution>> factory)
        where TExecution : class, IExecutionNode => new(factory);

    // Preserve the authored delegate's covariance; no requested-type closure is introduced here.
    public ExecutionFactory<IBoundExecution<TExecution>> Require<TExecution>()
        where TExecution : class, IExecutionNode =>
        factory as ExecutionFactory<IBoundExecution<TExecution>> ??
        throw new InvalidOperationException($"Prepared factory {factory.GetType()} cannot provide {typeof(TExecution)}.");
}

public sealed class PreparedWrapper
{
    private readonly Delegate wrapper;

    private PreparedWrapper(Delegate wrapper) => this.wrapper = wrapper;

    public static PreparedWrapper From<TExecution>(WrapperExecutionFactory<IBoundExecution<TExecution>> wrapper)
        where TExecution : class, IExecutionNode => new(wrapper);

    public WrapperExecutionFactory<IBoundExecution<TExecution>> Require<TExecution>()
        where TExecution : class, IExecutionNode =>
        wrapper as WrapperExecutionFactory<IBoundExecution<TExecution>> ??
        throw new InvalidOperationException($"Prepared wrapper {wrapper.GetType()} cannot wrap {typeof(TExecution)}.");
}

public abstract class PreparedOperator
{
    public abstract PreparedFactory Bind<TCandidate, TOperation, TSearchSpace, TProblem, TRunSearchState>(
        OperationBinding<TCandidate, TOperation, TSearchSpace, TProblem, TRunSearchState> binding)
        where TOperation : IOperation<TOperation>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TRunSearchState : class, ISearchState;

    public static PreparedOperator Raw<TExecution>(ExecutionFactory<TExecution> factory)
        where TExecution : class, IExecutionNode => new RawPrepared(PreparedFactory.Raw(factory));

    private sealed class RawPrepared(PreparedFactory factory) : PreparedOperator
    {
        public override PreparedFactory Bind<TCandidate, TOperation, TSearchSpace, TProblem, TRunSearchState>(
            OperationBinding<TCandidate, TOperation, TSearchSpace, TProblem, TRunSearchState> binding) => factory;
    }
}

public abstract class OperationBinding<TCandidate, TOperation, TSearchSpace, TProblem, TRunSearchState>
    where TOperation : IOperation<TOperation>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TRunSearchState : class, ISearchState
{
    public abstract Type ExecutionType { get; }
    public abstract bool SupportsCandidateWork { get; }
    public abstract PreparedWrapper Identity();
    public abstract PreparedFactory Wrap(IOperator<TCandidate, TOperation> child, PreparedWrapper wrapper);
    public abstract PreparedWrapper Success<THook>(THook hook) where THook : struct, ISuccessHook;
    public abstract PreparedWrapper Hooks<THooks, TToken>(THooks hooks) where THooks : struct, ICallHooks<TToken>;

    public PreparedFactory Forward(IOperator<TCandidate, TOperation> child) => Wrap(child, Identity());
}

public abstract class OperationBinding<TCandidate, TOperation, TSearchSpace, TProblem, TRunSearchState, TExecution>
    : OperationBinding<TCandidate, TOperation, TSearchSpace, TProblem, TRunSearchState>
    where TOperation : IOperation<TOperation>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TRunSearchState : class, ISearchState
    where TExecution : class, IExecutionNode
{
    public sealed override Type ExecutionType => typeof(TExecution);

    public sealed override PreparedWrapper Identity() => PreparedWrapper.From<TExecution>(static child => child);

    public sealed override PreparedWrapper Success<THook>(THook hook) =>
        PreparedWrapper.From<TExecution>(child =>
            new BoundExecution<TExecution>(CreateSuccessExecution(child.Operation, hook), child.Controls));

    protected abstract TExecution CreateSuccessExecution<THook>(TExecution child, THook hook)
        where THook : struct, ISuccessHook;

    public sealed override PreparedWrapper Hooks<THooks, TToken>(THooks hooks) =>
        PreparedWrapper.From<TExecution>(child =>
            new BoundExecution<TExecution>(CreateHooksExecution<THooks, TToken>(child.Operation, hooks), child.Controls));

    protected abstract TExecution CreateHooksExecution<THooks, TToken>(TExecution child, THooks hooks)
        where THooks : struct, ICallHooks<TToken>;

    public sealed override PreparedFactory Wrap(IOperator<TCandidate, TOperation> child, PreparedWrapper wrapper)
    {
        var construct = wrapper.Require<TExecution>();
        return PreparedFactory.Bound<TExecution>(scope => construct(scope.ResolveOperator<TCandidate, TOperation,
            TSearchSpace, TProblem, TRunSearchState, TExecution>(child, this)));
    }
}
