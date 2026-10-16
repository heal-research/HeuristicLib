using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems.TestFunctions;
using RoleSlice;

// TArguments separate foundation variant: include search-state type in the hidden run-binding
// context, while configurations still name only candidate and role.
namespace StateFamilies;

public interface IStateRole<TRole> where TRole : IStateRole<TRole>
{
    static abstract Context<TCandidate, TRole, TSearchSpace, TProblem, TState> Create<TCandidate, TSearchSpace, TProblem, TState>()
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TState : class, ISearchState;
}
public interface IOperator<TCandidate, TRole> : IConfigurationNode where TRole : IStateRole<TRole>
{
    PreparedFactory Prepare<TSearchSpace, TProblem, TState>(Context<TCandidate, TRole, TSearchSpace, TProblem, TState> context)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TState : class, ISearchState;
}
public abstract class Context<TCandidate, TRole, TSearchSpace, TProblem, TState>
    where TRole : IStateRole<TRole>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TState : class, ISearchState
{
    public abstract PreparedFactory Forward(IOperator<TCandidate, TRole> source);
}
public sealed class TypedContext<TCandidate, TRole, TSearchSpace, TProblem, TState, TExecution> : Context<TCandidate, TRole, TSearchSpace, TProblem, TState>
    where TRole : IStateRole<TRole>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TState : class, ISearchState
    where TExecution : class, IExecutionNode
{
    public BoundExecution<TExecution> Resolve(ResolutionScope scope, IOperator<TCandidate, TRole> source) => scope.Resolve(source,
        configuration => configuration.Prepare(this).Require<TExecution>(),
        static (advice, configuration) => throw new InvalidOperationException("This isolated state-family probe installs no advice."));
    public override PreparedFactory Forward(IOperator<TCandidate, TRole> source) => PreparedFactory.Bound<TExecution>(scope => Resolve(scope, source));
}
public sealed class TerminationRole : IStateRole<TerminationRole>
{
    public static Context<TCandidate, TerminationRole, TSearchSpace, TProblem, TState> Create<TCandidate, TSearchSpace, TProblem, TState>()
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TState : class, ISearchState => new TypedContext<TCandidate, TerminationRole, TSearchSpace, TProblem, TState, ITerminatorExecution<TCandidate, TSearchSpace, TProblem, TState>>();
}
public sealed class InterceptionRole : IStateRole<InterceptionRole>
{
    public static Context<TCandidate, InterceptionRole, TSearchSpace, TProblem, TState> Create<TCandidate, TSearchSpace, TProblem, TState>()
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TState : class, ISearchState => new TypedContext<TCandidate, InterceptionRole, TSearchSpace, TProblem, TState, IInterceptorExecution<TCandidate, TSearchSpace, TProblem, TState>>();
}
public sealed record Named<TCandidate, TRole>(IOperator<TCandidate, TRole> Child, string Name) : IOperator<TCandidate, TRole> where TRole : IStateRole<TRole>
{
    public PreparedFactory Prepare<TSearchSpace, TProblem, TState>(Context<TCandidate, TRole, TSearchSpace, TProblem, TState> context)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TState : class, ISearchState => context.Forward(Child);
}
public sealed record AlwaysTerminator<TCandidate, TAuthoredSearchState> : IOperator<TCandidate, TerminationRole> where TAuthoredSearchState : class, ISearchState
{
    public PreparedFactory Prepare<TSearchSpace, TProblem, TState>(Context<TCandidate, TerminationRole, TSearchSpace, TProblem, TState> context)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TState : class, ISearchState
    {
        if (!typeof(TState).IsAssignableTo(typeof(TAuthoredSearchState)))
            throw new InvalidOperationException("Terminator does not accept this state.");
        return PreparedFactory.Raw<ITerminatorExecution<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, TAuthoredSearchState>>(_ => new Execution());
    }
    private sealed class Execution : ITerminatorExecution<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, TAuthoredSearchState>
    {
        public bool IsTerminalState(TAuthoredSearchState state, ISearchSpace<TCandidate> space, IProblem<TCandidate, ISearchSpace<TCandidate>> problem) => true;
    }
}
public sealed record IdentityInterceptor<TCandidate, TAuthoredSearchState> : IOperator<TCandidate, InterceptionRole> where TAuthoredSearchState : class, ISearchState
{
    public PreparedFactory Prepare<TSearchSpace, TProblem, TState>(Context<TCandidate, InterceptionRole, TSearchSpace, TProblem, TState> context)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TState : class, ISearchState
    {
        if (typeof(TState) != typeof(TAuthoredSearchState))
            throw new InvalidOperationException("Interceptor state must match exactly.");
        return PreparedFactory.Raw<IInterceptorExecution<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, TAuthoredSearchState>>(_ => new Execution());
    }
    private sealed class Execution : IInterceptorExecution<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, TAuthoredSearchState>
    {
        public TAuthoredSearchState Transform(TAuthoredSearchState current, TAuthoredSearchState? previous, IRandomNumberGenerator random, ISearchSpace<TCandidate> space, IProblem<TCandidate, ISearchSpace<TCandidate>> problem) => current;
    }
}
public static class Program
{
    private static void MustFail(Action action)
    {
        try
        { action(); }
        catch (InvalidOperationException) { return; }
        throw new Exception("Expected state mismatch.");
    }
    public static void Main()
    {
        var problem = new TestFunctionProblem(new SphereFunction(1));
        IOperator<RealVector, TerminationRole> broad = new Named<RealVector, TerminationRole>(new AlwaysTerminator<RealVector, ISearchState>(), "any-state termination");
        var terminators = (TypedContext<RealVector, TerminationRole, BoundedRealVectorSearchSpace, TestFunctionProblem, PopulationState<RealVector>, ITerminatorExecution<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, PopulationState<RealVector>>>)
            TerminationRole.Create<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, PopulationState<RealVector>>();
        var state = Population.From(new[] { new EvaluatedCandidate<RealVector>(new RealVector(1.0), (ObjectiveVector)1.0) }).ToPopulationState();
        if (!terminators.Resolve(ResolutionScope.Create(), broad).Operation.IsTerminalState(state, problem.SearchSpace, problem))
            throw new Exception();
        var other = (TypedContext<RealVector, TerminationRole, BoundedRealVectorSearchSpace, TestFunctionProblem, SearchState, ITerminatorExecution<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, SearchState>>)
            TerminationRole.Create<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, SearchState>();
        MustFail(() => other.Resolve(ResolutionScope.Create(), new AlwaysTerminator<RealVector, PopulationState<RealVector>>()));
        var interceptors = (TypedContext<RealVector, InterceptionRole, BoundedRealVectorSearchSpace, TestFunctionProblem, PopulationState<RealVector>, IInterceptorExecution<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, PopulationState<RealVector>>>)
            InterceptionRole.Create<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, PopulationState<RealVector>>();
        var exact = new IdentityInterceptor<RealVector, PopulationState<RealVector>>();
        if (!ReferenceEquals(state, interceptors.Resolve(ResolutionScope.Create(), exact).Operation.Transform(state, null, RandomNumberGenerator.Create(1), problem.SearchSpace, problem)))
            throw new Exception();
        MustFail(() => interceptors.Resolve(ResolutionScope.Create(), new IdentityInterceptor<RealVector, ISearchState>()));
        Console.WriteLine("PASS state-family variant: candidate-only termination/interception roles; generic naming; hidden run-state parameter; real contravariant terminator and invariant interceptor interfaces");
    }
}
