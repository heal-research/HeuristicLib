namespace RoleSlice;

public interface IRole<TSelf> where TSelf : IRole<TSelf>
{
    static abstract RoleBinding<TCandidate, TSelf, TSearchSpace, TProblem> CreateBinding<TCandidate, TSearchSpace, TProblem>()
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>;
}
public interface ISynchronousRole<TSelf> : IRole<TSelf> where TSelf : ISynchronousRole<TSelf>;
public interface ICandidateCountingRole;

public interface IOperator<TCandidate, TRole> : IConfigurationNode where TRole : IRole<TRole>
{
    IOperatorContract<TCandidate>? CandidateContract => this as IOperatorContract<TCandidate>;
    PreparedFactory Prepare<TSearchSpace, TProblem>(RoleBinding<TCandidate, TRole, TSearchSpace, TProblem> binding)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>;
}

public interface IControls { TResult? Get<TResult>() where TResult : class; }
public sealed class RawControls(object execution) : IControls
{
    public TResult? Get<TResult>() where TResult : class => execution as TResult;
}
public sealed class NoControls : IControls
{
    public static NoControls Instance { get; } = new();
    private NoControls() { }
    public TResult? Get<TResult>() where TResult : class => null;
}

// Operation-free controls travel with the binding, independently of operation wrappers.
public sealed class BoundExecution<TExecution>(TExecution operation, IControls controls) : IExecutionNode where TExecution : class, IExecutionNode
{
    public TExecution Operation { get; } = operation;
    public IControls Controls { get; } = controls;
    public BoundExecution<TExecution> With(TExecution operation) => new(operation, Controls);
}

public sealed class PreparedFactory
{
    private readonly Delegate factory;
    private readonly bool isBound;
    private PreparedFactory(Delegate factory, bool isBound) { this.factory = factory; this.isBound = isBound; }
    public static PreparedFactory Raw<TExecution>(ExecutionFactory<TExecution> factory) where TExecution : class, IExecutionNode => new(factory, false);
    public static PreparedFactory Bound<TExecution>(ExecutionFactory<BoundExecution<TExecution>> factory) where TExecution : class, IExecutionNode => new(factory, true);
    public static int Extractions;
    public ExecutionFactory<BoundExecution<TExecution>> Require<TExecution>() where TExecution : class, IExecutionNode
    {
        Extractions++;
        if (isBound)
            return factory as ExecutionFactory<BoundExecution<TExecution>> ?? throw Mismatch<TExecution>();
        // Delegate covariance retains broad-to-specific execution adaptation.
        var raw = factory as ExecutionFactory<TExecution> ?? throw Mismatch<TExecution>();
        return scope => { var execution = raw(scope); return new(execution, new RawControls(execution)); };
    }
    private InvalidOperationException Mismatch<TExecution>() => new($"Factory {factory.GetType()} cannot supply {typeof(TExecution)}.");
}
public sealed class PreparedWrapper
{
    private readonly Delegate factory;
    private PreparedWrapper(Delegate factory) => this.factory = factory;
    public static PreparedWrapper From<TExecution>(WrapperExecutionFactory<BoundExecution<TExecution>> factory) where TExecution : class, IExecutionNode => new(factory);
    public WrapperExecutionFactory<BoundExecution<TExecution>> Require<TExecution>() where TExecution : class, IExecutionNode =>
        factory as WrapperExecutionFactory<BoundExecution<TExecution>> ?? throw new InvalidOperationException($"Advice cannot supply {typeof(TExecution)}.");
}
public sealed class PreparedComposite
{
    private readonly Delegate factory;
    private PreparedComposite(Delegate factory) => this.factory = factory;
    public static PreparedComposite From<TExecution>(Func<ImmutableArray<BoundExecution<TExecution>>, BoundExecution<TExecution>> factory) where TExecution : class, IExecutionNode => new(factory);
    public Func<ImmutableArray<BoundExecution<TExecution>>, BoundExecution<TExecution>> Require<TExecution>() where TExecution : class, IExecutionNode =>
        factory as Func<ImmutableArray<BoundExecution<TExecution>>, BoundExecution<TExecution>> ?? throw new InvalidOperationException($"Composition cannot supply {typeof(TExecution)}.");
}

