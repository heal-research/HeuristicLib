using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Crossovers;

public abstract record StatelessCrossover<TCandidate, TSearchSpace, TProblem>
    : Crossover<TCandidate, TSearchSpace, TProblem>, ICrossoverInstance<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected sealed override ICrossoverInstance<TCandidate, TSearchSpace, TProblem> CreateCrossoverInstance(ExecutionInstanceRegistry registry) => this;

    public abstract IReadOnlyList<TCandidate> Cross(IReadOnlyList<Parents<TCandidate>> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
}

public abstract record StatelessCrossover<TCandidate, TSearchSpace>
    : Crossover<TCandidate, TSearchSpace>, ICrossoverInstance<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>
    where TSearchSpace : class, ISearchSpace<TCandidate>
{
    protected sealed override ICrossoverInstance<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>> CreateCrossoverInstance(ExecutionInstanceRegistry registry) => this;

    public abstract IReadOnlyList<TCandidate> Cross(IReadOnlyList<Parents<TCandidate>> parents, IRandomNumberGenerator random, TSearchSpace searchSpace);

    IReadOnlyList<TCandidate> ICrossoverInstance<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>.Cross(IReadOnlyList<Parents<TCandidate>> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, IProblem<TCandidate, TSearchSpace> problem) =>
        Cross(parents, random, searchSpace);
}

public abstract record StatelessCrossover<TCandidate>
    : Crossover<TCandidate>, ICrossoverInstance<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>
{
    protected sealed override ICrossoverInstance<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>> CreateCrossoverInstance(ExecutionInstanceRegistry registry) => this;

    public abstract IReadOnlyList<TCandidate> Cross(IReadOnlyList<Parents<TCandidate>> parents, IRandomNumberGenerator random);

    IReadOnlyList<TCandidate> ICrossoverInstance<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>.Cross(IReadOnlyList<Parents<TCandidate>> parents, IRandomNumberGenerator random, ISearchSpace<TCandidate> searchSpace, IProblem<TCandidate, ISearchSpace<TCandidate>> problem) =>
        Cross(parents, random);
}
