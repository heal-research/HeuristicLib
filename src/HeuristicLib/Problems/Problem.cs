using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.GeneticAlgorithm;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Problems;

public interface IProblem { }

public interface IProblem<TSolution, TGenotype> : IProblem {
  Fitness Evaluate(TSolution solution);
  Objective Objective { get; } 
  IGenotypeMapper<TGenotype, TSolution> Mapper { get; }
}

public interface IGenotypeMapper<TGenotype, TSolution> {
  TSolution Decode(TGenotype genotype);
  TGenotype Encode(TSolution solution);
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
  public IGenotypeMapper<TGenotype, TSolution> Mapper { get; }
  public Objective Objective { get; }
  
  protected ProblemBase(IGenotypeMapper<TGenotype, TSolution> mapper, Objective objective) {
    Mapper = mapper;
    Objective = objective;
  }
}

public abstract class ProblemBase<TGenotype> : ProblemBase<TGenotype, TGenotype> {
  protected ProblemBase(Objective objective) : base(GenotypeMapper.Identity<TGenotype>(), objective) {}
}

public static class GeneticAlgorithmBuilderUsingProblemExtensions {
  public static TBuilder WithFitnessFunctionFromProblem<TBuilder,TGenotype, TSolution>(this TBuilder builder, IProblem<TSolution, TGenotype> problem)
    where TBuilder : IGeneticAlgorithmBuilder<TGenotype, TBuilder> {
    return builder
      .WithFitnessFunction(problem.Evaluate, problem.Mapper)
      .WithObjective(problem.Objective);
  }
  
  public static TBuilder WithFitnessFunction<TBuilder, TGenotype, TSolution>(this TBuilder builder, Func<TSolution, Fitness> fitnessFunction, IGenotypeMapper<TGenotype, TSolution> mapper)
    where TBuilder : IGeneticAlgorithmBuilder<TGenotype, TBuilder> {
    return builder.WithFitnessFunction<TBuilder, TGenotype>(solution => fitnessFunction(mapper.Decode(solution)));
  }
  
  public static TBuilder WithFitnessFunction<TBuilder, TGenotype>(this TBuilder builder, Func<TGenotype, Fitness> fitnessFunction)
    where TBuilder : IGeneticAlgorithmBuilder<TGenotype, TBuilder> {
    var evaluator = EvaluatorOperator.UsingFitnessFunction(fitnessFunction);
    return builder.WithEvaluator(evaluator);
  }
}
