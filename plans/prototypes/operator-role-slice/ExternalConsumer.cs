using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems.TestFunctions;
using RoleSlice;

namespace ConsumerRoles;

// This entire role, including its non-collection result shape, is outside RoleSlice.dll.
public interface IChecksumExecution<TCandidate, in TSearchSpace, in TProblem> : IExecutionNode
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    int Checksum(TCandidate candidate, IRandomNumberGenerator random, TSearchSpace space, TProblem problem);
}
public sealed class ChecksumRole : ISynchronousRole<ChecksumRole>
{
    public static RoleBinding<TCandidate, ChecksumRole, TSearchSpace, TProblem> CreateBinding<TCandidate, TSearchSpace, TProblem>()
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> => new ChecksumBinding<TCandidate, TSearchSpace, TProblem>();
}
public sealed class ChecksumBinding<TCandidate, TSearchSpace, TProblem> : SynchronousBinding<TCandidate, ChecksumRole, TSearchSpace, TProblem, IChecksumExecution<TCandidate, TSearchSpace, TProblem>>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected override IChecksumExecution<TCandidate, TSearchSpace, TProblem> CreatePolicyExecution(ImmutableArray<IChecksumExecution<TCandidate, TSearchSpace, TProblem>> children, SynchronousPolicy policy) => new PolicyExecution(children, policy);
    protected override IChecksumExecution<TCandidate, TSearchSpace, TProblem> CreateHookExecution<THooks, TToken>(IChecksumExecution<TCandidate, TSearchSpace, TProblem> inner, THooks hooks) => new HookExecution<THooks, TToken>(inner, hooks);
    protected override IChecksumExecution<TCandidate, TSearchSpace, TProblem> CreateSuccessExecution<THooks>(IChecksumExecution<TCandidate, TSearchSpace, TProblem> inner, THooks hook) => new SuccessExecution<THooks>(inner, hook);
    private sealed class SuccessExecution<THooks>(IChecksumExecution<TCandidate, TSearchSpace, TProblem> inner, THooks hook) : IChecksumExecution<TCandidate, TSearchSpace, TProblem> where THooks : struct, ISuccessHook
    {
        private THooks callback = hook;
        public int Checksum(TCandidate candidate, IRandomNumberGenerator random, TSearchSpace space, TProblem problem)
        {
            var result = inner.Checksum(candidate, random, space, problem);
            callback.OnSuccess(0);
            return result;
        }
    }
    private sealed class PolicyExecution(ImmutableArray<IChecksumExecution<TCandidate, TSearchSpace, TProblem>> children, SynchronousPolicy policy) : IChecksumExecution<TCandidate, TSearchSpace, TProblem>
    {
        private static readonly Func<IChecksumExecution<TCandidate, TSearchSpace, TProblem>, (TCandidate Candidate, IRandomNumberGenerator Random, TSearchSpace Space, TProblem Problem), CallResult<int>> Next =
            static (inner, args) => new(inner.Checksum(args.Candidate, args.Random, args.Space, args.Problem), 0);
        public int Checksum(TCandidate candidate, IRandomNumberGenerator random, TSearchSpace space, TProblem problem) => policy.Execute(children, (candidate, random, space, problem), random, Next).Value;
    }
    private sealed class HookExecution<THooks, TToken>(IChecksumExecution<TCandidate, TSearchSpace, TProblem> inner, THooks hooks) : IChecksumExecution<TCandidate, TSearchSpace, TProblem> where THooks : struct, ICallHooks<TToken>
    {
        private THooks callbacks = hooks;
        public int Checksum(TCandidate candidate, IRandomNumberGenerator random, TSearchSpace space, TProblem problem)
        {
            var token = callbacks.Enter();
            try
            { var result = inner.Checksum(candidate, random, space, problem); callbacks.Success(ref token, 0); return result; }
            catch (Exception error) { callbacks.Fault(ref token, error); throw; }
            finally { callbacks.Finally(ref token); }
        }
    }
}
public sealed record ChecksumOperator(int Failures) : BoundOperator<RealVector, ChecksumRole, ISearchSpace<RealVector>, IProblem<RealVector, ISearchSpace<RealVector>>, IChecksumExecution<RealVector, ISearchSpace<RealVector>, IProblem<RealVector, ISearchSpace<RealVector>>>>
{
    public override ExecutionFactory<IChecksumExecution<RealVector, ISearchSpace<RealVector>, IProblem<RealVector, ISearchSpace<RealVector>>>> CreateExecutionFactory()
    {
        var execution = new Execution(Failures);
        return _ => execution;
    }
    private sealed class Execution(int remaining) : IChecksumExecution<RealVector, ISearchSpace<RealVector>, IProblem<RealVector, ISearchSpace<RealVector>>>
    {
        private int failures = remaining;
        public int Checksum(RealVector candidate, IRandomNumberGenerator random, ISearchSpace<RealVector> space, IProblem<RealVector, ISearchSpace<RealVector>> problem) => failures-- > 0 ? throw new RetryableFailure() : candidate.Count;
    }
}

