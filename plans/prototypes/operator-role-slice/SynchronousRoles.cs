using HEAL.HeuristicLib.Operators;

namespace RoleSlice;

public sealed class MutationRole : ISynchronousRole<MutationRole>, ICandidateCountingRole
{
    public static RoleBinding<TCandidate, MutationRole, TSearchSpace, TProblem> CreateBinding<TCandidate, TSearchSpace, TProblem>()
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> => new MutationBinding<TCandidate, TSearchSpace, TProblem>();
}
public sealed class MutationBinding<TCandidate, TSearchSpace, TProblem> : SynchronousBinding<TCandidate, MutationRole, TSearchSpace, TProblem, IMutatorExecution<TCandidate, TSearchSpace, TProblem>>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected override IMutatorExecution<TCandidate, TSearchSpace, TProblem> CreatePolicyExecution(ImmutableArray<IMutatorExecution<TCandidate, TSearchSpace, TProblem>> children, SynchronousPolicy policy) => new PolicyExecution(children, policy);
    protected override IMutatorExecution<TCandidate, TSearchSpace, TProblem> CreateHookExecution<THooks, TToken>(IMutatorExecution<TCandidate, TSearchSpace, TProblem> inner, THooks hooks) => new HookExecution<THooks, TToken>(inner, hooks);
    protected override IMutatorExecution<TCandidate, TSearchSpace, TProblem> CreateSuccessExecution<THooks>(IMutatorExecution<TCandidate, TSearchSpace, TProblem> inner, THooks hook) => new SuccessExecution<THooks>(inner, hook);
    private sealed class SuccessExecution<THooks>(IMutatorExecution<TCandidate, TSearchSpace, TProblem> inner, THooks hook) : IMutatorExecution<TCandidate, TSearchSpace, TProblem> where THooks : struct, ISuccessHook
    {
        private THooks callback = hook;
        public IReadOnlyList<TCandidate> Mutate(IReadOnlyList<TCandidate> items, IRandomNumberGenerator random, TSearchSpace space, TProblem problem)
        {
            var result = inner.Mutate(items, random, space, problem);
            callback.OnSuccess(result.Count);
            return result;
        }
    }
    private sealed class PolicyExecution(ImmutableArray<IMutatorExecution<TCandidate, TSearchSpace, TProblem>> children, SynchronousPolicy policy) : IMutatorExecution<TCandidate, TSearchSpace, TProblem>
    {
        private static readonly Func<IMutatorExecution<TCandidate, TSearchSpace, TProblem>, (IReadOnlyList<TCandidate> Items, IRandomNumberGenerator Random, TSearchSpace Space, TProblem Problem), CallResult<IReadOnlyList<TCandidate>>> Next = static (inner, args) =>
        {
            var result = inner.Mutate(args.Items, args.Random, args.Space, args.Problem);
            return new(result, result.Count);
        };
        public IReadOnlyList<TCandidate> Mutate(IReadOnlyList<TCandidate> items, IRandomNumberGenerator random, TSearchSpace space, TProblem problem) =>
            policy.Execute(children, (items, random, space, problem), random, Next).Value;
    }
    private sealed class HookExecution<THooks, TToken>(IMutatorExecution<TCandidate, TSearchSpace, TProblem> inner, THooks hooks) : IMutatorExecution<TCandidate, TSearchSpace, TProblem> where THooks : struct, ICallHooks<TToken>
    {
        private THooks callbacks = hooks;
        public IReadOnlyList<TCandidate> Mutate(IReadOnlyList<TCandidate> items, IRandomNumberGenerator random, TSearchSpace space, TProblem problem)
        {
            var token = callbacks.Enter();
            try
            {
                var result = inner.Mutate(items, random, space, problem);
                callbacks.Success(ref token, result.Count);
                return result;
            }
            catch (Exception error) { callbacks.Fault(ref token, error); throw; }
            finally { callbacks.Finally(ref token); }
        }
    }
}

