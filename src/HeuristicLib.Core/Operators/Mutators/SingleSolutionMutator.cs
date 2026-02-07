using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Mutators;

public abstract class SingleSolutionMutator<TGenotype, TSearchSpace, TProblem> 
  : Mutator<TGenotype, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
}

public abstract class SingleSolutionMutatorInstance<TGenotype, TSearchSpace, TProblem>
  : MutatorInstance<TGenotype, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  public abstract TGenotype Mutate(TGenotype parent, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
  
  public override IReadOnlyList<TGenotype> Mutate(IReadOnlyList<TGenotype> parent, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
    BatchExecution.Sequential(parent, (p, r) => Mutate(p, r, searchSpace, problem), random);
}


public abstract class SingleSolutionMutator<TGenotype, TSearchSpace>
  : Mutator<TGenotype, TSearchSpace>
  where TSearchSpace : class, ISearchSpace<TGenotype>
{
}

public abstract class SingleSolutionMutatorInstance<TGenotype, TSearchSpace> 
  : MutatorInstance<TGenotype, TSearchSpace>
  where TSearchSpace : class, ISearchSpace<TGenotype>
{
  public abstract TGenotype Mutate(TGenotype parent, IRandomNumberGenerator random, TSearchSpace searchSpace);
  
  public override IReadOnlyList<TGenotype> Mutate(IReadOnlyList<TGenotype> parent, IRandomNumberGenerator random, TSearchSpace searchSpace) =>
    BatchExecution.Sequential(parent, (p, r) => Mutate(p, r, searchSpace), random);
}


public abstract class SingleSolutionMutator<TGenotype>
  : Mutator<TGenotype>
{
}

public abstract class SingleSolutionMutatorInstance<TGenotype>
  : MutatorInstance<TGenotype>
{
  public abstract TGenotype Mutate(TGenotype parent, IRandomNumberGenerator random);
  
  public override IReadOnlyList<TGenotype> Mutate(IReadOnlyList<TGenotype> parent, IRandomNumberGenerator random) =>
    BatchExecution.Sequential(parent, (p, r) => Mutate(p, r), random);
}


public abstract class SingleSolutionStatelessMutator<TGenotype, TSearchSpace, TProblem> 
  : StatelessMutator<TGenotype, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  public abstract TGenotype Mutate(TGenotype parent, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
  
  public override IReadOnlyList<TGenotype> Mutate(IReadOnlyList<TGenotype> parent, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
    BatchExecution.Sequential(parent, (p, r) => Mutate(p, r, searchSpace, problem), random);
}

public abstract class SingleSolutionStatelessMutator<TGenotype, TSearchSpace> 
  : StatelessMutator<TGenotype, TSearchSpace>
  where TSearchSpace : class, ISearchSpace<TGenotype>
{
  public abstract TGenotype Mutate(TGenotype parent, IRandomNumberGenerator random, TSearchSpace searchSpace);
  
  public override IReadOnlyList<TGenotype> Mutate(IReadOnlyList<TGenotype> parent, IRandomNumberGenerator random, TSearchSpace searchSpace) =>
    BatchExecution.Sequential(parent, (p, r) => Mutate(p, r, searchSpace), random);
}

public abstract class SingleSolutionStatelessMutator<TGenotype>
  : StatelessMutator<TGenotype>
{
  public abstract TGenotype Mutate(TGenotype parent, IRandomNumberGenerator random);
  
  public override IReadOnlyList<TGenotype> Mutate(IReadOnlyList<TGenotype> parent, IRandomNumberGenerator random) =>
    BatchExecution.Sequential(parent, (p, r) => Mutate(p, r), random);
}
