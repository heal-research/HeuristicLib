using HEAL.HeuristicLib.Core;

namespace HEAL.HeuristicLib.Problems;

public interface IProblem { }

public interface IProblem<in TSolution> : IProblem {
  Fitness Evaluate(TSolution solution);
  Objective Objective { get; } 
}

public abstract class ProblemBase<TSolution> : IProblem<TSolution> {
  public Objective Objective { get; }
  
  protected ProblemBase(Objective objective) {
    Objective = objective;
  }
  
  public abstract Fitness Evaluate(TSolution solution);
}

//
// public static class GeneticAlgorithmBuilderUsingProblemExtensions {
//   public static TBuilder WithFitnessFunctionFromProblem<TBuilder, TGenotype, TPhenotype, TEncodingParameter>(this TBuilder builder, IProblem<TPhenotype, TGenotype> problem)
//     where TBuilder : IGeneticAlgorithmBuilder<TGenotype, TPhenotype, TEncodingParameter, TBuilder>
//     where TEncodingParameter : IEncodingParameter<TGenotype>
//   {
//     return builder
//       .WithFitnessFunction<TBuilder, TGenotype, TPhenotype, TEncodingParameter>(problem.Evaluate, problem.Decoder)
//       .WithObjective(problem.Objective);
//   }
//   
//   public static TBuilder WithFitnessFunction<TBuilder, TGenotype, TPhenotype, TEncodingParameter>(this TBuilder builder, Func<TPhenotype, Fitness> fitnessFunction, IDecoder<TGenotype, TPhenotype> mapper)
//     where TBuilder : IGeneticAlgorithmBuilder<TGenotype, TPhenotype, TEncodingParameter, TBuilder>
//     where TEncodingParameter : IEncodingParameter<TGenotype>
//   {
//     var evaluator = Evaluator.FromFitnessFunction(fitnessFunction, mapper);
//     return builder.WithEvaluator(evaluator);
//     //return builder.WithFitnessFunction<TBuilder, TGenotype, TPhenotype, TEncodingParameter>(genotype => fitnessFunction(mapper.Decode(genotype)));
//   }
//   
//   // public static TBuilder WithFitnessFunction<TBuilder, TGenotype, TPhenotype, TEncodingParameter>(this TBuilder builder, Func<TGenotype, Fitness> fitnessFunction)
//   //   where TBuilder : IGeneticAlgorithmBuilder<TGenotype, TPhenotype, TEncodingParameter, TBuilder>
//   //   where TEncodingParameter : IEncodingParameter<TGenotype>
//   // {
//   //   FitnessFunctionEvaluator<TGenotype, TGenotype> evaluator = Evaluator.UsingFitnessFunction(fitnessFunction);
//   //   return builder.WithEvaluator(evaluator);
//   // }
// }
