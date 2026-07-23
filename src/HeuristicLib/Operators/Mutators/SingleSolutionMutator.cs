using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Mutators;

public abstract record SingleSolutionMutator<TCandidate, TSearchSpace, TProblem>
  : StatelessMutator<TCandidate, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public abstract TCandidate Mutate(TCandidate parent, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

    public override IReadOnlyList<TCandidate> Mutate(IReadOnlyList<TCandidate> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
      BatchExecution.Sequential(parents, (p, r) => Mutate(p, r, searchSpace, problem), random);
}

public abstract record SingleSolutionMutator<TCandidate, TSearchSpace>
  : StatelessMutator<TCandidate, TSearchSpace>
  where TSearchSpace : class, ISearchSpace<TCandidate>
{
    public abstract TCandidate Mutate(TCandidate parent, IRandomNumberGenerator random, TSearchSpace searchSpace);

    public override IReadOnlyList<TCandidate> Mutate(IReadOnlyList<TCandidate> parents, IRandomNumberGenerator random, TSearchSpace searchSpace) =>
      BatchExecution.Sequential(parents, (p, r) => Mutate(p, r, searchSpace), random);
}

public abstract record SingleSolutionMutator<TCandidate>
  : StatelessMutator<TCandidate>
{
    public abstract TCandidate Mutate(TCandidate parent, IRandomNumberGenerator random);

    public override IReadOnlyList<TCandidate> Mutate(IReadOnlyList<TCandidate> parents, IRandomNumberGenerator random) =>
      BatchExecution.Sequential(parents, (p, r) => Mutate(p, r), random);
}
