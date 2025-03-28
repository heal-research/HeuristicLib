using HEAL.HeuristicLib.Core;
using HEAL.HeuristicLib.Encodings;

namespace HEAL.HeuristicLib.Problems;

public interface IProblem { }

public interface IProblemInstance {
}

public interface IProblem<in TSolution, in TProblemInstance> : IProblem {
  Fitness Evaluate(TSolution solution, TProblemInstance instance);
  //Objective Objective { get; } 
}

public interface IEncodableProblem<out TSolution, in TGenotype, out TEncoding> : IProblem 
  where TEncoding : IEncoding<TGenotype, TSolution> 
{
  TEncoding GetEncoding();
}

public interface IBindableProblemInstance<out TEncodingParameter, TGenotype> : IProblemInstance
  where TEncodingParameter : IEncodingParameter<TGenotype>
{
  Objective GetObjective();
  TEncodingParameter GetEncodingParameter();
}


public abstract class ProblemBase<TSolution, TProblemInstance> : IProblem<TSolution, TProblemInstance> {
  //public abstract Objective Objective { get; }
  //
  // protected ProblemBase(Objective objective) {
  //   Objective = objective;
  // }
  
  public abstract Fitness Evaluate(TSolution solution, TProblemInstance instance);
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