public abstract class OperatorAdvice<TCandidate, TRole> : Advice<IOperator<TCandidate, TRole>> where TRole : IRole<TRole>
{
    public abstract PreparedWrapper Prepare<TSearchSpace, TProblem>(IOperator<TCandidate, TRole> source, RoleBinding<TCandidate, TRole, TSearchSpace, TProblem> binding)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>;
}
public abstract class RoleBinding<TCandidate, TRole, TSearchSpace, TProblem>
    where TRole : IRole<TRole>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public abstract Type ExecutionType { get; }
    public abstract PreparedFactory Forward(IOperator<TCandidate, TRole> child);
    public abstract PreparedWrapper Identity();
    public abstract PreparedFactory Wrap(IOperator<TCandidate, TRole> child, PreparedWrapper wrapper);
    public abstract PreparedFactory Compose(IReadOnlyList<IOperator<TCandidate, TRole>> children, PreparedComposite composite);
}
public abstract class ExecutionBinding<TCandidate, TRole, TSearchSpace, TProblem, TExecution> : RoleBinding<TCandidate, TRole, TSearchSpace, TProblem>
    where TRole : IRole<TRole>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TExecution : class, IExecutionNode
{
    public override Type ExecutionType => typeof(TExecution);
    public BoundExecution<TExecution> Resolve(ResolutionScope scope, IOperator<TCandidate, TRole> source) => scope.Resolve(source,
        configuration => configuration.Prepare(this).Require<TExecution>(),
        (advice, configuration) => ((OperatorAdvice<TCandidate, TRole>)advice).Prepare(configuration, this).Require<TExecution>());
    public override PreparedFactory Forward(IOperator<TCandidate, TRole> child) => PreparedFactory.Bound<TExecution>(scope => Resolve(scope, child));
    public override PreparedWrapper Identity() => PreparedWrapper.From<TExecution>(child => child);
    public override PreparedFactory Wrap(IOperator<TCandidate, TRole> child, PreparedWrapper wrapper)
    {
        var create = wrapper.Require<TExecution>();
        return PreparedFactory.Bound<TExecution>(scope => create(Resolve(scope, child)));
    }
    public override PreparedFactory Compose(IReadOnlyList<IOperator<TCandidate, TRole>> children, PreparedComposite composite)
    {
        var create = composite.Require<TExecution>();
        return PreparedFactory.Bound<TExecution>(scope => create([.. children.Select(child => Resolve(scope, child))]));
    }
}
public static class BindingCache<TCandidate, TRole, TSearchSpace, TProblem>
    where TRole : IRole<TRole>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    // Dispatch follows the role's declaration, not a registry or a built-in role list.
    public static RoleBinding<TCandidate, TRole, TSearchSpace, TProblem> Binding { get; } = TRole.CreateBinding<TCandidate, TSearchSpace, TProblem>();
}

public abstract record BoundOperator<TCandidate, TRole, TSearchSpace, TProblem, TExecution> : OperatorConfiguration, IOperator<TCandidate, TRole>
    where TRole : IRole<TRole>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TExecution : class, IExecutionNode
{
    public abstract ExecutionFactory<TExecution> CreateExecutionFactory();
    public bool Fits(ExecutionSignature signature) => signature.SearchSpace.IsAssignableTo(typeof(TSearchSpace)) && signature.Problem.IsAssignableTo(typeof(TProblem));
    public PreparedFactory Prepare<TRunSearchSpace, TRunProblem>(RoleBinding<TCandidate, TRole, TRunSearchSpace, TRunProblem> binding)
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>
    {
        if (!typeof(TRunSearchSpace).IsAssignableTo(typeof(TSearchSpace)) || !typeof(TRunProblem).IsAssignableTo(typeof(TProblem)) || !typeof(TExecution).IsAssignableTo(binding.ExecutionType))
            throw new InvalidOperationException($"{GetType().Name} cannot bind {typeof(TRunSearchSpace).Name}/{typeof(TRunProblem).Name} as {binding.ExecutionType}.");
        return PreparedFactory.Raw(CreateExecutionFactory());
    }
}

