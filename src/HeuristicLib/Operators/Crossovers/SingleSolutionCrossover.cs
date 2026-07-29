using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Crossovers;

public abstract record SingleSolutionCrossover<TCandidate, TSearchSpace, TProblem>
  : StatelessCrossover<TCandidate, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public abstract TCandidate Cross(Parents<TCandidate> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

    public override IReadOnlyList<TCandidate> Cross(IReadOnlyList<Parents<TCandidate>> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
      BatchExecution.Sequential(parents, (p, r) => Cross(p, r, searchSpace, problem), random);
}

public abstract record SingleSolutionCrossover<TCandidate, TSearchSpace>
  : StatelessCrossover<TCandidate, TSearchSpace>
  where TSearchSpace : class, ISearchSpace<TCandidate>
{
    public abstract TCandidate Cross(Parents<TCandidate> parents, IRandomNumberGenerator random, TSearchSpace searchSpace);

    public override IReadOnlyList<TCandidate> Cross(IReadOnlyList<Parents<TCandidate>> parents, IRandomNumberGenerator random, TSearchSpace searchSpace) =>
      BatchExecution.Sequential(parents, (p, r) => Cross(p, r, searchSpace), random);
}

public abstract record SingleSolutionCrossover<TCandidate>
  : StatelessCrossover<TCandidate>
{
    public abstract TCandidate Cross(Parents<TCandidate> parents, IRandomNumberGenerator random);

    public override IReadOnlyList<TCandidate> Cross(IReadOnlyList<Parents<TCandidate>> parents, IRandomNumberGenerator random) =>
      BatchExecution.Sequential(parents, (p, r) => Cross(p, r), random);
}
