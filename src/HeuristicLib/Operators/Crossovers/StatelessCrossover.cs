using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Crossovers;

public abstract record StatelessCrossover<TCandidate, TSearchSpace, TProblem>
  : ICrossover<TCandidate, TSearchSpace, TProblem>,
    ICrossoverInstance<TCandidate, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public ICrossoverInstance<TCandidate, TSearchSpace, TProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) => this;

    public abstract IReadOnlyList<TCandidate> Cross(IReadOnlyList<IParents<TCandidate>> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
}

public abstract record StatelessCrossover<TCandidate, TSearchSpace>
  : ICrossover<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>,
    ICrossoverInstance<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>
  where TSearchSpace : class, ISearchSpace<TCandidate>
{
    public ICrossoverInstance<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) => this;

    public abstract IReadOnlyList<TCandidate> Cross(IReadOnlyList<IParents<TCandidate>> parents, IRandomNumberGenerator random, TSearchSpace searchSpace);

    IReadOnlyList<TCandidate> ICrossoverInstance<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>.Cross(IReadOnlyList<IParents<TCandidate>> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, IProblem<TCandidate, TSearchSpace> problem) =>
      Cross(parents, random, searchSpace);
}

public abstract record StatelessCrossover<TCandidate>
  : ICrossover<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>,
    ICrossoverInstance<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>
{
    public ICrossoverInstance<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) => this;

    public abstract IReadOnlyList<TCandidate> Cross(IReadOnlyList<IParents<TCandidate>> parents, IRandomNumberGenerator random);

    IReadOnlyList<TCandidate> ICrossoverInstance<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>.Cross(IReadOnlyList<IParents<TCandidate>> parents, IRandomNumberGenerator random, ISearchSpace<TCandidate> searchSpace, IProblem<TCandidate, ISearchSpace<TCandidate>> problem) =>
      Cross(parents, random);
}
