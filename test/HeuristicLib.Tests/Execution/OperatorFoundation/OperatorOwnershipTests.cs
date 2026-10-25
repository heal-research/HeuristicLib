using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.Tests.ExecutionInfrastructure.OperatorFoundation.Fixtures;

namespace HEAL.HeuristicLib.Tests.ExecutionInfrastructure.OperatorFoundation;

public sealed class OperatorOwnershipTests
{
    private static void Check(bool condition, string description) => Assert.True(condition, description);

    private static readonly TestFunctionProblem Problem = new(new SphereFunction(1));
    private static readonly RealVector[] Input = [new(2.0)];

    private static void Reject(Action action, string expected)
    {
        try
        {
            action();
        }
        catch (InvalidOperationException exception)
        {
            Check(exception.Message.Contains(expected, StringComparison.Ordinal), $"diagnostic includes '{expected}'");
            return;
        }
        throw new InvalidOperationException($"Expected rejection: {expected}");
    }

    private static ResolutionScope<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem> Typed(ResolutionScope scope) =>
        scope.For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>();

    private static double Invoke(IMutationExecutor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem> executor) =>
        executor.Mutate(Input, RandomNumberGenerator.Create(7), Problem.SearchSpace, Problem)[0][0];

    [Fact]
    public void PublicApi()
    {
        var typed = Typed(ResolutionScope.Create());
        IOperator<Mutation, RealVector> mutation = new IdentityMutation();
        IOperator<Evaluation, RealVector> evaluation = new SphereEvaluation();
        var named = mutation.Named("inner").Named("outer");
        Check(ReferenceEquals(typed.Resolve(mutation), typed.Resolve(named)), "names add no callable node");
        Check(ReferenceEquals(Input, typed.Resolve(named).Mutate(Input, RandomNumberGenerator.Create(1), Problem.SearchSpace, Problem)),
            "stateless author overrides only its domain method");
        Check(typed.Resolve(evaluation).Evaluate(Input, RandomNumberGenerator.Create(1), Problem.SearchSpace, Problem).Count == 1,
            "evaluation keeps objective-vector results");
        Check(Pointcut.Named<IOperator<Mutation, RealVector>>("outer").Matches(named), "common pointcuts recognize generic names");
        Check(!Pointcut.Reference(mutation).Matches(named), "wrapper and child remain separate configurations");
        IOperator<Mutation, RealVector> counting = new CountingMutation();
        Check(Invoke(typed.Resolve(counting)) == 1 && Invoke(typed.Resolve(counting.Named("count"))) == 2,
            "stateful author overrides CreateScopedState and domain method only");
        Check(typed.ResolveOptional((IOperator<Mutation, RealVector>?)null) is null, "optional resolution infers its operation");
        Check(typed.TryResolve(mutation, out var resolved, out var reason) && resolved is not null && reason is null,
            "TryResolve needs no explicit executor type");

        IOperator<Checksum, RealVector> checksum = new SharedOperations(new ProbeLog());
        Check(typed.Resolve(new Forward<Checksum, RealVector>(checksum).Named("external")).Read(RandomNumberGenerator.Create(1), Problem.SearchSpace, Problem) == 3,
            "external operation and topology use public APIs across the assembly boundary");
        var recommendation = new ChecksumRecommendation();
        Check(recommendation.TryCreateRecommendedOperator(out var first) &&
            recommendation.TryCreateRecommendedOperator(out var second) && !ReferenceEquals(first, second),
            "common marker accepts an external operation recommendation with fresh configurations");
    }

