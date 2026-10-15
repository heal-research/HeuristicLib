using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Selectors;

public abstract record StatelessSelector<TCandidate, TSearchSpace, TProblem>
    : Selector<TCandidate, TSearchSpace, TProblem>, ISelectorExecution<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public sealed override ExecutionFactory<ISelectorExecution<TCandidate, TSearchSpace, TProblem>> CreateExecutionFactory() => _ => this;

    public abstract IReadOnlyList<EvaluatedCandidate<TCandidate>> Select(IReadOnlyList<EvaluatedCandidate<TCandidate>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
}

public abstract record StatelessSelector<TCandidate, TSearchSpace>
    : Selector<TCandidate, TSearchSpace>, ISelectorExecution<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>
    where TSearchSpace : class, ISearchSpace<TCandidate>
{
    public sealed override ExecutionFactory<ISelectorExecution<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>> CreateExecutionFactory() => _ => this;

    public abstract IReadOnlyList<EvaluatedCandidate<TCandidate>> Select(IReadOnlyList<EvaluatedCandidate<TCandidate>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random, TSearchSpace searchSpace);

    IReadOnlyList<EvaluatedCandidate<TCandidate>> ISelectorExecution<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>.Select(IReadOnlyList<EvaluatedCandidate<TCandidate>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random, TSearchSpace searchSpace, IProblem<TCandidate, TSearchSpace> problem) =>
        Select(population, objective, count, random, searchSpace);
}

public abstract record StatelessSelector<TCandidate>
    : Selector<TCandidate>, ISelectorExecution<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>
{
    public sealed override ExecutionFactory<ISelectorExecution<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>> CreateExecutionFactory() => _ => this;

    public abstract IReadOnlyList<EvaluatedCandidate<TCandidate>> Select(IReadOnlyList<EvaluatedCandidate<TCandidate>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random);

    IReadOnlyList<EvaluatedCandidate<TCandidate>> ISelectorExecution<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>.Select(IReadOnlyList<EvaluatedCandidate<TCandidate>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random, ISearchSpace<TCandidate> searchSpace, IProblem<TCandidate, ISearchSpace<TCandidate>> problem) =>
        Select(population, objective, count, random);
}
