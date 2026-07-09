using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Creators;

public abstract record SingleSolutionCreator<TCandidate, TSearchSpace, TProblem>
  : StatelessCreator<TCandidate, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public abstract TCandidate Create(IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

    public override IReadOnlyList<TCandidate> Create(int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
      BatchExecution.Sequential(count, r => Create(r, searchSpace, problem), random);
}

public abstract record SingleSolutionCreator<TCandidate, TSearchSpace>
  : StatelessCreator<TCandidate, TSearchSpace>
  where TSearchSpace : class, ISearchSpace<TCandidate>
{
    public abstract TCandidate Create(IRandomNumberGenerator random, TSearchSpace searchSpace);

    public override IReadOnlyList<TCandidate> Create(int count, IRandomNumberGenerator random, TSearchSpace searchSpace) =>
      BatchExecution.Sequential(count, r => Create(r, searchSpace), random);
}

public abstract record SingleSolutionCreator<TCandidate>
  : StatelessCreator<TCandidate>
{
    public abstract TCandidate Create(IRandomNumberGenerator random);

    public override IReadOnlyList<TCandidate> Create(int count, IRandomNumberGenerator random) =>
      BatchExecution.Sequential(count, r => Create(r), random);
}