    [Fact]
    public void MultipleViews()
    {
        foreach (var evaluationFirst in new[] { false, true })
        {
            var log = new ProbeLog();
            var source = new SharedOperations(log);
            var root = ResolutionScope.Create();
            var typed = Typed(root);
            IOperator<Mutation, RealVector> mutation = source;
            IOperator<Evaluation, RealVector> evaluation = source;
            if (evaluationFirst)
                _ = typed.Resolve(evaluation);
            var operation = typed.Resolve(mutation, out ILiveValue? control);
            Check(control is not null && control.Value == 3, "control projects from selected owner");
            control!.Value = 12;
            _ = typed.Resolve(evaluation);
            Check(Invoke(operation) == 12 && typed.Resolve((IOperator<Checksum, RealVector>)source).Read(RandomNumberGenerator.Create(1), Problem.SearchSpace, Problem) == 12,
                "mutation and external operation see shared live state");
            var named = typed.Resolve(mutation.Named("one").Named("two"), out ILiveValue? namedControl);
            // Each resolution builds its own executor, so forwarding shows in the shared live value rather than in identity.
            Check(namedControl is not null && namedControl.Value == 12 && Invoke(named) == 12, "forwarding preserves controls and operation");
            _ = root.For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, ProbeState>().Resolve(mutation);
            Check(log.Preparations == 1, "one owner serves mutation, evaluation, external and actual-state views");
            var constructions = log.Constructions;
            _ = typed.Resolve(mutation);
            Check(log.Constructions == constructions + 1, "every resolution constructs again; no view is cached");
            Check(Invoke(Typed(ResolutionScope.Create()).Resolve(mutation)) == 3 && log.Preparations == 2,
                "an independent root starts with independent state");
        }
    }

    [Fact]
    public void StateVariance()
    {
        foreach (var narrowFirst in new[] { false, true })
        {
            var source = new SharedOperations(new ProbeLog());
            IOperator<Termination, RealVector> termination = source;
            var scope = ResolutionScope.Create();
            var broad = scope.For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, ISearchState>();
            var narrow = scope.For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, ProbeState>();
            if (narrowFirst)
                _ = narrow.Resolve(termination.Named("narrow"));
            _ = broad.Resolve(termination.Named("broad"));
            _ = narrow.Resolve(termination.Named("narrow"));
            Check(source.Log.Preparations == 1, "broad authored terminator supports both views without new preparation");
            Check(!narrow.Resolve(termination).IsTerminalState(new ProbeState(), RandomNumberGenerator.Create(0), Problem.SearchSpace, Problem), "terminator remains callable");
        }
        var interceptor = new IdentityInterceptor(new ProbeLog());
        var root = ResolutionScope.Create();
        var exact = root.For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, ProbeState>();
        var broadState = root.For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, ISearchState>();
        var state = new ProbeState();
        Check(ReferenceEquals(state, broadState.Resolve(interceptor).Transform(state, null, RandomNumberGenerator.Create(1),
            Problem.SearchSpace, Problem)), "a scope over another state resolves the interceptor over the state its operation names");
        Check(ReferenceEquals(state, exact.Resolve(interceptor).Transform(state, null, RandomNumberGenerator.Create(1),
            Problem.SearchSpace, Problem)), "the exact scope resolves the same interceptor");
        Check(interceptor.Log.Preparations == 1 && interceptor.Log.Constructions == 2,
            "both scopes construct their node from one preparation");
    }

    [Fact]
    public void Ownership()
    {
        IOperator<Mutation, RealVector> source = new CountingMutation();
        var parent = ResolutionScope.Create();
        var child = parent.CreateChildScope();
        // A stateful leaf builds a new executor for each resolution, so the call count shows which state an executor shares.
        var local = Typed(child).Resolve(source);
        var ancestor = Typed(parent).Resolve(source);
        Check(Invoke(local) == 1 && Invoke(ancestor) == 1, "child-first preparation is not hoisted");
        Check(Invoke(Typed(child).Resolve(source)) == 2, "new parent owner does not replace an existing child owner");
        var nextChild = parent.CreateChildScope();
        Check(Invoke(Typed(nextChild).Resolve(source)) == 2, "parent-first owner is reused by descendants");
        Check(Invoke(Typed(parent.With()).Resolve(source)) == 3, "With shares the owning scope's state");
        IOperator<Mutation, RealVector> equalCopy = new CountingMutation();
        Check(source.Equals(equalCopy) && Invoke(Typed(parent).Resolve(equalCopy)) == 1, "value-equal records have separate owners");

        var firstChild = ResolutionScope.Create();
        var left = firstChild.CreateChildScope();
        var right = firstChild.CreateChildScope();
        Check(Invoke(Typed(left).Resolve(source)) == 1 && Invoke(Typed(right).Resolve(source)) == 1, "siblings do not see sibling-local owners");
    }

