using HEAL.HeuristicLib.Algorithms.GeneticAlgorithm;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Encodings;

public interface IEncoding { }

public interface IEncoding<in TGenotype> : IEncoding {
  bool IsValidGenotype(TGenotype genotype);
  //bool IsValidOperator(IEncodingOperator<TGenotype, TSelf> @operator);
}

public abstract record class EncodingBase<TGenotype> : IEncoding<TGenotype> {
  protected EncodingBase() { }
  public abstract bool IsValidGenotype(TGenotype genotype);
  // public virtual bool IsValidOperator(IEncodingOperator<TGenotype, TSelf> @operator) {
  //   return @operator.Encoding.Equals(this);
  // }
}

public interface IEncodingProvider<TGenotype, out TEncoding> 
  where TEncoding : IEncoding<TGenotype> 
{
  TEncoding GetEncoding();
}

public interface ICreatorProvider<out TGenotype, in TEncoding> where TEncoding : IEncoding<TGenotype> {
  ICreator<TGenotype, TEncoding> Creator { get; }
}

public interface ICrossoverProvider<TGenotype, in TEncoding> where TEncoding : IEncoding<TGenotype> {
  ICrossover<TGenotype, TEncoding> Crossover { get; }
}

public interface IMutatorProvider<TGenotype, in TEncoding> where TEncoding : IEncoding<TGenotype> {
  IMutator<TGenotype, TEncoding> Mutator { get; }
}

// ToDo: move to somewhere else since the Encoding should not depend on any algorithm
public static class GeneticAlgorithmBuilderEncodingExtensions {
  public static GeneticAlgorithmBuilder<TGenotype, TPhenotype, TEncoding> UsingEncoding<TGenotype, TPhenotype, TEncoding>(
    this GeneticAlgorithmBuilder builder,
    TEncoding encoding
  )
    where TEncoding : IEncoding<TGenotype>
  {
    var parameterizedBuilder = builder
      .UsingEncodingType<TGenotype, TPhenotype, TEncoding>()
      .WithEncoding(encoding);
    // var parameterizedBuilder = builder.UsingGenotype<TGenotype, TPhenotype, TEncoding>();
    //parameterizedBuilder.WithDecoder(encoding.Decoder);
    
    // if (encoding is ICreatorProvider<TGenotype, TEncoding> creatorProvider)
    //   parameterizedBuilder.WithCreator(creatorProvider.Creator);
    // if (encoding is ICrossoverProvider<TGenotype, TEncoding> crossoverProvider)
    //   parameterizedBuilder.WithCrossover(crossoverProvider.Crossover);
    // if (encoding is IMutatorProvider<TGenotype, TEncoding> mutatorProvider)
    //   parameterizedBuilder.WithMutator(mutatorProvider.Mutator);
    
    return parameterizedBuilder;
  }
}