public sealed class EvaluationRole : ISynchronousRole<EvaluationRole>, ICandidateCountingRole
{
    public static RoleBinding<TCandidate, EvaluationRole, TSearchSpace, TProblem> CreateBinding<TCandidate, TSearchSpace, TProblem>()
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> => new EvaluationBinding<TCandidate, TSearchSpace, TProblem>();
}
public sealed class EvaluationBinding<TCandidate, TSearchSpace, TProblem> : SynchronousBinding<TCandidate, EvaluationRole, TSearchSpace, TProblem, IEvaluatorExecution<TCandidate, TSearchSpace, TProblem>>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected override IEvaluatorExecution<TCandidate, TSearchSpace, TProblem> CreatePolicyExecution(ImmutableArray<IEvaluatorExecution<TCandidate, TSearchSpace, TProblem>> children, SynchronousPolicy policy) => new PolicyExecution(children, policy);
    protected override IEvaluatorExecution<TCandidate, TSearchSpace, TProblem> CreateHookExecution<THooks, TToken>(IEvaluatorExecution<TCandidate, TSearchSpace, TProblem> inner, THooks hooks) => new HookExecution<THooks, TToken>(inner, hooks);
    protected override IEvaluatorExecution<TCandidate, TSearchSpace, TProblem> CreateSuccessExecution<THooks>(IEvaluatorExecution<TCandidate, TSearchSpace, TProblem> inner, THooks hook) => new SuccessExecution<THooks>(inner, hook);
    private sealed class SuccessExecution<THooks>(IEvaluatorExecution<TCandidate, TSearchSpace, TProblem> inner, THooks hook) : IEvaluatorExecution<TCandidate, TSearchSpace, TProblem> where THooks : struct, ISuccessHook
    {
        private THooks callback = hook;
        public IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> items, IRandomNumberGenerator random, TSearchSpace space, TProblem problem)
        {
            var result = inner.Evaluate(items, random, space, problem);
            callback.OnSuccess(items.Count);
            return result;
        }
    }
    private sealed class PolicyExecution(ImmutableArray<IEvaluatorExecution<TCandidate, TSearchSpace, TProblem>> children, SynchronousPolicy policy) : IEvaluatorExecution<TCandidate, TSearchSpace, TProblem>
    {
        private static readonly Func<IEvaluatorExecution<TCandidate, TSearchSpace, TProblem>, (IReadOnlyList<TCandidate> Items, IRandomNumberGenerator Random, TSearchSpace Space, TProblem Problem), CallResult<IReadOnlyList<ObjectiveVector>>> Next = static (inner, args) =>
        {
            var result = inner.Evaluate(args.Items, args.Random, args.Space, args.Problem);
            return new(result, args.Items.Count);
        };
        public IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> items, IRandomNumberGenerator random, TSearchSpace space, TProblem problem) =>
            policy.Execute(children, (items, random, space, problem), random, Next).Value;
    }
    private sealed class HookExecution<THooks, TToken>(IEvaluatorExecution<TCandidate, TSearchSpace, TProblem> inner, THooks hooks) : IEvaluatorExecution<TCandidate, TSearchSpace, TProblem> where THooks : struct, ICallHooks<TToken>
    {
        private THooks callbacks = hooks;
        public IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> items, IRandomNumberGenerator random, TSearchSpace space, TProblem problem)
        {
            var token = callbacks.Enter();
            try
            {
                var result = inner.Evaluate(items, random, space, problem);
                callbacks.Success(ref token, items.Count);
                return result;
            }
            catch (Exception error) { callbacks.Fault(ref token, error); throw; }
            finally { callbacks.Finally(ref token); }
        }
    }
}

