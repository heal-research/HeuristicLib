using HEAL.HeuristicLib.Core;
using HEAL.HeuristicLib.Encodings;

namespace HEAL.HeuristicLib.Problems;

public interface IProblem { }

public interface IProblem<in TSolution> : IProblem {
  Fitness Evaluate(TSolution solution);
  Objective Objective { get; } 
}

public interface IProblem<TSolution, TGenotype, out TEncoding> : IProblem<TSolution>, IEncodingProvider<TGenotype, TEncoding>
 where TEncoding : IEncoding<TGenotype> {
  TSolution Decode(TGenotype genotype);
}

public abstract class ProblemBase<TSolution> : IProblem<TSolution> {
  public Objective Objective { get; }
  
  protected ProblemBase(Objective objective) {
    Objective = objective;
  }
  
  public abstract Fitness Evaluate(TSolution solution);
}

public abstract class ProblemBase<TSolution, TGenotype, TEncoding> : ProblemBase<TSolution>, IProblem<TSolution, TGenotype, TEncoding>
  where TEncoding : IEncoding<TGenotype>
{
  protected ProblemBase(Objective objective) : base(objective) {}

  public abstract TEncoding GetEncoding();
  public abstract TSolution Decode(TGenotype genotype);
}

//
// public static class GeneticAlgorithmBuilderUsingProblemExtensions {
//   public static TBuilder WithFitnessFunctionFromProblem<TBuilder, TGenotype, TPhenotype, TEncoding>(this TBuilder builder, IProblem<TPhenotype, TGenotype> problem)
//     where TBuilder : IGeneticAlgorithmBuilder<TGenotype, TPhenotype, TEncoding, TBuilder>
//     where TEncoding : IEncoding<TGenotype>
//   {
//     return builder
//       .WithFitnessFunction<TBuilder, TGenotype, TPhenotype, TEncoding>(problem.Evaluate, problem.Decoder)
//       .WithObjective(problem.Objective);
//   }
//   
//   public static TBuilder WithFitnessFunction<TBuilder, TGenotype, TPhenotype, TEncoding>(this TBuilder builder, Func<TPhenotype, Fitness> fitnessFunction, IDecoder<TGenotype, TPhenotype> mapper)
//     where TBuilder : IGeneticAlgorithmBuilder<TGenotype, TPhenotype, TEncoding, TBuilder>
//     where TEncoding : IEncoding<TGenotype>
//   {
//     var evaluator = Evaluator.FromFitnessFunction(fitnessFunction, mapper);
//     return builder.WithEvaluator(evaluator);
//     //return builder.WithFitnessFunction<TBuilder, TGenotype, TPhenotype, TEncoding>(genotype => fitnessFunction(mapper.Decode(genotype)));
//   }
//   
//   // public static TBuilder WithFitnessFunction<TBuilder, TGenotype, TPhenotype, TEncoding>(this TBuilder builder, Func<TGenotype, Fitness> fitnessFunction)
//   //   where TBuilder : IGeneticAlgorithmBuilder<TGenotype, TPhenotype, TEncoding, TBuilder>
//   //   where TEncoding : IEncoding<TGenotype>
//   // {
//   //   FitnessFunctionEvaluator<TGenotype, TGenotype> evaluator = Evaluator.UsingFitnessFunction(fitnessFunction);
//   //   return builder.WithEvaluator(evaluator);
//   // }
// }
