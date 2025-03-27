using HEAL.HeuristicLib.Algorithms.GeneticAlgorithm;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;

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

public interface IEncoding<TGenotype, out TEncodingParameter>
  : IEncoding
  where TEncodingParameter : IEncodingParameter<TGenotype>
{
  TEncodingParameter Parameter { get; }
}

public interface IEncoding<TGenotype, out TEncodingParameter, TPhenotype> : IEncoding<TGenotype, TEncodingParameter> 
  where TEncodingParameter : IEncodingParameter<TGenotype> 
{
  IGenotypeMapper<TGenotype, TPhenotype> Decoder { get; }
}

public abstract class Encoding<TGenotype, TEncodingParameter, TPhenotype> 
  : IEncoding<TGenotype, TEncodingParameter, TPhenotype>
  where TEncodingParameter : IEncodingParameter<TGenotype>
{
  public TEncodingParameter Parameter { get; }
  public IGenotypeMapper<TGenotype, TPhenotype> Decoder { get; }

  protected Encoding(TEncodingParameter parameter, IGenotypeMapper<TGenotype, TPhenotype> decoder) {
    Parameter = parameter;
    Decoder = decoder;
  }
}

public interface ICreatorProvider<TGenotype, TEncodingParameter> : IEncoding<TGenotype, TEncodingParameter> where TEncodingParameter : IEncodingParameter<TGenotype> {
  ICreator<TGenotype, TEncodingParameter> Creator { get; }
}

public interface ICrossoverProvider<TGenotype, TEncodingParameter> : IEncoding<TGenotype, TEncodingParameter> where TEncodingParameter : IEncodingParameter<TGenotype> {
  ICrossover<TGenotype, TEncodingParameter> Crossover { get; }
}

public interface IMutatorProvider<TGenotype, TEncodingParameter> : IEncoding<TGenotype, TEncodingParameter> where TEncodingParameter : IEncodingParameter<TGenotype> {
  IMutator<TGenotype, TEncodingParameter> Mutator { get; }
}

// ToDo: move to somewhere else since the Encoding should not depend on any algorithm
public static class GeneticAlgorithmBuilderEncodingExtensions {
  public static GeneticAlgorithmBuilder<TGenotype, TPhenotype, TEncodingParameter> UsingEncoding<TGenotype, TPhenotype, TEncodingParameter, TEncoding>(
    this GeneticAlgorithmBuilder builder,
    TEncoding encoding
  )
    where TEncoding : IEncoding<TGenotype, TEncodingParameter>
    where TEncodingParameter : IEncodingParameter<TGenotype> 
  {
    var parameterizedBuilder = builder.UsingEncodingParameters<TGenotype, TPhenotype, TEncodingParameter>(encoding.Parameter);
    
    if (encoding is ICreatorProvider<TGenotype, TEncodingParameter> creatorProvider)
      parameterizedBuilder.WithCreator(creatorProvider.Creator);
    if (encoding is ICrossoverProvider<TGenotype, TEncodingParameter> crossoverProvider)
      parameterizedBuilder.WithCrossover(crossoverProvider.Crossover);
    if (encoding is IMutatorProvider<TGenotype, TEncodingParameter> mutatorProvider)
      parameterizedBuilder.WithMutator(mutatorProvider.Mutator);
    
    return parameterizedBuilder;
  }
}