// TArguments new generic meta operator authored by a consumer, usable on package and consumer roles.
public sealed record Twice<TCandidate, TRole> : WrappingOperator<TCandidate, TRole> where TRole : ISynchronousRole<TRole>
{
    public Twice(IOperator<TCandidate, TRole> child) : base(child) { }
    protected override PreparedWrapper CreateWrapperFactory<TSearchSpace, TProblem>(RoleBinding<TCandidate, TRole, TSearchSpace, TProblem> binding) =>
        ((ISynchronousBinding<TCandidate, TRole, TSearchSpace, TProblem>)binding).PolicyWrapper(new TwicePolicy());
    private sealed class TwicePolicy : SynchronousPolicy
    {
        public override CallResult<TResult> Execute<TExecution, TArguments, TResult>(ImmutableArray<TExecution> children, TArguments arguments, IRandomNumberGenerator random, Func<TExecution, TArguments, CallResult<TResult>> next)
        {
            next(children[0], arguments);
            return next(children[0], arguments);
        }
    }
}
public static class Program
{
    public static void Main()
    {
        var problem = new TestFunctionProblem(new SphereFunction(3));
        var random = RandomNumberGenerator.Create(12);
        var source = new ChecksumOperator(1);
        var wrapped = source.Retry(2).Named("external checksum");
        var scope = ResolutionScope.Create(source.CountCalls(out var calls));
        var binding = (ChecksumBinding<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>)BindingCache<RealVector, ChecksumRole, BoundedRealVectorSearchSpace, TestFunctionProblem>.Binding;
        if (binding.Resolve(scope, wrapped).Operation.Checksum(new RealVector(1.0, 2.0, 3.0), random, problem.SearchSpace, problem) != 3 || calls.CurrentCount != 1)
            throw new Exception();
        var twice = new Twice<RealVector, ChecksumRole>(wrapped).Named("new wrapper");
        binding.Resolve(scope, twice).Operation.Checksum(new RealVector(1.0), random, problem.SearchSpace, problem);
        if (calls.CurrentCount != 3)
            throw new Exception();
        var mutation = new IdentityMutator<RealVector>();
        var mutationScope = ResolutionScope.Create(mutation.CountCalls(out var mutationCalls));
        var typed = new TypedScope<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(mutationScope);
        typed.Resolve(new Twice<RealVector, MutationRole>(mutation)).Mutate([new RealVector(1.0)], random, problem.SearchSpace, problem);
        if (mutationCalls.CurrentCount != 2)
            throw new Exception();
        Console.WriteLine("PASS external non-collection role: inferred retry/name, shared hooks and explicit self-typed role binding");
        Console.WriteLine("PASS external generic Twice wrapper uses the same implementation on built-in and external roles; no role registry edits");
    }
}