public sealed class CreationRole : ISynchronousRole<CreationRole>, ICandidateCountingRole
{
    public static RoleBinding<TCandidate, CreationRole, TSearchSpace, TProblem> CreateBinding<TCandidate, TSearchSpace, TProblem>()
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> => new CreationBinding<TCandidate, TSearchSpace, TProblem>();
}
public sealed class CreationBinding<TCandidate, TSearchSpace, TProblem> : SynchronousBinding<TCandidate, CreationRole, TSearchSpace, TProblem, ICreatorExecution<TCandidate, TSearchSpace, TProblem>>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected override ICreatorExecution<TCandidate, TSearchSpace, TProblem> CreatePolicyExecution(ImmutableArray<ICreatorExecution<TCandidate, TSearchSpace, TProblem>> children, SynchronousPolicy policy) => new PolicyExecution(children, policy);
    protected override ICreatorExecution<TCandidate, TSearchSpace, TProblem> CreateHookExecution<THooks, TToken>(ICreatorExecution<TCandidate, TSearchSpace, TProblem> inner, THooks hooks) => new HookExecution<THooks, TToken>(inner, hooks);
    protected override ICreatorExecution<TCandidate, TSearchSpace, TProblem> CreateSuccessExecution<THooks>(ICreatorExecution<TCandidate, TSearchSpace, TProblem> inner, THooks hook) => new SuccessExecution<THooks>(inner, hook);
    private sealed class SuccessExecution<THooks>(ICreatorExecution<TCandidate, TSearchSpace, TProblem> inner, THooks hook) : ICreatorExecution<TCandidate, TSearchSpace, TProblem> where THooks : struct, ISuccessHook
    {
        private THooks callback = hook;
        public IReadOnlyList<TCandidate> Create(int count, IRandomNumberGenerator random, TSearchSpace space, TProblem problem)
        {
            var result = inner.Create(count, random, space, problem);
            callback.OnSuccess(result.Count);
            return result;
        }
    }
    private sealed class PolicyExecution(ImmutableArray<ICreatorExecution<TCandidate, TSearchSpace, TProblem>> children, SynchronousPolicy policy) : ICreatorExecution<TCandidate, TSearchSpace, TProblem>
    {
        private static readonly Func<ICreatorExecution<TCandidate, TSearchSpace, TProblem>, (int Count, IRandomNumberGenerator Random, TSearchSpace Space, TProblem Problem), CallResult<IReadOnlyList<TCandidate>>> Next = static (inner, args) =>
        {
            var result = inner.Create(args.Count, args.Random, args.Space, args.Problem);
            return new(result, result.Count);
        };
        public IReadOnlyList<TCandidate> Create(int count, IRandomNumberGenerator random, TSearchSpace space, TProblem problem) =>
            policy.Execute(children, (count, random, space, problem), random, Next).Value;
    }
    private sealed class HookExecution<THooks, TToken>(ICreatorExecution<TCandidate, TSearchSpace, TProblem> inner, THooks hooks) : ICreatorExecution<TCandidate, TSearchSpace, TProblem> where THooks : struct, ICallHooks<TToken>
    {
        private THooks callbacks = hooks;
        public IReadOnlyList<TCandidate> Create(int count, IRandomNumberGenerator random, TSearchSpace space, TProblem problem)
        {
            var token = callbacks.Enter();
            try
            {
                var result = inner.Create(count, random, space, problem);
                callbacks.Success(ref token, result.Count);
                return result;
            }
            catch (Exception error) { callbacks.Fault(ref token, error); throw; }
            finally { callbacks.Finally(ref token); }
        }
    }
}

