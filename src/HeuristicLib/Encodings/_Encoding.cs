using HEAL.HeuristicLib.Algorithms.GeneticAlgorithm;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;

namespace HEAL.HeuristicLib.Encodings;

public interface IEncodingParameter { }

public interface IEncoding { }

public interface IEncoding<TGenotype> { }


public interface IEncoding<TGenotype, out TEncodingParameter> : IEncoding<TGenotype> 
  where TEncodingParameter : IEncodingParameter<TGenotype> 
{
  TEncodingParameter Parameter { get; }
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

public abstract class Encoding<TGenotype, TEncodingParameter> : IEncoding<TGenotype, TEncodingParameter>
  where TEncodingParameter : IEncodingParameter<TGenotype>
{
  public TEncodingParameter Parameter { get; }

  protected Encoding(TEncodingParameter parameter) {
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

public static class GeneticAlgorithmBuilderEncodingExtensions {
  public static GeneticAlgorithmBuilder<TGenotype, TEncodingParameter> UsingEncoding<TGenotype, TEncodingParameter, TEncoding>(
    this GeneticAlgorithmBuilder<TGenotype> builder,
    TEncoding encoding
  )
    where TEncoding : IEncoding<TGenotype, TEncodingParameter>
    where TEncodingParameter : IEncodingParameter<TGenotype> 
  {
    var parameterizedBuilder = builder.UsingEncodingParameters(encoding.Parameter);
    
    if (encoding is ICreatorProvider<TGenotype> creatorProvider)
      parameterizedBuilder.WithCreator(creatorProvider.Creator);
    if (encoding is ICrossoverProvider<TGenotype> crossoverProvider)
      parameterizedBuilder.WithCrossover(crossoverProvider.Crossover);
    if (encoding is IMutatorProvider<TGenotype> mutatorProvider)
      parameterizedBuilder.WithMutator(mutatorProvider.Mutator);
    
    return parameterizedBuilder;
  }
}
