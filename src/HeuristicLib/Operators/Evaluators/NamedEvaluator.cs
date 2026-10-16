using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators.Evaluators;

namespace HEAL.HeuristicLib.Operators;

/// <summary>Gives a evaluator configuration a name and forwards its execution unchanged.</summary>
/// <remarks>The wrapper and its child are separate configurations; a name pointcut selects the wrapper.</remarks>
public sealed record NamedEvaluator<TCandidate> : WrappingEvaluator<TCandidate>, INamedConfiguration
{
    public NamedEvaluator(IEvaluator<TCandidate> childEvaluator, string name)
        : base(childEvaluator)
    {
        Name = name;
    }

    public string Name { get; init; }

    protected override WrapperExecutorFactory<IEvaluationExecutor<TCandidate, TRunSearchSpace, TRunProblem>> CreateWrapperFactory<TRunSearchSpace, TRunProblem>() =>
        child => child;
}

public static class NamedEvaluator
{
    public static NamedEvaluator<TCandidate> Create<TCandidate>(IEvaluator<TCandidate> childEvaluator, string name) => new(childEvaluator, name);

    extension<TCandidate>(IEvaluator<TCandidate> evaluator)
    {
        public NamedEvaluator<TCandidate> Named(string name) => Create(evaluator, name);
    }
}