public sealed class CrossoverRole : ISynchronousRole<CrossoverRole>, ICandidateCountingRole
{
    public static RoleBinding<TCandidate, CrossoverRole, TSearchSpace, TProblem> CreateBinding<TCandidate, TSearchSpace, TProblem>()
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> => new CrossoverBinding<TCandidate, TSearchSpace, TProblem>();
}
public sealed class CrossoverBinding<TCandidate, TSearchSpace, TProblem> : SynchronousBinding<TCandidate, CrossoverRole, TSearchSpace, TProblem, ICrossoverExecution<TCandidate, TSearchSpace, TProblem>>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected override ICrossoverExecution<TCandidate, TSearchSpace, TProblem> CreatePolicyExecution(ImmutableArray<ICrossoverExecution<TCandidate, TSearchSpace, TProblem>> children, SynchronousPolicy policy) => new PolicyExecution(children, policy);
    protected override ICrossoverExecution<TCandidate, TSearchSpace, TProblem> CreateHookExecution<THooks, TToken>(ICrossoverExecution<TCandidate, TSearchSpace, TProblem> inner, THooks hooks) => new HookExecution<THooks, TToken>(inner, hooks);
    protected override ICrossoverExecution<TCandidate, TSearchSpace, TProblem> CreateSuccessExecution<THooks>(ICrossoverExecution<TCandidate, TSearchSpace, TProblem> inner, THooks hook) => new SuccessExecution<THooks>(inner, hook);
    private sealed class SuccessExecution<THooks>(ICrossoverExecution<TCandidate, TSearchSpace, TProblem> inner, THooks hook) : ICrossoverExecution<TCandidate, TSearchSpace, TProblem> where THooks : struct, ISuccessHook
    {
        private THooks callback = hook;
        public IReadOnlyList<TCandidate> Cross(IReadOnlyList<Parents<TCandidate>> parents, IRandomNumberGenerator random, TSearchSpace space, TProblem problem)
        {
            var result = inner.Cross(parents, random, space, problem);
            callback.OnSuccess(result.Count);
            return result;
        }
    }
    private sealed class PolicyExecution(ImmutableArray<ICrossoverExecution<TCandidate, TSearchSpace, TProblem>> children, SynchronousPolicy policy) : ICrossoverExecution<TCandidate, TSearchSpace, TProblem>
    {
        private static readonly Func<ICrossoverExecution<TCandidate, TSearchSpace, TProblem>, (IReadOnlyList<Parents<TCandidate>> Parents, IRandomNumberGenerator Random, TSearchSpace Space, TProblem Problem), CallResult<IReadOnlyList<TCandidate>>> Next = static (inner, args) =>
        {
            var result = inner.Cross(args.Parents, args.Random, args.Space, args.Problem);
            return new(result, result.Count);
        };
        public IReadOnlyList<TCandidate> Cross(IReadOnlyList<Parents<TCandidate>> parents, IRandomNumberGenerator random, TSearchSpace space, TProblem problem) =>
            policy.Execute(children, (parents, random, space, problem), random, Next).Value;
    }
    private sealed class HookExecution<THooks, TToken>(ICrossoverExecution<TCandidate, TSearchSpace, TProblem> inner, THooks hooks) : ICrossoverExecution<TCandidate, TSearchSpace, TProblem> where THooks : struct, ICallHooks<TToken>
    {
        private THooks callbacks = hooks;
        public IReadOnlyList<TCandidate> Cross(IReadOnlyList<Parents<TCandidate>> parents, IRandomNumberGenerator random, TSearchSpace space, TProblem problem)
        {
            var token = callbacks.Enter();
            try
            {
                var result = inner.Cross(parents, random, space, problem);
                callbacks.Success(ref token, result.Count);
                return result;
            }
            catch (Exception error) { callbacks.Fault(ref token, error); throw; }
            finally { callbacks.Finally(ref token); }
        }
    }
}

public sealed class SelectionRole : ISynchronousRole<SelectionRole>, ICandidateCountingRole
{
    public static RoleBinding<TCandidate, SelectionRole, TSearchSpace, TProblem> CreateBinding<TCandidate, TSearchSpace, TProblem>()
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> => new SelectionBinding<TCandidate, TSearchSpace, TProblem>();
}
public sealed class SelectionBinding<TCandidate, TSearchSpace, TProblem> : SynchronousBinding<TCandidate, SelectionRole, TSearchSpace, TProblem, ISelectorExecution<TCandidate, TSearchSpace, TProblem>>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected override ISelectorExecution<TCandidate, TSearchSpace, TProblem> CreatePolicyExecution(ImmutableArray<ISelectorExecution<TCandidate, TSearchSpace, TProblem>> children, SynchronousPolicy policy) => new PolicyExecution(children, policy);
    protected override ISelectorExecution<TCandidate, TSearchSpace, TProblem> CreateHookExecution<THooks, TToken>(ISelectorExecution<TCandidate, TSearchSpace, TProblem> inner, THooks hooks) => new HookExecution<THooks, TToken>(inner, hooks);
    protected override ISelectorExecution<TCandidate, TSearchSpace, TProblem> CreateSuccessExecution<THooks>(ISelectorExecution<TCandidate, TSearchSpace, TProblem> inner, THooks hook) => new SuccessExecution<THooks>(inner, hook);
    private sealed class SuccessExecution<THooks>(ISelectorExecution<TCandidate, TSearchSpace, TProblem> inner, THooks hook) : ISelectorExecution<TCandidate, TSearchSpace, TProblem> where THooks : struct, ISuccessHook
    {
        private THooks callback = hook;
        public IReadOnlyList<EvaluatedCandidate<TCandidate>> Select(IReadOnlyList<EvaluatedCandidate<TCandidate>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random, TSearchSpace space, TProblem problem)
        {
            var result = inner.Select(population, objective, count, random, space, problem);
            callback.OnSuccess(result.Count);
            return result;
        }
    }
    private sealed class PolicyExecution(ImmutableArray<ISelectorExecution<TCandidate, TSearchSpace, TProblem>> children, SynchronousPolicy policy) : ISelectorExecution<TCandidate, TSearchSpace, TProblem>
    {
        private static readonly Func<ISelectorExecution<TCandidate, TSearchSpace, TProblem>, (IReadOnlyList<EvaluatedCandidate<TCandidate>> Population, ObjectiveDirections Objective, int Count, IRandomNumberGenerator Random, TSearchSpace Space, TProblem Problem), CallResult<IReadOnlyList<EvaluatedCandidate<TCandidate>>>> Next = static (inner, args) =>
        {
            var result = inner.Select(args.Population, args.Objective, args.Count, args.Random, args.Space, args.Problem);
            return new(result, result.Count);
        };
        public IReadOnlyList<EvaluatedCandidate<TCandidate>> Select(IReadOnlyList<EvaluatedCandidate<TCandidate>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random, TSearchSpace space, TProblem problem) =>
            policy.Execute(children, (population, objective, count, random, space, problem), random, Next).Value;
    }
    private sealed class HookExecution<THooks, TToken>(ISelectorExecution<TCandidate, TSearchSpace, TProblem> inner, THooks hooks) : ISelectorExecution<TCandidate, TSearchSpace, TProblem> where THooks : struct, ICallHooks<TToken>
    {
        private THooks callbacks = hooks;
        public IReadOnlyList<EvaluatedCandidate<TCandidate>> Select(IReadOnlyList<EvaluatedCandidate<TCandidate>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random, TSearchSpace space, TProblem problem)
        {
            var token = callbacks.Enter();
            try
            {
                var result = inner.Select(population, objective, count, random, space, problem);
                callbacks.Success(ref token, result.Count);
                return result;
            }
            catch (Exception error) { callbacks.Fault(ref token, error); throw; }
            finally { callbacks.Finally(ref token); }
        }
    }
}

