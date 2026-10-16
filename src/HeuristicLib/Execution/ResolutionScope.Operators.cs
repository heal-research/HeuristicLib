using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Execution;

public sealed partial class ResolutionScope
{
    public IExecutorWithControls<TExecutor> ResolveOperator<TCandidate, TOperation, TSearchSpace, TProblem,
        TRunSearchState, TExecutor>(IOperator<TCandidate, TOperation> source,
        OperationExecutorAdapter<TCandidate, TOperation, TSearchSpace, TProblem, TRunSearchState> adapter)
        where TOperation : IOperationDescriptor<TOperation>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TRunSearchState : class, ISearchState
        where TExecutor : class, IExecutor
    {
        if (adapter.ExecutorType != typeof(TExecutor))
            throw new InvalidOperationException($"{typeof(TOperation)} requires {adapter.ExecutorType}, not {typeof(TExecutor)}.");
        if (!source.Fits(ExecutionSignature.For<TSearchSpace, TProblem, TRunSearchState>()))
            throw ExecutionSignature.Mismatch(source, "its declared signature",
                ExecutionSignature.Describe(typeof(TSearchSpace), typeof(TProblem), typeof(TRunSearchState)));
        if (!state.Constructing.Add(source))
            throw new InvalidOperationException($"Recursive construction of {source.GetType().Name} through {typeof(TOperation).Name}.");
        try
        {
            var (entry, owner) = state.GetOrAddOperatorOwner(source);
            OperatorExecutorSignature view = new OperatorExecutorSignature(typeof(TCandidate), typeof(TOperation), typeof(TSearchSpace),
                typeof(TProblem), typeof(TRunSearchState), typeof(TExecutor));
            if (!entry.Factories.TryGetValue(view, out var found))
            {
                // Failed binding/checking is retryable; successful owner preparation remains cached.
                found = entry.Factory.GetExecutorFactory(adapter).GetFor<TExecutor>();
                entry.Factories.Add(view, found);
            }

            var wrappers = new List<WrapperExecutorFactory<IExecutorWithControls<TExecutor>>>();
            foreach (var (pointcut, advice) in Advices())
                if (pointcut.Matches(source))
                {
                    var wrapper = GetOperatorAdviceFactory<TCandidate, TOperation, TSearchSpace, TProblem,
                        TRunSearchState, TExecutor>(entry, advice, source, adapter, view);
                    if (wrapper is not null)
                        wrappers.Add(wrapper);
                }

            var factory = (ExecutorFactory<IExecutorWithControls<TExecutor>>)found;
            var executor = factory(WithState(owner));
            foreach (var wrapper in wrappers)
                executor = wrapper(executor);
            return executor;
        }
        finally { state.Constructing.Remove(source); }
    }

    private static WrapperExecutorFactory<IExecutorWithControls<TExecutor>>? GetOperatorAdviceFactory<TCandidate,
        TOperation, TSearchSpace, TProblem, TRunSearchState, TExecutor>(OperatorResolutionEntry owner, Advice advice,
        IOperator<TCandidate, TOperation> source,
        OperationExecutorAdapter<TCandidate, TOperation, TSearchSpace, TProblem, TRunSearchState> adapter, OperatorExecutorSignature view)
        where TOperation : IOperationDescriptor<TOperation>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TRunSearchState : class, ISearchState
        where TExecutor : class, IExecutor
    {
        OperatorAdviceResolutionEntry prepared;
        WrapperExecutorFactory wrapper;
        if (advice is OperatorAdvice general)
        {
            prepared = owner.GetOrAddAdvice(advice, () => general.CreateWrapperFactory(source));
            if (prepared.Wrappers.TryGetValue(view, out var cached))
                return (WrapperExecutorFactory<IExecutorWithControls<TExecutor>>)cached;
            wrapper = ((OperatorAdviceWrapperFactory)prepared.WrapperFactory).GetWrapperFactory(source, adapter);
        }
        else if (advice is OperatorAdvice<TCandidate, TOperation> typed)
        {
            prepared = owner.GetOrAddAdvice(advice, () => typed.CreateWrapperFactory(source));
            if (prepared.Wrappers.TryGetValue(view, out var cached))
                return (WrapperExecutorFactory<IExecutorWithControls<TExecutor>>)cached;
            wrapper = ((OperatorAdviceWrapperFactory<TCandidate, TOperation>)prepared.WrapperFactory).GetWrapperFactory(source, adapter);
        }
        else if (advice is ITypedOperatorAdvice)
            return null;
        else
            throw new InvalidOperationException($"{advice.GetType().FullName} cannot bind operation {typeof(TOperation).Name} " +
                $"on {source.GetType().Name}. Supported extension bases are OperatorAdvice and OperatorAdvice<TCandidate, TOperation>.");

        // Check before caching or invoking the raw factory. Failed view binding preserves prepared advice state.
        var constructor = wrapper.GetFor<TExecutor>();
        prepared.Wrappers.Add(view, constructor);
        return constructor;
    }

    internal readonly record struct OperatorExecutorSignature(Type Candidate, Type Operation, Type SearchSpace,
        Type Problem, Type SearchState, Type ExecutorType);

    internal sealed class OperatorResolutionEntry(OperatorExecutorFactory prepared)
    {
        private readonly ConditionalWeakTable<Advice, OperatorAdviceResolutionEntry> advices = new();
        public OperatorExecutorFactory Factory { get; } = prepared;
        public Dictionary<OperatorExecutorSignature, Delegate> Factories { get; } = new();

        public OperatorAdviceResolutionEntry GetOrAddAdvice(Advice advice, Func<object> prepare)
        {
            if (advices.TryGetValue(advice, out var found))
                return found;
            OperatorAdviceResolutionEntry created = new OperatorAdviceResolutionEntry(prepare());
            advices.Add(advice, created);
            return created;
        }
    }

    internal sealed class OperatorAdviceResolutionEntry(object prepared)
    {
        // General and typed preparation are checked at dispatch, outside operation invocation.
        public object WrapperFactory { get; } = prepared;
        public Dictionary<OperatorExecutorSignature, Delegate> Wrappers { get; } = new();
    }

    internal sealed partial class State
    {
        internal (OperatorResolutionEntry Entry, State Owner) GetOrAddOperatorOwner<TCandidate, TOperation>(
            IOperator<TCandidate, TOperation> source) where TOperation : IOperationDescriptor<TOperation>
        {
            var selected = FindOwner(source);
            if (selected is { } existing)
                return (existing.Entry.Operator ?? throw PreparationPathMismatch(source), existing.Owner);
            OperatorResolutionEntry created = new OperatorResolutionEntry(source.CreateExecutorFactory());
            owners.Add(source, new ConfigurationResolutionEntry(created));
            return (created, this);
        }
    }
}