    [Fact]
    public void Activation()
    {
        foreach (var policy in new[] { ChildScopePolicy.Fresh, ChildScopePolicy.Retained, ChildScopePolicy.Shared })
        {
            IOperator<Mutation, RealVector> source = new CountingMutation();
            var root = ResolutionScope.Create();
            var selector = new ChildScopeSelector(policy);
            var first = Typed(selector.GetScopeFor(source, root)).Resolve(source);
            var second = Typed(selector.GetScopeFor(source, root.With())).Resolve(source);
            Check(Invoke(first) == 1 && Invoke(second) == (policy == ChildScopePolicy.Fresh ? 1 : 2), $"{policy} state trajectory");
        }
    }

    [Fact]
    public void ContextualChildren()
    {
        IOperator<Mutation, RealVector> child = new NodeTokenMutation();
        IOperator<Mutation, RealVector> source = new ContextualMutation(child);
        var root = ResolutionScope.Create();
        var first = Typed(root).Resolve(source);
        var second = Typed(root.With()).Resolve(source);
        Check(!ReferenceEquals(first, second) && Invoke(first) == 1 && Invoke(second) == 2 && Invoke(first) == 1,
            "rebinding constructs new nodes without replacing an earlier node's children");

        IOperator<Mutation, RealVector> counter = new CountingMutation();
        var parent = ResolutionScope.Create();
        var localScope = parent.CreateChildScope();
        var local = Typed(localScope).Resolve(counter);
        var parentComposite = new ContextualMutation(counter);
        var parentNode = Typed(parent).Resolve(parentComposite);
        var descendantNode = Typed(localScope).Resolve(parentComposite);
        Check(Invoke(local) == 1 && Invoke(parentNode) == 1 && Invoke(descendantNode) == 2,
            "ancestor-owned composite dependencies use its owning scope despite a child-local selection");
    }

    [Fact]
    public void Failures()
    {
        var log = new ProbeLog { FailPreparation = true };
        var source = new SharedOperations(log);
        IOperator<Mutation, RealVector> mutation = source;
        var typed = Typed(ResolutionScope.Create());
        Reject(() => typed.Resolve(mutation), "preparation failure");
        log.FailPreparation = false;
        log.FailConstruction = true;
        Reject(() => typed.Resolve(mutation), "construction failure");
        Check(log.Preparations == 2, "failed preparation is retried");
        log.FailConstruction = false;
        Check(Invoke(typed.Resolve(mutation)) == 3 && log.Preparations == 2,
            "successful preparation survives a construction failure");
        var dual = new DualRoleMutation();
        IOperator<Mutation, RealVector> dualMutation = dual;
        var root = ResolutionScope.Create();
        Reject(() => Typed(root.With(Aspect.Create(Pointcut.Reference(dual), new DualRoleAdvice()))).Resolve(dualMutation), "Supported extension bases");
        Check(Invoke(Typed(root).Resolve(dualMutation)) == Input[0][0], "advice rejection leaves unadvised owner usable");
    }

