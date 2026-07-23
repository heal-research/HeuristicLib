using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Evaluators;

/// <remarks>
/// Derive directly from this base when the evaluator owns child execution instances or needs direct control over its execution structure.
/// Use <see cref="StatelessEvaluator{TCandidate,TSearchSpace,TProblem}"/> when no mutable execution data is needed.
/// Use <see cref="StatefulEvaluator{TCandidate,TSearchSpace,TProblem,TState}"/> when only ordinary execution data is needed.
/// </remarks>
public abstract record Evaluator<TCandidate, TSearchSpace, TProblem>
    : IEvaluator<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected abstract IEvaluatorInstance<TCandidate, TSearchSpace, TProblem> CreateEvaluatorInstance(ExecutionInstanceRegistry registry);

    IEvaluatorInstance<TCandidate, TSearchSpace, TProblem> IExecutionInstanceResolvable<IEvaluatorInstance<TCandidate, TSearchSpace, TProblem>>.CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
        CreateEvaluatorInstance(instanceRegistry);
}

public abstract record Evaluator<TCandidate, TSearchSpace>
    : IEvaluator<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>
    where TSearchSpace : class, ISearchSpace<TCandidate>
{
    protected abstract IEvaluatorInstance<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>> CreateEvaluatorInstance(ExecutionInstanceRegistry registry);

    IEvaluatorInstance<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>> IExecutionInstanceResolvable<IEvaluatorInstance<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>>.CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
        CreateEvaluatorInstance(instanceRegistry);
}

public abstract record Evaluator<TCandidate>
    : IEvaluator<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>
{
    protected abstract IEvaluatorInstance<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>> CreateEvaluatorInstance(ExecutionInstanceRegistry registry);

    IEvaluatorInstance<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>> IExecutionInstanceResolvable<IEvaluatorInstance<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>>.CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
        CreateEvaluatorInstance(instanceRegistry);
}

public abstract class EvaluatorInstance<TCandidate, TSearchSpace, TProblem>
    : IEvaluatorInstance<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public abstract IReadOnlyList<EvaluatedCandidate<TCandidate>> Evaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
}

public abstract class EvaluatorInstance<TCandidate, TSearchSpace>
    : IEvaluatorInstance<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>
    where TSearchSpace : class, ISearchSpace<TCandidate>
{
    public abstract IReadOnlyList<EvaluatedCandidate<TCandidate>> Evaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace);

    IReadOnlyList<EvaluatedCandidate<TCandidate>> IEvaluatorInstance<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>.Evaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace, IProblem<TCandidate, TSearchSpace> problem) =>
        Evaluate(candidates, random, searchSpace);
}

public abstract class EvaluatorInstance<TCandidate>
    : IEvaluatorInstance<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>
{
    public abstract IReadOnlyList<EvaluatedCandidate<TCandidate>> Evaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random);

    IReadOnlyList<EvaluatedCandidate<TCandidate>> IEvaluatorInstance<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>.Evaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, ISearchSpace<TCandidate> searchSpace, IProblem<TCandidate, ISearchSpace<TCandidate>> problem) =>
        Evaluate(candidates, random);
}
