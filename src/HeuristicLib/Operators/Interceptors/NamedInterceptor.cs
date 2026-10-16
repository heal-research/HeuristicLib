using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators.Interceptors;

namespace HEAL.HeuristicLib.Operators;

/// <summary>Gives a interceptor configuration a name and forwards its execution unchanged.</summary>
/// <remarks>The wrapper and its child are separate configurations; a name pointcut selects the wrapper.</remarks>
public sealed record NamedInterceptor<TCandidate> : WrappingInterceptor<TCandidate>, INamedConfiguration
{
    public NamedInterceptor(IInterceptor<TCandidate> childInterceptor, string name)
        : base(childInterceptor)
    {
        Name = name;
    }

    public string Name { get; init; }

    protected override WrapperExecutorFactory<IInterceptionExecutor<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState>> CreateWrapperFactory<TRunSearchSpace, TRunProblem, TRunSearchState>() =>
        child => child;
}

public static class NamedInterceptor
{
    public static NamedInterceptor<TCandidate> Create<TCandidate>(IInterceptor<TCandidate> childInterceptor, string name) => new(childInterceptor, name);

    extension<TCandidate>(IInterceptor<TCandidate> interceptor)
    {
        public NamedInterceptor<TCandidate> Named(string name) => Create(interceptor, name);
    }
}
