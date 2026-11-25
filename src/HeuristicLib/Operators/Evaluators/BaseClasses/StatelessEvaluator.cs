using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Evaluators;

public abstract record StatelessEvaluator<TCandidate, TSearchSpace, TProblem> : Evaluator<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected sealed override IEvaluationExecutor<TCandidate> CreateExecutor<TRunSearchState>(ResolutionScope<TCandidate, TSearchSpace, TProblem, TRunSearchState> scope) => new Executor(this, scope.Problem);

    public abstract IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

    private sealed class Executor(StatelessEvaluator<TCandidate, TSearchSpace, TProblem> configuration, TProblem problem) : EvaluationExecutor<TCandidate>
    {
        public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random) => configuration.Evaluate(candidates, random, problem.SearchSpace, problem);
    }
}

public abstract record StatelessEvaluator<TCandidate, TSearchSpace> : Evaluator<TCandidate, TSearchSpace>
    where TSearchSpace : class, ISearchSpace<TCandidate>
{
    protected sealed override IEvaluationExecutor<TCandidate> CreateExecutor<TRunSearchState>(ResolutionScope<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>, TRunSearchState> scope) => new Executor(this, scope.SearchSpace);

    public abstract IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace);

    private sealed class Executor(StatelessEvaluator<TCandidate, TSearchSpace> configuration, TSearchSpace searchSpace) : EvaluationExecutor<TCandidate>
    {
        public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random) => configuration.Evaluate(candidates, random, searchSpace);
    }
}

public abstract record StatelessEvaluator<TCandidate> : Evaluator<TCandidate>, IEvaluationExecutor<TCandidate>
{
    protected sealed override IEvaluationExecutor<TCandidate> CreateExecutor<TRunSearchState>(ResolutionScope<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, TRunSearchState> scope) => this;

    public abstract IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random);
}