public readonly struct TypedScope<TCandidate, TSearchSpace, TProblem>(ResolutionScope scope)
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public ResolutionScope Scope { get; } = scope;
    public IMutatorExecution<TCandidate, TSearchSpace, TProblem> Resolve(IOperator<TCandidate, MutationRole> source) =>
        ((MutationBinding<TCandidate, TSearchSpace, TProblem>)BindingCache<TCandidate, MutationRole, TSearchSpace, TProblem>.Binding).Resolve(Scope, source).Operation;
    public IEvaluatorExecution<TCandidate, TSearchSpace, TProblem> Resolve(IOperator<TCandidate, EvaluationRole> source) =>
        ((EvaluationBinding<TCandidate, TSearchSpace, TProblem>)BindingCache<TCandidate, EvaluationRole, TSearchSpace, TProblem>.Binding).Resolve(Scope, source).Operation;
    public ICreatorExecution<TCandidate, TSearchSpace, TProblem> Resolve(IOperator<TCandidate, CreationRole> source) =>
        ((CreationBinding<TCandidate, TSearchSpace, TProblem>)BindingCache<TCandidate, CreationRole, TSearchSpace, TProblem>.Binding).Resolve(Scope, source).Operation;
    public ICrossoverExecution<TCandidate, TSearchSpace, TProblem> Resolve(IOperator<TCandidate, CrossoverRole> source) =>
        ((CrossoverBinding<TCandidate, TSearchSpace, TProblem>)BindingCache<TCandidate, CrossoverRole, TSearchSpace, TProblem>.Binding).Resolve(Scope, source).Operation;
    public ISelectorExecution<TCandidate, TSearchSpace, TProblem> Resolve(IOperator<TCandidate, SelectionRole> source) =>
        ((SelectionBinding<TCandidate, TSearchSpace, TProblem>)BindingCache<TCandidate, SelectionRole, TSearchSpace, TProblem>.Binding).Resolve(Scope, source).Operation;
    public HEAL.HeuristicLib.Operators.IMutatorExecution<TCandidate, TSearchSpace, TProblem> Resolve<TControl>(IOperator<TCandidate, MutationRole> source, out TControl? control) where TControl : class
    {
        var bound = ((MutationBinding<TCandidate, TSearchSpace, TProblem>)BindingCache<TCandidate, MutationRole, TSearchSpace, TProblem>.Binding).Resolve(Scope, source);
        control = bound.Controls.Get<TControl>();
        return bound.Operation;
    }
}