public readonly record struct CallResult<TResult>(TResult Value, int CandidateCount);
public abstract class SynchronousPolicy
{
    public abstract CallResult<TResult> Execute<TExecution, TArguments, TResult>(ImmutableArray<TExecution> children, TArguments arguments, IRandomNumberGenerator random, Func<TExecution, TArguments, CallResult<TResult>> next);
}
public interface ICallHooks<TToken>
{
    TToken Enter();
    void Success(ref TToken token, int candidateCount);
    void Fault(ref TToken token, Exception exception);
    void Finally(ref TToken token);
}
public readonly struct Unit;
public interface ISuccessHook { void OnSuccess(int candidateCount); }
public interface ISynchronousBinding<TCandidate, TRole, TSearchSpace, TProblem>
    where TRole : ISynchronousRole<TRole>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    PreparedWrapper PolicyWrapper(SynchronousPolicy policy);
    PreparedComposite PolicyComposite(SynchronousPolicy policy);
    PreparedWrapper Hooks<THooks, TToken>(THooks hooks) where THooks : struct, ICallHooks<TToken>;
    PreparedWrapper Success<THooks>(THooks hook) where THooks : struct, ISuccessHook;
}
public abstract class SynchronousBinding<TCandidate, TRole, TSearchSpace, TProblem, TExecution> : ExecutionBinding<TCandidate, TRole, TSearchSpace, TProblem, TExecution>, ISynchronousBinding<TCandidate, TRole, TSearchSpace, TProblem>
    where TRole : ISynchronousRole<TRole>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TExecution : class, IExecutionNode
{
    protected abstract TExecution CreatePolicyExecution(ImmutableArray<TExecution> children, SynchronousPolicy policy);
    protected abstract TExecution CreateHookExecution<THooks, TToken>(TExecution inner, THooks hooks) where THooks : struct, ICallHooks<TToken>;
    protected abstract TExecution CreateSuccessExecution<THooks>(TExecution inner, THooks hook) where THooks : struct, ISuccessHook;
    public PreparedWrapper PolicyWrapper(SynchronousPolicy policy) => PreparedWrapper.From<TExecution>(inner => inner.With(CreatePolicyExecution([inner.Operation], policy)));
    public PreparedComposite PolicyComposite(SynchronousPolicy policy) => PreparedComposite.From<TExecution>(children =>
        new(CreatePolicyExecution([.. children.Select(child => child.Operation)], policy), NoControls.Instance));
    public PreparedWrapper Hooks<THooks, TToken>(THooks hooks) where THooks : struct, ICallHooks<TToken> =>
        PreparedWrapper.From<TExecution>(inner => inner.With(CreateHookExecution<THooks, TToken>(inner.Operation, hooks)));
    public PreparedWrapper Success<THooks>(THooks hook) where THooks : struct, ISuccessHook =>
        PreparedWrapper.From<TExecution>(inner => inner.With(CreateSuccessExecution(inner.Operation, hook)));
}

