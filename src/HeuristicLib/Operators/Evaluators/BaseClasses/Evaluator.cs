using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
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
    : IEvaluator<TCandidate>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public abstract IEvaluatorInstance<TCandidate, TSearchSpace, TProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry);

    IEvaluatorInstance<TCandidate, TRunSearchSpace, TRunProblem> IEvaluator<TCandidate>.CreateExecutionInstance<TRunSearchSpace, TRunProblem>(ExecutionInstanceRegistry instanceRegistry)
    {
        if (CreateExecutionInstance(instanceRegistry) is not IEvaluatorInstance<TCandidate, TRunSearchSpace, TRunProblem> instance)
        {
            throw new InvalidOperationException(
                $"{GetType().Name} is written for {typeof(TSearchSpace).Name} and {typeof(TProblem).Name}, and cannot run over {typeof(TRunSearchSpace).Name} with {typeof(TRunProblem).Name}.");
        }

        return instance;
    }
}

public abstract record Evaluator<TCandidate, TSearchSpace>
    : Evaluator<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>
    where TSearchSpace : class, ISearchSpace<TCandidate>;

public abstract record Evaluator<TCandidate>
    : Evaluator<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>;

public abstract class EvaluatorInstance<TCandidate, TSearchSpace, TProblem>
    : IEvaluatorInstance<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public abstract IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
}

public abstract class EvaluatorInstance<TCandidate, TSearchSpace>
    : IEvaluatorInstance<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>
    where TSearchSpace : class, ISearchSpace<TCandidate>
{
    public abstract IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace);

    IReadOnlyList<ObjectiveVector> IEvaluatorInstance<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>.Evaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace, IProblem<TCandidate, TSearchSpace> problem) =>
        Evaluate(candidates, random, searchSpace);
}

public abstract class EvaluatorInstance<TCandidate>
    : IEvaluatorInstance<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>
{
    public abstract IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random);

    IReadOnlyList<ObjectiveVector> IEvaluatorInstance<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>.Evaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, ISearchSpace<TCandidate> searchSpace, IProblem<TCandidate, ISearchSpace<TCandidate>> problem) =>
        Evaluate(candidates, random);
}
