using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Problems;

// public interface IDeterministicProblem<in TGenotype> : IProblem<TGenotype>
// {
//   double Evaluate(TGenotype Solution);
//   
//   // IEvaluator<TGenotype> GetEvaluator()
//   // {
//   //   return new DeterministicProblemEvaluator<TGenotype>(this);
//   // }
// }
// public interface IStochasticProblem<in TGenotype> : IProblem<TGenotype>
// {
//   double Evaluate(TGenotype Solution, IRandomNumberGenerator random);
// }

//
// public interface IEncodingProvider<out TSearchSpace> 
//   where TSearchSpace : IEncoding 
// {
//   TSearchSpace Encoding { get; }
// } 

//
// public static class EvaluationPipeline {
//   public static EvaluationPipeline<TGenotype, TPhenotype, TSvaluationResult> CreateAlgorithm<TGenotype, TPhenotype, TSvaluationResult>(
//     IDecoder<TGenotype, TPhenotype> decoder,
//     IEvaluator<TPhenotype, TSvaluationResult> Evaluator,
//     IFitnessExtractor<TSvaluationResult> fitnessExtractor
//   ) {
//     return new EvaluationPipeline<TGenotype, TPhenotype, TSvaluationResult>(decoder, Evaluator, fitnessExtractor);
//   }
// }
//
// public class EvaluationPipeline<TGenotype, TPhenotype, TSvaluationResult> {
//   public EvaluationPipeline(IDecoder<TGenotype, TPhenotype> decoder, IEvaluator<TPhenotype, TSvaluationResult> Evaluator, IFitnessExtractor<TSvaluationResult> fitnessExtractor) {
//     Decoder = decoder;
//     Evaluator = Evaluator;
//     FitnessExtractor = fitnessExtractor;
//   }
//   public IDecoder<TGenotype, TPhenotype> Decoder { get; }
//   public IEvaluator<TPhenotype, TSvaluationResult> Evaluator { get; }
//   public IFitnessExtractor<TSvaluationResult> FitnessExtractor { get; }
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
//     // Evaluator = Operators.Evaluator.FromFitnessFunction<TISolution>(Evaluate);
//     // ProblemContext = searchSpace;
//     Objective = objective;
//     // ProblemData = problemData;
//   }
//   
//   public abstract ObjectiveVector Evaluate(TISolution Solution);
// }

// public abstract class Problem<TISolution>(Objective objective)
//   : Problem<TISolution, IEncoding<TISolution>>(objective, null!) 
// {
// }

public abstract class Problem<TSolution, TSearchSpace>(Objective objective, TSearchSpace searchSpace) : IProblem<TSolution, TSearchSpace>
  where TSearchSpace : class, ISearchSpace<TSolution> {
  public Objective Objective { get; } = objective;
  public TSearchSpace SearchSpace { get; } = searchSpace;

  public abstract ObjectiveVector Evaluate(TSolution solution, IRandomNumberGenerator random);
}
