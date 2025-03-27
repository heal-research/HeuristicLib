using HEAL.HeuristicLib.Algorithms.GeneticAlgorithm;
using HEAL.HeuristicLib.Core;
using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Problems;

public interface IProblem { }

public interface IProblem<TPhenotype, TGenotype> : IProblem {
  Fitness Evaluate(TPhenotype solution);
  Objective Objective { get; } 
  IGenotypeMapper<TGenotype, TPhenotype> Decoder { get; }
}

public interface IGenotypeMapper<TGenotype, TPhenotype> {
  TPhenotype Decode(TGenotype genotype);
  TGenotype Encode(TPhenotype solution);
}

public class GenotypeMapper<TGenotype, TSolution> : IGenotypeMapper<TGenotype, TSolution> {
  private readonly Func<TGenotype, TSolution> decoder;
  private readonly Func<TSolution, TGenotype> encoder;
  public GenotypeMapper(Func<TGenotype, TSolution> decoder, Func<TSolution, TGenotype> encoder) {
    this.decoder = decoder;
    this.encoder = encoder;
  }
  public TSolution Decode(TGenotype genotype) => decoder(genotype);
  public TGenotype Encode(TSolution solution) => encoder(solution);
}

public static class GenotypeMapper {
  public static IGenotypeMapper<T, T> Identity<T>() => new GenotypeMapper<T, T>(x => x, x => x);
}

public abstract class ProblemBase<TSolution, TGenotype> : IProblem<TSolution, TGenotype> {
  public abstract Fitness Evaluate(TSolution solution);
  public IGenotypeMapper<TGenotype, TSolution> Decoder { get; }
  public Objective Objective { get; }
  
  protected ProblemBase(IGenotypeMapper<TGenotype, TSolution> mapper, Objective objective) {
    Decoder = mapper;
    Objective = objective;
  }
}

public abstract class ProblemBase<TGenotype> : ProblemBase<TGenotype, TGenotype> {
  protected ProblemBase(Objective objective) : base(GenotypeMapper.Identity<TGenotype>(), objective) {}
}


public static class GeneticAlgorithmBuilderUsingProblemExtensions {
  public static TBuilder WithFitnessFunctionFromProblem<TBuilder, TGenotype, TPhenotype, TEncodingParameter>(this TBuilder builder, IProblem<TPhenotype, TGenotype> problem)
    where TBuilder : IGeneticAlgorithmBuilder<TGenotype, TPhenotype, TEncodingParameter, TBuilder>
    where TEncodingParameter : IEncodingParameter<TGenotype>
  {
    return builder
      .WithFitnessFunction<TBuilder, TGenotype, TPhenotype, TEncodingParameter>(problem.Evaluate, problem.Decoder)
      .WithObjective(problem.Objective);
  }
  
  public static TBuilder WithFitnessFunction<TBuilder, TGenotype, TPhenotype, TEncodingParameter>(this TBuilder builder, Func<TPhenotype, Fitness> fitnessFunction, IGenotypeMapper<TGenotype, TPhenotype> mapper)
    where TBuilder : IGeneticAlgorithmBuilder<TGenotype, TPhenotype, TEncodingParameter, TBuilder>
    where TEncodingParameter : IEncodingParameter<TGenotype>
  {
    var evaluator = Evaluator.UsingFitnessFunction(fitnessFunction, mapper);
    return builder.WithEvaluator(evaluator);
    //return builder.WithFitnessFunction<TBuilder, TGenotype, TPhenotype, TEncodingParameter>(genotype => fitnessFunction(mapper.Decode(genotype)));
  }
  
  // public static TBuilder WithFitnessFunction<TBuilder, TGenotype, TPhenotype, TEncodingParameter>(this TBuilder builder, Func<TGenotype, Fitness> fitnessFunction)
  //   where TBuilder : IGeneticAlgorithmBuilder<TGenotype, TPhenotype, TEncodingParameter, TBuilder>
  //   where TEncodingParameter : IEncodingParameter<TGenotype>
  // {
  //   FitnessFunctionEvaluator<TGenotype, TGenotype> evaluator = Evaluator.UsingFitnessFunction(fitnessFunction);
  //   return builder.WithEvaluator(evaluator);
  // }
}