public abstract record WrappingOperator<TCandidate, TRole> : OperatorConfiguration, IOperator<TCandidate, TRole> where TRole : IRole<TRole>
{
    protected WrappingOperator(IOperator<TCandidate, TRole> child) => ChildOperator = child;
    public IOperator<TCandidate, TRole> ChildOperator { get; init; }
    public virtual IOperatorContract<TCandidate>? CandidateContract => ChildOperator.CandidateContract;
    public bool Fits(ExecutionSignature signature) => ChildOperator.Fits(signature);
    public PreparedFactory Prepare<TSearchSpace, TProblem>(RoleBinding<TCandidate, TRole, TSearchSpace, TProblem> binding)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> => binding.Wrap(ChildOperator, CreateWrapperFactory(binding));
    protected abstract PreparedWrapper CreateWrapperFactory<TSearchSpace, TProblem>(RoleBinding<TCandidate, TRole, TSearchSpace, TProblem> binding)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>;
}
public abstract record MultiOperator<TCandidate, TRole> : OperatorConfiguration, IOperator<TCandidate, TRole> where TRole : IRole<TRole>
{
    protected MultiOperator(IReadOnlyList<IOperator<TCandidate, TRole>> children) => ChildOperators = children.ToValueArray();
    public ValueArray<IOperator<TCandidate, TRole>> ChildOperators { get; init; }
    public virtual IOperatorContract<TCandidate>? CandidateContract => ChildOperators.Any(child => child.CandidateContract is not null)
        ? new CompositeCandidateContract<TCandidate>([.. ChildOperators.Select(child => child.CandidateContract)]) : null;
    public bool Fits(ExecutionSignature signature) => signature.Fits([.. ChildOperators]);
    public PreparedFactory Prepare<TSearchSpace, TProblem>(RoleBinding<TCandidate, TRole, TSearchSpace, TProblem> binding)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> => binding.Compose(ChildOperators, CreateCompositeFactory(binding));
    protected abstract PreparedComposite CreateCompositeFactory<TSearchSpace, TProblem>(RoleBinding<TCandidate, TRole, TSearchSpace, TProblem> binding)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>;
}
public sealed class ContractProjection<TCandidate>(IOperatorContract<TCandidate>? contract) : HEAL.HeuristicLib.Operators.IOperator, IOperatorContract<TCandidate>
{
    public bool? Ensures(ICandidateInvariant<TCandidate> invariant) => contract?.Ensures(invariant);
    public IReadOnlyList<ICandidateInvariant<TCandidate>> Requires => contract?.Requires ?? [];
}
public sealed class CompositeCandidateContract<TCandidate>(IReadOnlyList<IOperatorContract<TCandidate>?> children) : IOperatorContract<TCandidate>
{
    public bool? Ensures(ICandidateInvariant<TCandidate> invariant) => OperatorContractComposition.Ensures(children.Select(child => new ContractProjection<TCandidate>(child)), invariant);
    public IReadOnlyList<ICandidateInvariant<TCandidate>> Requires => OperatorContractComposition.Requires<TCandidate>(children.Select(child => new ContractProjection<TCandidate>(child)));
}
public static class CandidateCompatibility
{
    public static IReadOnlyList<Incompatibility> Check<TCandidate, TRole>(IOperator<TCandidate, TRole> source, ISearchSpace<TCandidate> space) where TRole : IRole<TRole> =>
        SearchSpaceCompatibility.Check(new ContractProjection<TCandidate>(source.CandidateContract), space);
}
public sealed record Named<TCandidate, TRole> : WrappingOperator<TCandidate, TRole>, INamedConfiguration where TRole : IRole<TRole>
{
    public Named(IOperator<TCandidate, TRole> child, string name) : base(child) => Name = name;
    public string Name { get; init; }
    protected override PreparedWrapper CreateWrapperFactory<TSearchSpace, TProblem>(RoleBinding<TCandidate, TRole, TSearchSpace, TProblem> binding) => binding.Identity();
}
public sealed class RetryableFailure : Exception;
public sealed record Retry<TCandidate, TRole> : WrappingOperator<TCandidate, TRole> where TRole : ISynchronousRole<TRole>
{
    public Retry(IOperator<TCandidate, TRole> child, int attempts) : base(child) => Attempts = attempts;
    public int Attempts { get; init; }
    protected override PreparedWrapper CreateWrapperFactory<TSearchSpace, TProblem>(RoleBinding<TCandidate, TRole, TSearchSpace, TProblem> binding)
    {
        if (Attempts < 1)
            throw new InvalidOperationException("Attempts must be positive.");
        return ((ISynchronousBinding<TCandidate, TRole, TSearchSpace, TProblem>)binding).PolicyWrapper(new RetryPolicy(Attempts));
    }
    private sealed class RetryPolicy(int attempts) : SynchronousPolicy
    {
        public override CallResult<TResult> Execute<TExecution, TArguments, TResult>(ImmutableArray<TExecution> children, TArguments arguments, IRandomNumberGenerator random, Func<TExecution, TArguments, CallResult<TResult>> next)
        {
            for (var attempt = 1; ; attempt++)
            {
                try
                { return next(children[0], arguments); }
                catch (RetryableFailure) when (attempt < attempts) { }
            }
        }
    }
}
public sealed record ChooseCall<TCandidate, TRole> : MultiOperator<TCandidate, TRole> where TRole : ISynchronousRole<TRole>
{
    public ChooseCall(IReadOnlyList<IOperator<TCandidate, TRole>> children) : base(children) { }
    protected override PreparedComposite CreateCompositeFactory<TSearchSpace, TProblem>(RoleBinding<TCandidate, TRole, TSearchSpace, TProblem> binding)
    {
        if (ChildOperators.Count == 0)
            throw new InvalidOperationException("At least one child is required.");
        return ((ISynchronousBinding<TCandidate, TRole, TSearchSpace, TProblem>)binding).PolicyComposite(new ChoicePolicy());
    }
    private sealed class ChoicePolicy : SynchronousPolicy
    {
        public override CallResult<TResult> Execute<TExecution, TArguments, TResult>(ImmutableArray<TExecution> children, TArguments arguments, IRandomNumberGenerator random, Func<TExecution, TArguments, CallResult<TResult>> next) =>
            next(children[Math.Min((int)(random.NextDouble() * children.Length), children.Length - 1)], arguments);
    }
}
public static class Composition
{
    public static Named<TCandidate, TRole> Named<TCandidate, TRole>(this IOperator<TCandidate, TRole> child, string name) where TRole : IRole<TRole> => new(child, name);
    public static Retry<TCandidate, TRole> Retry<TCandidate, TRole>(this IOperator<TCandidate, TRole> child, int attempts) where TRole : ISynchronousRole<TRole> => new(child, attempts);
    public static ChooseCall<TCandidate, TRole> OrCall<TCandidate, TRole>(this IOperator<TCandidate, TRole> first, IOperator<TCandidate, TRole> second) where TRole : ISynchronousRole<TRole> => new([first, second]);
}
