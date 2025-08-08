using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators;

public interface IMutator<TGenotype, in TEncoding, in TProblem> 
  where TEncoding : IEncoding<TGenotype>
  where TProblem : IProblem<TGenotype, TEncoding>
{
  TGenotype Mutate(TGenotype parent, IRandomNumberGenerator random, TEncoding encoding, TProblem problem);
}

public abstract class Mutator<TGenotype, TEncoding, TProblem> : IMutator<TGenotype, TEncoding, TProblem>
  where TEncoding : IEncoding<TGenotype>
  where TProblem : IProblem<TGenotype, TEncoding>
{
  public abstract TGenotype Mutate(TGenotype parent, IRandomNumberGenerator random, TEncoding encoding, TProblem problem);
}

public abstract class Mutator<TGenotype, TEncoding> : IMutator<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>>
  where TEncoding : IEncoding<TGenotype>
{
  public abstract TGenotype Mutate(TGenotype parent, IRandomNumberGenerator random, TEncoding encoding);
  
  TGenotype IMutator<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>>.Mutate(TGenotype parent, IRandomNumberGenerator random, TEncoding encoding, IProblem<TGenotype, TEncoding> problem) {
    return Mutate(parent, random, encoding);
  }
}

public abstract class Mutator<TGenotype> : IMutator<TGenotype, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>>
{
  public abstract TGenotype Mutate(TGenotype parent, IRandomNumberGenerator random);
  
  TGenotype IMutator<TGenotype, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>>.Mutate(TGenotype parent, IRandomNumberGenerator random, IEncoding<TGenotype> encoding, IProblem<TGenotype, IEncoding<TGenotype>> problem) {
    return Mutate(parent, random);
  }
}


public class NoChangeMutator<TGenotype> : Mutator<TGenotype>
{
  public override TGenotype Mutate(TGenotype parent, IRandomNumberGenerator random) {
    return parent;
  }
}


