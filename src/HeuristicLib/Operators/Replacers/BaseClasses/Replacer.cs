using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Replacers;

/// <remarks>
/// Derive directly from this base when the replacer owns child execution nodes or needs direct control over its execution structure.
/// Use <see cref="StatelessReplacer{TCandidate,TSearchSpace,TProblem}"/> when no mutable execution data is needed.
/// Use <see cref="StatefulReplacer{TCandidate,TSearchSpace,TProblem,TState}"/> when only ordinary execution data is needed.
/// </remarks>
public abstract record Replacer<TCandidate, TSearchSpace, TProblem>
    : IReplacer<TCandidate>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public abstract ExecutionFactory<IReplacerExecution<TCandidate, TSearchSpace, TProblem>> CreateExecutionFactory();

    public bool Fits(ExecutionSignature execution) => execution.SearchSpace.IsAssignableTo(typeof(TSearchSpace)) && execution.Problem.IsAssignableTo(typeof(TProblem));

    ExecutionFactory<IReplacerExecution<TCandidate, TRunSearchSpace, TRunProblem>> IReplacer<TCandidate>.CreateExecutionFactory<TRunSearchSpace, TRunProblem>()
    {
        if (!typeof(TRunSearchSpace).IsAssignableTo(typeof(TSearchSpace)) || !typeof(TRunProblem).IsAssignableTo(typeof(TProblem)))
        {
            throw ExecutionSignature.Mismatch(
                this,
                ExecutionSignature.Describe(typeof(TSearchSpace), typeof(TProblem)),
                ExecutionSignature.Describe(typeof(TRunSearchSpace), typeof(TRunProblem)));
        }

        return (ExecutionFactory<IReplacerExecution<TCandidate, TRunSearchSpace, TRunProblem>>)CreateExecutionFactory();
    }
}

public abstract record Replacer<TCandidate, TSearchSpace>
    : Replacer<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>
    where TSearchSpace : class, ISearchSpace<TCandidate>;

public abstract record Replacer<TCandidate>
    : Replacer<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>;

public abstract class ReplacerExecution<TCandidate, TSearchSpace, TProblem>
    : IReplacerExecution<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public abstract IReadOnlyList<EvaluatedCandidate<TCandidate>> Replace(IReadOnlyList<EvaluatedCandidate<TCandidate>> previousPopulation, IReadOnlyList<EvaluatedCandidate<TCandidate>> offspringPopulation, ObjectiveDirections objective, int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
}

public abstract class ReplacerExecution<TCandidate, TSearchSpace>
    : IReplacerExecution<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>
    where TSearchSpace : class, ISearchSpace<TCandidate>
{
    public abstract IReadOnlyList<EvaluatedCandidate<TCandidate>> Replace(IReadOnlyList<EvaluatedCandidate<TCandidate>> previousPopulation, IReadOnlyList<EvaluatedCandidate<TCandidate>> offspringPopulation, ObjectiveDirections objective, int count, IRandomNumberGenerator random, TSearchSpace searchSpace);

    IReadOnlyList<EvaluatedCandidate<TCandidate>> IReplacerExecution<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>.Replace(IReadOnlyList<EvaluatedCandidate<TCandidate>> previousPopulation, IReadOnlyList<EvaluatedCandidate<TCandidate>> offspringPopulation, ObjectiveDirections objective, int count, IRandomNumberGenerator random, TSearchSpace searchSpace, IProblem<TCandidate, TSearchSpace> problem) =>
        Replace(previousPopulation, offspringPopulation, objective, count, random, searchSpace);
}

public abstract class ReplacerExecution<TCandidate>
    : IReplacerExecution<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>
{
    public abstract IReadOnlyList<EvaluatedCandidate<TCandidate>> Replace(IReadOnlyList<EvaluatedCandidate<TCandidate>> previousPopulation, IReadOnlyList<EvaluatedCandidate<TCandidate>> offspringPopulation, ObjectiveDirections objective, int count, IRandomNumberGenerator random);

    IReadOnlyList<EvaluatedCandidate<TCandidate>> IReplacerExecution<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>.Replace(IReadOnlyList<EvaluatedCandidate<TCandidate>> previousPopulation, IReadOnlyList<EvaluatedCandidate<TCandidate>> offspringPopulation, ObjectiveDirections objective, int count, IRandomNumberGenerator random, ISearchSpace<TCandidate> searchSpace, IProblem<TCandidate, ISearchSpace<TCandidate>> problem) =>
        Replace(previousPopulation, offspringPopulation, objective, count, random);
}
