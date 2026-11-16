using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Execution;

/// <summary>Base for an operation's wrapping executor, which passes each call of the executor it wraps to an invocation wrapper.</summary>
/// <remarks>
/// An operation's <see cref="IOperation{TSelf}.Wrap{TCandidate, TSearchState, TWrapper}"/> returns
/// a sealed class that derives from this base and implements the operation's executor interface. Its named method, such
/// as Mutate, passes each call to <see cref="Wrapper"/> as the operation's invocation over <see cref="Executor"/>.
/// </remarks>
/// <param name="executor">The executor to wrap, which implements <typeparamref name="TExecutor"/>.</param>
/// <param name="wrapper">The wrapper that receives each call.</param>
public abstract class WrappingExecutor<TExecutor, TWrapper>(IExecutor executor, TWrapper wrapper)
    where TExecutor : class, IExecutor
    where TWrapper : struct, IInvocationWrapper
{
    /// <summary>The wrapped executor, typed as the operation's executor interface.</summary>
    protected TExecutor Executor { get; } = (TExecutor)executor;

    /// <summary>The wrapper that receives each call.</summary>
    protected TWrapper Wrapper { get; } = wrapper;
}
