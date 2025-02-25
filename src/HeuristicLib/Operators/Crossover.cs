using HEAL.HeuristicLib.Encodings;

namespace HEAL.HeuristicLib.Operators;

public interface ICrossover<TEncoding, TGenotype> : IEncodingSpecificOperator<TEncoding, TGenotype> 
  where TEncoding : IEncoding<TGenotype>
{
  TGenotype Crossover(TGenotype parent1, TGenotype parent2);
}

public abstract class CrossoverBase<TEncoding, TGenotype>
  : EncodingSpecificOperator<TEncoding, TGenotype>, ICrossover<TEncoding, TGenotype>
  where TEncoding : IEncoding<TGenotype>
{
  protected CrossoverBase(TEncoding encoding) : base(encoding) {
  }
  
  public TGenotype Crossover(TGenotype parent1, TGenotype parent2) {
    throw new NotImplementedException();
  }
  
}
