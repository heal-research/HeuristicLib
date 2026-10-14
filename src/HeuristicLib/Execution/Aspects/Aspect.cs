namespace HEAL.HeuristicLib.Execution;

/// <summary>Pairs a pointcut with a wrapper recipe or composes other aspects.</summary>
/// <remarks>
/// Each rule retains its aspect identity across installations. Composition declarations run once and expand in
/// installation order. A shared aspect applies once at its first occurrence throughout a scope's aspect collection.
/// Prepared advice belongs to the scope's execution state, so aspects can be reused without sharing that state.
/// </remarks>
public abstract class Aspect
{
    private readonly Lock sync = new();
    private readonly Func<IConfigurationNode, bool>? matches;
    private readonly Func<IConfigurationNode, IConfigurationNode>? recipe;
    private ImmutableArray<Aspect> children;
    private bool declaring;

    protected Aspect() { }

    private Aspect(Func<IConfigurationNode, bool> matches, Func<IConfigurationNode, IConfigurationNode> recipe)
    {
        this.matches = matches;
        this.recipe = recipe;
        // A single-rule aspect has no children and provides its own advice-cache identity.
        children = [];
    }

    protected abstract void Declare(AspectBuilder builder);
    public static Aspect Create(Action<AspectBuilder> declare) => new CallbackAspect(declare);

    public static Aspect Create<TConfiguration>(TConfiguration configuration, Func<TConfiguration, TConfiguration> recipe)
        where TConfiguration : class, IConfigurationNode => Create(Pointcut.Reference(configuration), recipe);

    /// <remarks>
    /// Recipes receive the selected configuration, including composite targets, and must return a single-child
    /// wrapper of the resolved role. Each call creates an independent aspect, even for the same pointcut and recipe.
    /// Pass it directly to a resolution scope or run, or install it within a composition.
    /// </remarks>
    public static Aspect Create<TConfiguration>(Pointcut<TConfiguration> pointcut, Func<TConfiguration, TConfiguration> recipe)
        where TConfiguration : class, IConfigurationNode =>
        new AdvisingAspect(pointcut.Matches, configuration => recipe((TConfiguration)configuration));

    internal bool Matches(IConfigurationNode configuration) => matches?.Invoke(configuration) ?? false;

    internal Advice<TExecution> Prepare<TExecution>(IConfigurationNode configuration, Func<IConfigurationNode, Advice<TExecution>> prepareAdvice)
        where TExecution : class, IExecutionNode => prepareAdvice(CreateWrapper(configuration));

    private IConfigurationNode CreateWrapper(IConfigurationNode configuration)
    {
        if (recipe is null)
            throw new InvalidOperationException("Only a single-rule aspect creates a wrapper.");
        var wrapper = recipe(configuration) ?? throw new InvalidOperationException("An advice recipe returned null.");
        if (ReferenceEquals(wrapper, configuration))
            throw new InvalidOperationException("An advice recipe must wrap its supplied source, not return it unchanged.");
        return wrapper;
    }

    private void EnsureDeclared()
    {
        lock (sync)
        {
            if (!children.IsDefault)
                return;
            if (declaring)
                throw new InvalidOperationException("Recursive aspect declaration.");
            declaring = true;
            try
            {
                var builder = new AspectBuilder();
                Declare(builder);
                children = builder.Snapshot();
            }
            finally { declaring = false; }
        }
    }

    internal static ImmutableArray<Aspect> Collect(IReadOnlyList<Aspect> aspects)
    {
        var result = ImmutableArray.CreateBuilder<Aspect>();
        var visited = new HashSet<Aspect>(ReferenceEqualityComparer.Instance);
        foreach (var aspect in aspects)
            Visit(aspect);
        return result.ToImmutable();

        void Visit(Aspect aspect)
        {
            if (!visited.Add(aspect))
                return;
            aspect.EnsureDeclared();
            if (aspect.matches is not null)
                result.Add(aspect);
            foreach (var child in aspect.children)
                Visit(child);
        }
    }

    private sealed class CallbackAspect(Action<AspectBuilder> declare) : Aspect
    {
        protected override void Declare(AspectBuilder builder) => declare(builder);
    }

    private sealed class AdvisingAspect(Func<IConfigurationNode, bool> matchConfiguration, Func<IConfigurationNode, IConfigurationNode> createWrapper)
        : Aspect(matchConfiguration, createWrapper)
    {
        protected override void Declare(AspectBuilder builder) { }
    }
}
