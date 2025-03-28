using HEAL.HeuristicLib.Algorithms.GeneticAlgorithm;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Encodings;

public interface IEncodingParameter { }

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

public interface IEncoding { }

public interface IEncoding<in TGenotype> : IEncoding { }

public interface IEncoding<in TGenotype, out TPhenotype> : IEncoding<TGenotype> {
  IDecoder<TGenotype, TPhenotype> Decoder { get; }
}


public abstract class Encoding<TGenotype, TPhenotype> : IEncoding<TGenotype, TPhenotype> {
  public IDecoder<TGenotype, TPhenotype> Decoder { get; }

  protected Encoding(IDecoder<TGenotype, TPhenotype> decoder) {
    Decoder = decoder;
  }
}

public interface ICreatorProvidingEncoding<TGenotype, in TEncodingParameter> : IEncoding<TGenotype> where TEncodingParameter : IEncodingParameter<TGenotype> {
  ICreator<TGenotype, TEncodingParameter> Creator { get; }
}

public interface ICrossoverProvidingEncoding<TGenotype, in TEncodingParameter> : IEncoding<TGenotype> where TEncodingParameter : IEncodingParameter<TGenotype> {
  ICrossover<TGenotype, TEncodingParameter> Crossover { get; }
}

public interface IMutatorProvidingEncoding<TGenotype, in TEncodingParameter> : IEncoding<TGenotype> where TEncodingParameter : IEncodingParameter<TGenotype> {
  IMutator<TGenotype, TEncodingParameter> Mutator { get; }
}

// ToDo: move to somewhere else since the Encoding should not depend on any algorithm
public static class GeneticAlgorithmBuilderEncodingExtensions {
  public static GeneticAlgorithmBuilder<TGenotype, TPhenotype, TEncodingParameter> UsingEncoding<TGenotype, TPhenotype, TEncodingParameter, TEncoding>(
    this GeneticAlgorithmBuilder builder,
    TEncoding encoding
  )
    where TEncoding : IEncoding<TGenotype, TPhenotype>
    where TEncodingParameter : IEncodingParameter<TGenotype> 
  {
    //var parameterizedBuilder = builder.UsingEncodingParameters<TGenotype, TPhenotype, TEncodingParameter>(encoding.Parameter);
    var parameterizedBuilder = builder.UsingGenotype<TGenotype, TPhenotype, TEncodingParameter>();
    parameterizedBuilder.WithDecoder(encoding.Decoder);
    
    if (encoding is ICreatorProvidingEncoding<TGenotype, TEncodingParameter> creatorProvider)
      parameterizedBuilder.WithCreator(creatorProvider.Creator);
    if (encoding is ICrossoverProvidingEncoding<TGenotype, TEncodingParameter> crossoverProvider)
      parameterizedBuilder.WithCrossover(crossoverProvider.Crossover);
    if (encoding is IMutatorProvidingEncoding<TGenotype, TEncodingParameter> mutatorProvider)
      parameterizedBuilder.WithMutator(mutatorProvider.Mutator);
    
    return parameterizedBuilder;
  }
}
