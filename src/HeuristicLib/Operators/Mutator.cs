using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators;

public interface IMutator<TGenotype, in TEncoding, in TProblem> 
  where TEncoding : IEncoding<TGenotype>
  where TProblem : IProblem<TGenotype, TEncoding>
{
  IReadOnlyList<TGenotype> Mutate(IReadOnlyList<TGenotype> parent, IRandomNumberGenerator random, TEncoding encoding, TProblem problem);
}

public interface IMutator<TGenotype, in TEncoding> : IMutator<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>>
  where TEncoding : IEncoding<TGenotype>
{
  IReadOnlyList<TGenotype> Mutate(IReadOnlyList<TGenotype> parent, IRandomNumberGenerator random, TEncoding encoding);
}

public interface IMutator<TGenotype> : IMutator<TGenotype, IEncoding<TGenotype>>
{
  IReadOnlyList<TGenotype> Mutate(IReadOnlyList<TGenotype> parent, IRandomNumberGenerator random);
}


public abstract class BatchMutator<TGenotype, TEncoding, TProblem> : IMutator<TGenotype, TEncoding, TProblem>
  where TEncoding : IEncoding<TGenotype>
  where TProblem : IProblem<TGenotype, TEncoding>
{
  public abstract IReadOnlyList<TGenotype> Mutate(IReadOnlyList<TGenotype> parent, IRandomNumberGenerator random, TEncoding encoding, TProblem problem);
}

public abstract class BatchMutator<TGenotype, TEncoding> : IMutator<TGenotype, TEncoding>
  where TEncoding : IEncoding<TGenotype>
{
  public abstract IReadOnlyList<TGenotype> Mutate(IReadOnlyList<TGenotype> parent, IRandomNumberGenerator random, TEncoding encoding);

  IReadOnlyList<TGenotype> IMutator<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>>.Mutate(IReadOnlyList<TGenotype> parent, IRandomNumberGenerator random, TEncoding encoding, IProblem<TGenotype, TEncoding> problem) {
    return Mutate(parent, random, encoding);
  }
}

public abstract class BatchMutator<TGenotype> : IMutator<TGenotype>
{
  public abstract IReadOnlyList<TGenotype> Mutate(IReadOnlyList<TGenotype> parent, IRandomNumberGenerator random);

  IReadOnlyList<TGenotype> IMutator<TGenotype, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>>.Mutate(IReadOnlyList<TGenotype> parent, IRandomNumberGenerator random, IEncoding<TGenotype> encoding, IProblem<TGenotype, IEncoding<TGenotype>> problem) {
    return Mutate(parent, random);
  }
  IReadOnlyList<TGenotype> IMutator<TGenotype, IEncoding<TGenotype>>.Mutate(IReadOnlyList<TGenotype> parent, IRandomNumberGenerator random, IEncoding<TGenotype> encoding) {
    return Mutate(parent, random);
  }
}


public abstract class Mutator<TGenotype, TEncoding, TProblem> : IMutator<TGenotype, TEncoding, TProblem>
  where TEncoding : IEncoding<TGenotype>
  where TProblem : IProblem<TGenotype, TEncoding>
{
  public abstract TGenotype Mutate(TGenotype parent, IRandomNumberGenerator random, TEncoding encoding, TProblem problem);

  IReadOnlyList<TGenotype> IMutator<TGenotype, TEncoding, TProblem>.Mutate(IReadOnlyList<TGenotype> parent, IRandomNumberGenerator random, TEncoding encoding, TProblem problem) {
    var offspring = new TGenotype[parent.Count];
    Parallel.For(0, parent.Count, i => {
      offspring[i] = Mutate(parent[i], random, encoding, problem);
    });
    return offspring;
  }
}

public abstract class Mutator<TGenotype, TEncoding> : IMutator<TGenotype, TEncoding>
  where TEncoding : IEncoding<TGenotype>
{
  public abstract TGenotype Mutate(TGenotype parent, IRandomNumberGenerator random, TEncoding encoding);

  public IReadOnlyList<TGenotype> Mutate(IReadOnlyList<TGenotype> parent, IRandomNumberGenerator random, TEncoding encoding) {
    var offspring = new TGenotype[parent.Count];
    Parallel.For(0, parent.Count, i => {
      offspring[i] = Mutate(parent[i], random, encoding);
    });
    return offspring;
  }
  
  IReadOnlyList<TGenotype> IMutator<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>>.Mutate(IReadOnlyList<TGenotype> parent, IRandomNumberGenerator random, TEncoding encoding, IProblem<TGenotype, TEncoding> problem) {
    return Mutate(parent, random, encoding);
  }
}

public abstract class Mutator<TGenotype> : IMutator<TGenotype>
{
  public abstract TGenotype Mutate(TGenotype parent, IRandomNumberGenerator random);

  public IReadOnlyList<TGenotype> Mutate(IReadOnlyList<TGenotype> parent, IRandomNumberGenerator random) {
    var offspring = new TGenotype[parent.Count];
    Parallel.For(0, parent.Count, i => {
      offspring[i] = Mutate(parent[i], random);
    });
    return offspring;
  }
  
  IReadOnlyList<TGenotype> IMutator<TGenotype, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>>.Mutate(IReadOnlyList<TGenotype> parent, IRandomNumberGenerator random, IEncoding<TGenotype> encoding, IProblem<TGenotype, IEncoding<TGenotype>> problem) {
    return Mutate(parent, random);
  }
  IReadOnlyList<TGenotype> IMutator<TGenotype, IEncoding<TGenotype>>.Mutate(IReadOnlyList<TGenotype> parent, IRandomNumberGenerator random, IEncoding<TGenotype> encoding) {
    return Mutate(parent, random);
  }
}


public class NoChangeMutator<TGenotype> : BatchMutator<TGenotype>
{
  public override IReadOnlyList<TGenotype> Mutate(IReadOnlyList<TGenotype> parent, IRandomNumberGenerator random) {
    return parent;
  }
}


