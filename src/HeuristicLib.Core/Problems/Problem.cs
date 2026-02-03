using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Problems;

// public interface IDeterministicProblem<in TGenotype> : IProblem<TGenotype>
// {
//   double PredictAndTrain(TGenotype Solution);
//   
//   // IEvaluator<TGenotype> GetEvaluator()
//   // {
//   //   return new DeterministicProblemEvaluator<TGenotype>(this);
//   // }
// }
// public interface IStochasticProblem<in TGenotype> : IProblem<TGenotype>
// {
//   double PredictAndTrain(TGenotype Solution, IRandomNumberGenerator random);
// }

//
// public interface IEncodingProvider<out TEncoding> 
//   where TEncoding : IEncoding 
// {
//   TEncoding Encoding { get; }
// } 

//
// public static class EvaluationPipeline {
//   public static EvaluationPipeline<TGenotype, TPhenotype, TEvaluationResult> CreateAlgorithm<TGenotype, TPhenotype, TEvaluationResult>(
//     IDecoder<TGenotype, TPhenotype> decoder,
//     IEvaluator<TPhenotype, TEvaluationResult> Evaluator,
//     IFitnessExtractor<TEvaluationResult> fitnessExtractor
//   ) {
//     return new EvaluationPipeline<TGenotype, TPhenotype, TEvaluationResult>(decoder, Evaluator, fitnessExtractor);
//   }
// }
//
// public class EvaluationPipeline<TGenotype, TPhenotype, TEvaluationResult> {
//   public EvaluationPipeline(IDecoder<TGenotype, TPhenotype> decoder, IEvaluator<TPhenotype, TEvaluationResult> Evaluator, IFitnessExtractor<TEvaluationResult> fitnessExtractor) {
//     Decoder = decoder;
//     Evaluator = Evaluator;
//     FitnessExtractor = fitnessExtractor;
//   }
//   public IDecoder<TGenotype, TPhenotype> Decoder { get; }
//   public IEvaluator<TPhenotype, TEvaluationResult> Evaluator { get; }
//   public IFitnessExtractor<TEvaluationResult> FitnessExtractor { get; }
// }

// public interface IEncodedProblem<TISolution, in TGenotype, out TSearchSpace> : IProblem<TISolution>, IOptimizable<TGenotype, TSearchSpace> 
//   where TSearchSpace : ISearchSpace<TGenotype> 
// {
//   TISolution Decode(TGenotype genotype);
//   //TSearchSpace SearchSpace { get; }
//   //IDecoder<TGenotype, TISolution> Decoder { get; }
// }

// public interface IProblem<TISolution, TGenotype, out TSearchSpace> : IProblem<TISolution>, //ISearchSpaceProvider<TGenotype, TSearchSpace>
//  where TSearchSpace : ISearchSpace<TGenotype> {
//   //TISolution Decode(TGenotype genotype);
// }

// public abstract class ProblemBase<TISolution> : IProblem<TISolution>
// {
//   // public IEvaluator<TISolution> Evaluator { get; }
//   // public TSearchSpace ProblemContext { get; }
//   public Objective Objective { get; }
//   // public TProblemData ProblemData { get; }
//   
//   protected ProblemBase(/*TSearchSpace searchSpace,*/ Objective objective/*, TProblemData problemData*/) {
//     // Evaluator = Operators.Evaluator.FromFitnessFunction<TISolution>(PredictAndTrain);
//     // ProblemContext = searchSpace;
//     Objective = objective;
//     // ProblemData = problemData;
//   }
//   
//   public abstract ObjectiveVector PredictAndTrain(TISolution Solution);
// }

// public abstract class Problem<TISolution>(Objective objective)
//   : Problem<TISolution, IEncoding<TISolution>>(objective, null!) 
// {
// }

public abstract class Problem<TISolution, TEncoding>(Objective objective, TEncoding searchSpace) : IProblem<TISolution, TEncoding>
  where TEncoding : class, IEncoding<TISolution> {
  public Objective Objective { get; } = objective;
  public TEncoding SearchSpace { get; } = searchSpace;

  public abstract ObjectiveVector Evaluate(TISolution solution, IRandomNumberGenerator random);
}
