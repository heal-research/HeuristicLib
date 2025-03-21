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
public interface ISingleObjectiveProblem<TSolution, TGenotype> : IProblem<TSolution, TGenotype, Fitness, Goal>;
public interface IMultiObjectiveProblem<TSolution, TGenotype> : IProblem<TSolution, TGenotype, FitnessVector, GoalVector>;

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
public abstract class SingleObjectiveProblemBase<TSolution, TGenotype> : ProblemBase<TSolution, TGenotype, Fitness, Goal>, ISingleObjectiveProblem<TSolution, TGenotype> {
  protected SingleObjectiveProblemBase(IGenotypeMapper<TGenotype, TSolution> mapper, Goal goal) : base(mapper, goal) { }
}
public abstract class MultiObjectiveProblemBase<TSolution, TGenotype> : ProblemBase<TSolution, TGenotype, FitnessVector, GoalVector>, IMultiObjectiveProblem<TSolution, TGenotype> {
  protected MultiObjectiveProblemBase(IGenotypeMapper<TGenotype, TSolution> mapper, GoalVector goal) : base(mapper, goal) { }
}

public static class GeneticAlgorithmBuilderUsingProblemExtensions {
  public static TBuilder WithFitnessFunctionFromProblem<TBuilder,TGenotype, TSolution>(this TBuilder builder, IProblem<TSolution, TGenotype, Fitness, Goal> problem)
    where TBuilder : IGeneticAlgorithmBuilder<TGenotype, TBuilder>
  {
    return builder.WithFitnessFunction(problem.Evaluate, problem.Mapper);
  }
  
  public static TBuilder WithFitnessFunction<TBuilder, TGenotype, TSolution>(this TBuilder builder, Func<TSolution, Fitness> fitnessFunction, IGenotypeMapper<TGenotype, TSolution> mapper)
    where TBuilder : IGeneticAlgorithmBuilder<TGenotype, TBuilder>
  {
    return builder.WithFitnessFunction<TBuilder, TGenotype>(solution => fitnessFunction(mapper.Decode(solution)));
  }
  
  public static TBuilder WithFitnessFunction<TBuilder, TGenotype>(this TBuilder builder, Func<TGenotype, Fitness> fitnessFunction)
    where TBuilder : IGeneticAlgorithmBuilder<TGenotype, TBuilder>
  {
    var evaluator = Evaluator.UsingFitnessFunction(fitnessFunction);
    return builder.WithEvaluator(evaluator);
  }
}