    [Fact]
    public void Cycles()
    {
        var source = new CyclicOperations();
        Reject(() => Typed(ResolutionScope.Create()).Resolve((IOperator<Mutation, RealVector>)source), "Recursive construction");
        Check(source.Preparations == 1, "changing operation does not create a second owner or evade cycle detection");
        var mixed = new MixedCycle();
        Reject(() => Typed(ResolutionScope.Create()).Resolve(mixed), "Recursive construction");
        var ownRole = new OwnRoleNode();
        var scope = ResolutionScope.Create();
        var first = scope.Resolve(ownRole);
        var second = scope.With().Resolve(ownRole);
        Check(!ReferenceEquals(first, second) && ReferenceEquals(first.State, second.State)
            && ReferenceEquals(first.State, scope.CreateChildScope().Resolve(ownRole).State)
            && !ReferenceEquals(first.State, ResolutionScope.Create().Resolve(ownRole).State),
            "an own role builds per resolution and keeps its scoped state in its owner");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (WeakReference Source, WeakReference Execution) MakeDiscarded(ResolutionScope root)
    {
        IOperator<Mutation, RealVector> source = new CountingMutation();
        var executor = Typed(root.CreateChildScope()).Resolve(source);
        return (new WeakReference(source), new WeakReference(executor));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference MakeFreshExecution(ResolutionScope root, IOperator<Mutation, RealVector> source) =>
        new(Typed(root.CreateChildScope()).Resolve(source));

    [Fact]
    public void Lifetime()
    {
        var root = ResolutionScope.Create();
        IOperator<Mutation, RealVector> retainedSource = new CountingMutation();
        var discarded = MakeDiscarded(root);
        var discardedExecution = MakeFreshExecution(root, retainedSource);
        for (var attempt = 0; attempt < 5 && (discarded.Source.IsAlive || discarded.Execution.IsAlive || discardedExecution.IsAlive); attempt++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
        Check(!discarded.Source.IsAlive && !discarded.Execution.IsAlive, "live root does not retain discarded child owner or source");
        Check(!discardedExecution.IsAlive, "live source does not retain discarded fresh execution");
        GC.KeepAlive(root);
        GC.KeepAlive(retainedSource);
    }

    // A mutation that is also an own role, so advice for its own role meets it when it resolves as a mutation.
    private sealed record DualRoleMutation : SingleCandidateMutator<RealVector>, IConfigurationNode<Node>
    {
        public override RealVector MutateCandidate(RealVector parent, IRandomNumberGenerator random) => parent;

        Node IConfigurationNode<Node>.CreateExecutor(ResolutionScope scope) => new();
    }

    private sealed class DualRoleAdvice : Advice<DualRoleMutation, Node>
    {
        public override Node Wrap(Node executor, DualRoleMutation source) => executor;
    }

    private sealed class Node(object? state = null) : IExecutor
    {
        public object? State { get; } = state;
    }

    private sealed class OwnRoleNode : IConfigurationNode<Node>
    {
        public Node CreateExecutor(ResolutionScope scope) => new(scope.GetScopedState(this, static () => new object()));
    }

    private sealed class CyclicOperations : IOperator<Mutation, RealVector>, IOperator<Evaluation, RealVector>
    {
        public int Preparations { get; private set; }
        // One public method implements every operation's CreateExecutor, whose signature does not name the operation.
        public IExecutor CreateExecutor<TSearchSpace, TProblem, TSearchState>(ResolutionScope<RealVector, TSearchSpace, TProblem, TSearchState> scope)
            where TSearchSpace : class, ISearchSpace<RealVector>
            where TProblem : class, IProblem<RealVector, TSearchSpace>
            where TSearchState : class, ISearchState
        {
            scope.GetScopedState(this, () =>
            {
                Preparations++;
                return new Preparation();
            });
            _ = Typed(scope).Resolve((IOperator<Evaluation, RealVector>)this);
            throw new InvalidOperationException("Cycle was not detected.");
        }

        private sealed class Preparation;
    }

    private sealed class MixedCycle : IOperator<Mutation, RealVector>
    {
        IExecutor IOperator<Mutation, RealVector>.CreateExecutor<TSearchSpace, TProblem, TSearchState>(
            ResolutionScope<RealVector, TSearchSpace, TProblem, TSearchState> scope)
        {
            _ = scope.Resolve(new Bridge(this));
            throw new InvalidOperationException("Mixed cycle was not detected.");
        }

        private sealed class Bridge(IOperator<Mutation, RealVector> child) : IConfigurationNode<Node>
        {
            public Node CreateExecutor(ResolutionScope scope)
            {
                _ = Typed(scope).Resolve(child);
                return new Node();
            }
        }
    }
}
