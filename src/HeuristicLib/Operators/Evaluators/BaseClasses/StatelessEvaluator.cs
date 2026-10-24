using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Evaluators;

public abstract record StatelessEvaluator<TCandidate, TSearchSpace, TProblem>
    : Evaluator<TCandidate, TSearchSpace, TProblem>, IEvaluationExecutor<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected sealed override IEvaluationExecutor<TCandidate, TSearchSpace, TProblem> CreateExecutor(ResolutionScope scope) => this;

    public abstract IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
}

public abstract record StatelessEvaluator<TCandidate, TSearchSpace>
    : Evaluator<TCandidate, TSearchSpace>, IEvaluationExecutor<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>
    where TSearchSpace : class, ISearchSpace<TCandidate>
{
    protected sealed override IEvaluationExecutor<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>> CreateExecutor(ResolutionScope scope) => this;

    public abstract IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace);

    IReadOnlyList<ObjectiveVector> IEvaluationExecutor<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>.Evaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace, IProblem<TCandidate, TSearchSpace> problem) =>
        Evaluate(candidates, random, searchSpace);
}

public abstract record StatelessEvaluator<TCandidate>
    : Evaluator<TCandidate>, IEvaluationExecutor<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>
{
    protected sealed override IEvaluationExecutor<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>> CreateExecutor(ResolutionScope scope) => this;

    public abstract IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random);

    IReadOnlyList<ObjectiveVector> IEvaluationExecutor<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>.Evaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, ISearchSpace<TCandidate> searchSpace, IProblem<TCandidate, ISearchSpace<TCandidate>> problem) =>
        Evaluate(candidates, random);
}
