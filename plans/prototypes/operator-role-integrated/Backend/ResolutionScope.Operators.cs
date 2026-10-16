using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Execution;

// Compiled only beside a generated partial copy of the shipping ResolutionScope.
public sealed partial class ResolutionScope
{
    public IBoundExecution<TExecution> ResolveOperator<TCandidate, TOperation, TSearchSpace, TProblem,
        TRunSearchState, TExecution>(IOperator<TCandidate, TOperation> source,
        OperationBinding<TCandidate, TOperation, TSearchSpace, TProblem, TRunSearchState> binding)
        where TOperation : IOperation<TOperation>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TRunSearchState : class, ISearchState
        where TExecution : class, IExecutionNode
    {
        if (binding.ExecutionType != typeof(TExecution))
            throw new InvalidOperationException($"{typeof(TOperation)} requires {binding.ExecutionType}, not {typeof(TExecution)}.");
        if (!source.Fits(ExecutionSignature.For<TSearchSpace, TProblem, TRunSearchState>()))
            throw ExecutionSignature.Mismatch(source, "its declared signature",
                ExecutionSignature.Describe(typeof(TSearchSpace), typeof(TProblem), typeof(TRunSearchState)));
        if (!state.Constructing.Add(source))
            throw new InvalidOperationException($"Recursive construction of {source.GetType().Name} through {typeof(TOperation).Name}.");
        try
        {
            var (entry, owner) = state.GetOrAddOperatorOwner(source);
            var view = new OperatorView(typeof(TCandidate), typeof(TOperation), typeof(TSearchSpace),
                typeof(TProblem), typeof(TRunSearchState), typeof(TExecution));
            if (!entry.Factories.TryGetValue(view, out var found))
            {
                // Failed binding/checking is retryable; successful owner preparation remains cached.
                found = entry.Prepared.Bind(binding).Require<TExecution>();
                entry.Factories.Add(view, found);
            }

            var wrappers = new List<WrapperExecutionFactory<IBoundExecution<TExecution>>>();
            foreach (var (pointcut, advice) in Advices())
                if (pointcut.Matches(source))
                {
                    var wrapper = GetOperatorAdviceFactory<TCandidate, TOperation, TSearchSpace, TProblem,
                        TRunSearchState, TExecution>(entry, advice, source, binding, view);
                    if (wrapper is not null)
                        wrappers.Add(wrapper);
                }

            var factory = (ExecutionFactory<IBoundExecution<TExecution>>)found;
            var execution = factory(WithState(owner));
            foreach (var wrapper in wrappers)
                execution = wrapper(execution);
            return execution;
        }
        finally { state.Constructing.Remove(source); }
    }

    private static WrapperExecutionFactory<IBoundExecution<TExecution>>? GetOperatorAdviceFactory<TCandidate,
        TOperation, TSearchSpace, TProblem, TRunSearchState, TExecution>(OperatorOwner owner, Advice advice,
        IOperator<TCandidate, TOperation> source,
        OperationBinding<TCandidate, TOperation, TSearchSpace, TProblem, TRunSearchState> binding, OperatorView view)
        where TOperation : IOperation<TOperation>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TRunSearchState : class, ISearchState
        where TExecution : class, IExecutionNode
    {
        AdviceOwner prepared;
        PreparedWrapper wrapper;
        if (advice is OperatorAdvice general)
        {
            prepared = owner.GetOrAddAdvice(advice, () => general.Prepare(source));
            if (prepared.Wrappers.TryGetValue(view, out var cached))
                return (WrapperExecutionFactory<IBoundExecution<TExecution>>)cached;
            wrapper = ((PreparedOperatorAdvice)prepared.Prepared).Bind(source, binding);
        }
        else if (advice is OperatorAdvice<TCandidate, TOperation> typed)
        {
            prepared = owner.GetOrAddAdvice(advice, () => typed.Prepare(source));
            if (prepared.Wrappers.TryGetValue(view, out var cached))
                return (WrapperExecutionFactory<IBoundExecution<TExecution>>)cached;
            wrapper = ((PreparedOperatorAdvice<TCandidate, TOperation>)prepared.Prepared).Bind(source, binding);
        }
        else if (advice is ITypedOperatorAdvice)
            return null;
        else
            throw new InvalidOperationException($"{advice.GetType().FullName} cannot bind operation {typeof(TOperation).Name} " +
                $"on {source.GetType().Name}. Supported extension bases are OperatorAdvice and OperatorAdvice<TCandidate, TOperation>.");

        // Check before caching or invoking the raw factory. Failed view binding preserves prepared advice state.
        var constructor = wrapper.Require<TExecution>();
        prepared.Wrappers.Add(view, constructor);
        return constructor;
    }

    internal readonly record struct OperatorView(Type Candidate, Type Operation, Type SearchSpace,
        Type Problem, Type SearchState, Type Execution);

    internal sealed class OperatorOwner(PreparedOperator prepared)
    {
        private readonly ConditionalWeakTable<Advice, AdviceOwner> advices = new();
        public PreparedOperator Prepared { get; } = prepared;
        public Dictionary<OperatorView, Delegate> Factories { get; } = new();

        public AdviceOwner GetOrAddAdvice(Advice advice, Func<object> prepare)
        {
            if (advices.TryGetValue(advice, out var found))
                return found;
            var created = new AdviceOwner(prepare());
            advices.Add(advice, created);
            return created;
        }
    }

    internal sealed class AdviceOwner(object prepared)
    {
        // General and typed preparation are checked at dispatch, outside operation invocation.
        public object Prepared { get; } = prepared;
        public Dictionary<OperatorView, Delegate> Wrappers { get; } = new();
    }

    internal sealed partial class State
    {
        private readonly Dictionary<IOperator, OperatorOwner> operatorOwners = new(ReferenceEqualityComparer.Instance);

        internal (OperatorOwner Entry, State Owner) GetOrAddOperatorOwner<TCandidate, TOperation>(
            IOperator<TCandidate, TOperation> source) where TOperation : IOperation<TOperation>
        {
            for (var current = this; current is not null; current = current.parent)
                if (current.operatorOwners.TryGetValue(source, out var found))
                    return (found, current);
            var created = new OperatorOwner(source.Prepare());
            operatorOwners.Add(source, created);
            return (created, this);
        }
    }
}
