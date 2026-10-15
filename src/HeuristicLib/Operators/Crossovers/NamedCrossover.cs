using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators.Crossovers;

namespace HEAL.HeuristicLib.Operators;

/// <summary>Gives a crossover configuration a name and forwards its execution unchanged.</summary>
/// <remarks>The wrapper and its child are separate configurations; a name pointcut selects the wrapper.</remarks>
public sealed record NamedCrossover<TCandidate> : WrappingCrossover<TCandidate>, INamedConfiguration
{
    public NamedCrossover(ICrossover<TCandidate> childCrossover, string name)
        : base(childCrossover)
    {
        Name = name;
    }

    public string Name { get; init; }

    protected override WrapperExecutionFactory<ICrossoverExecution<TCandidate, TRunSearchSpace, TRunProblem>> CreateWrapperFactory<TRunSearchSpace, TRunProblem>() =>
        child => child;
}

public static class NamedCrossover
{
    public static NamedCrossover<TCandidate> Create<TCandidate>(ICrossover<TCandidate> childCrossover, string name) => new(childCrossover, name);

    extension<TCandidate>(ICrossover<TCandidate> crossover)
    {
        public NamedCrossover<TCandidate> Named(string name) => Create(crossover, name);
    }
}
