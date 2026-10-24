using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Crossovers;

public abstract record StatelessCrossover<TCandidate, TSearchSpace, TProblem>
    : Crossover<TCandidate, TSearchSpace, TProblem>, ICrossoverExecutor<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected sealed override ICrossoverExecutor<TCandidate, TSearchSpace, TProblem> CreateExecutor(ResolutionScope scope) => this;

    public abstract IReadOnlyList<TCandidate> Cross(IReadOnlyList<Parents<TCandidate>> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
}

public abstract record StatelessCrossover<TCandidate, TSearchSpace>
    : Crossover<TCandidate, TSearchSpace>, ICrossoverExecutor<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>
    where TSearchSpace : class, ISearchSpace<TCandidate>
{
    protected sealed override ICrossoverExecutor<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>> CreateExecutor(ResolutionScope scope) => this;

    public abstract IReadOnlyList<TCandidate> Cross(IReadOnlyList<Parents<TCandidate>> parents, IRandomNumberGenerator random, TSearchSpace searchSpace);

    IReadOnlyList<TCandidate> ICrossoverExecutor<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>.Cross(IReadOnlyList<Parents<TCandidate>> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, IProblem<TCandidate, TSearchSpace> problem) =>
        Cross(parents, random, searchSpace);
}

public abstract record StatelessCrossover<TCandidate>
    : Crossover<TCandidate>, ICrossoverExecutor<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>
{
    protected sealed override ICrossoverExecutor<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>> CreateExecutor(ResolutionScope scope) => this;

    public abstract IReadOnlyList<TCandidate> Cross(IReadOnlyList<Parents<TCandidate>> parents, IRandomNumberGenerator random);

    IReadOnlyList<TCandidate> ICrossoverExecutor<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>.Cross(IReadOnlyList<Parents<TCandidate>> parents, IRandomNumberGenerator random, ISearchSpace<TCandidate> searchSpace, IProblem<TCandidate, ISearchSpace<TCandidate>> problem) =>
        Cross(parents, random);
}
