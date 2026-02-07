using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Crossovers;

public abstract class SingleSolutionCrossover<TGenotype, TSearchSpace, TProblem> 
  : Crossover<TGenotype, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
}

public abstract class SingleSolutionCrossoverInstance<TGenotype, TSearchSpace, TProblem>
  : CrossoverInstance<TGenotype, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  public abstract TGenotype Cross(IParents<TGenotype> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
  
  public override IReadOnlyList<TGenotype> Cross(IReadOnlyList<IParents<TGenotype>> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
    BatchExecution.Sequential(parents, (p, r) => Cross(p, r, searchSpace, problem), random);
}


public abstract class SingleSolutionCrossover<TGenotype, TSearchSpace>
  : Crossover<TGenotype, TSearchSpace>
  where TSearchSpace : class, ISearchSpace<TGenotype>
{
}

public abstract class SingleSolutionCrossoverInstance<TGenotype, TSearchSpace> 
  : CrossoverInstance<TGenotype, TSearchSpace>
  where TSearchSpace : class, ISearchSpace<TGenotype>
{
  public abstract TGenotype Cross(IParents<TGenotype> parents, IRandomNumberGenerator random, TSearchSpace searchSpace);
  
  public override IReadOnlyList<TGenotype> Cross(IReadOnlyList<IParents<TGenotype>> parents, IRandomNumberGenerator random, TSearchSpace searchSpace) =>
    BatchExecution.Sequential(parents, (p, r) => Cross(p, r, searchSpace), random);
}


public abstract class SingleSolutionCrossover<TGenotype>
  : Crossover<TGenotype>
{
}

public abstract class SingleSolutionCrossoverInstance<TGenotype>
  : CrossoverInstance<TGenotype>
{
  public abstract TGenotype Cross(IParents<TGenotype> parents, IRandomNumberGenerator random);
  
  public override IReadOnlyList<TGenotype> Cross(IReadOnlyList<IParents<TGenotype>> parents, IRandomNumberGenerator random) =>
    BatchExecution.Sequential(parents, (p, r) => Cross(p, r), random);
}


// ToDo: Since the stateless versions will be the most commonly used ones, think about naming them "Crossover" and the other ones the "StatefulCrossover".

public abstract class SingleSolutionStatelessCrossover<TGenotype, TSearchSpace, TProblem> 
  : StatelessCrossover<TGenotype, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  public abstract TGenotype Cross(IParents<TGenotype> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
  
  public override IReadOnlyList<TGenotype> Cross(IReadOnlyList<IParents<TGenotype>> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
    BatchExecution.Sequential(parents, (p, r) => Cross(p, r, searchSpace, problem), random);
}

public abstract class SingleSolutionStatelessCrossover<TGenotype, TSearchSpace> 
  : StatelessCrossover<TGenotype, TSearchSpace>
  where TSearchSpace : class, ISearchSpace<TGenotype>
{
  public abstract TGenotype Cross(IParents<TGenotype> parents, IRandomNumberGenerator random, TSearchSpace searchSpace);
  
  public override IReadOnlyList<TGenotype> Cross(IReadOnlyList<IParents<TGenotype>> parents, IRandomNumberGenerator random, TSearchSpace searchSpace) =>
    BatchExecution.Sequential(parents, (p, r) => Cross(p, r, searchSpace), random);
}

public abstract class SingleSolutionStatelessCrossover<TGenotype>
  : StatelessCrossover<TGenotype>
{
  public abstract TGenotype Cross(IParents<TGenotype> parents, IRandomNumberGenerator random);
  
  public override IReadOnlyList<TGenotype> Cross(IReadOnlyList<IParents<TGenotype>> parents, IRandomNumberGenerator random) =>
    BatchExecution.Sequential(parents, (p, r) => Cross(p, r), random);
}
