using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Mutators;

// ToDo: Think about renaming this to "StatelessMutator"

public abstract record Mutator<TGenotype, TSearchSpace, TProblem>
  : IMutator<TGenotype, TSearchSpace, TProblem>,
    IMutatorInstance<TGenotype, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  public IMutatorInstance<TGenotype, TSearchSpace, TProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) => this;

  public abstract IReadOnlyList<TGenotype> Mutate(IReadOnlyList<TGenotype> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
}

public abstract record Mutator<TGenotype, TSearchSpace>
  : IMutator<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>>,
    IMutatorInstance<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>>
  where TSearchSpace : class, ISearchSpace<TGenotype>
{
  public IMutatorInstance<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) => this;

  public abstract IReadOnlyList<TGenotype> Mutate(IReadOnlyList<TGenotype> parents, IRandomNumberGenerator random, TSearchSpace searchSpace);

   IReadOnlyList<TGenotype> IMutatorInstance<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>>.Mutate(IReadOnlyList<TGenotype> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, IProblem<TGenotype, TSearchSpace> problem) =>
      Mutate(parents, random, searchSpace);
}

public abstract record Mutator<TGenotype>
  : IMutator<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>,
    IMutatorInstance<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>
{
  public IMutatorInstance<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) => this;

  public abstract IReadOnlyList<TGenotype> Mutate(IReadOnlyList<TGenotype> parents, IRandomNumberGenerator random);

  IReadOnlyList<TGenotype> IMutatorInstance<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>.Mutate(IReadOnlyList<TGenotype> parents, IRandomNumberGenerator random, ISearchSpace<TGenotype> searchSpace, IProblem<TGenotype, ISearchSpace<TGenotype>> problem) =>
    Mutate(parents, random);
}
