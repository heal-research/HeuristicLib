using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.GeneticAlgorithm;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Problems;

public interface IProblem { }

public interface IProblem<TSolution, TGenotype, out TFitness, out TGoal> : IProblem {
  TFitness Evaluate(TSolution solution);
  TGoal Goal { get; } 
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


public abstract class ProblemBase<TSolution, TGenotype, TFitness, TGoal> : IProblem<TSolution, TGenotype, TFitness, TGoal> {
  public abstract TFitness Evaluate(TSolution solution);
  public IGenotypeMapper<TGenotype, TSolution> Mapper { get; }
  public TGoal Goal { get; }
  
  protected ProblemBase(IGenotypeMapper<TGenotype, TSolution> mapper, TGoal goal) {
    Mapper = mapper;
    Goal = goal;
  }
}

public static class GeneticAlgorithmBuilderUsingProblemExtensions {
  public static IGeneticAlgorithmBuilder<TGenotype> UsingProblem<TGenotype, TSolution>(this IGeneticAlgorithmBuilder<TGenotype> builder,
    IProblem<TSolution, TGenotype, Fitness, Goal> problem) 
  {
    return builder.UsingFitnessFunction(problem.Evaluate, problem.Mapper);
  }
  
  public static IGeneticAlgorithmBuilder<TGenotype> UsingFitnessFunction<TGenotype, TSolution>(this IGeneticAlgorithmBuilder<TGenotype> builder,
    Func<TSolution, Fitness> fitnessFunction, IGenotypeMapper<TGenotype, TSolution> mapper) 
  {
    return builder.FromFitnessFunction(solution => fitnessFunction(mapper.Decode(solution)));
  }
  
  public static IGeneticAlgorithmBuilder<TGenotype> FromFitnessFunction<TGenotype>(this IGeneticAlgorithmBuilder<TGenotype> builder,
    Func<TGenotype, Fitness> fitnessFunction)
  {
    var evaluator = Evaluator.UsingFitnessFunction(fitnessFunction);
    return builder.WithEvaluator(evaluator);
  }
}
