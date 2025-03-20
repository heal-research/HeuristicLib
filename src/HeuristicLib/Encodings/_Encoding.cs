using HEAL.HeuristicLib.Algorithms.GeneticAlgorithm;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;

namespace HEAL.HeuristicLib.Encodings;

public interface IEncodingParameter { }

public interface IEncoding { }

public interface IEncoding<TGenotype> { }


public interface IEncoding<TGenotype, out TParameters> : IEncoding<TGenotype> where TParameters : IEncodingParameter<TGenotype> {
  TParameters Parameter { get; }
}


public interface IEncodingParameter<in TGenotype> : IEncodingParameter {
  bool IsValidGenotype(TGenotype genotype);
  //bool IsValidOperator(IEncodingOperator<TGenotype, TSelf> @operator);
}

public abstract record class EncodingParameterBase<TGenotype> : IEncodingParameter<TGenotype> {
  protected EncodingParameterBase() { }
  public abstract bool IsValidGenotype(TGenotype genotype);
  // public virtual bool IsValidOperator(IEncodingOperator<TGenotype, TSelf> @operator) {
  //   return @operator.Encoding.Equals(this);
  // }
}

public abstract class Encoding<TGenotype, TParameters> : IEncoding<TGenotype, TParameters>
  where TParameters : IEncodingParameter<TGenotype>
{
  public TParameters Parameter { get; }

  protected Encoding(TParameters parameter) {
    Parameter = parameter;
  }
}

public interface ICreatorProvider<TGenotype> : IEncoding<TGenotype> {
  ICreator<TGenotype> Creator { get; }
}

public interface ICrossoverProvider<TGenotype> : IEncoding<TGenotype> {
  ICrossover<TGenotype> Crossover { get; }
}

public interface IMutatorProvider<TGenotype> : IEncoding<TGenotype> {
  IMutator<TGenotype> Mutator { get; }
}

public static class GeneticAlgorithmBuilderUsingEncodingExtensions {
  public static IGeneticAlgorithmBuilder<TGenotype> UsingEncoding<TGenotype, TEncoding>(this IGeneticAlgorithmBuilder<TGenotype> builder, TEncoding encoding) where TEncoding : IEncoding<TGenotype> {
    if (encoding is ICreatorProvider<TGenotype> creatorProvider)
      builder.WithCreator(creatorProvider.Creator);
    if (encoding is ICrossoverProvider<TGenotype> crossoverProvider)
      builder.WithCrossover(crossoverProvider.Crossover);
    if (encoding is IMutatorProvider<TGenotype> mutatorProvider)
      builder.WithMutator(mutatorProvider.Mutator);
    
    return builder;
  }
}
