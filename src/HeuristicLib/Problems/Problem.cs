using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Problems;

// public interface IDeterministicProblem<in TGenotype> : IProblem<TGenotype>
// {
//   double Evaluate(TGenotype solution);
//   
//   // IEvaluator<TGenotype> GetEvaluator()
//   // {
//   //   return new DeterministicProblemEvaluator<TGenotype>(this);
//   // }
// }
// public interface IStochasticProblem<in TGenotype> : IProblem<TGenotype>
// {
//   double Evaluate(TGenotype solution, IRandomNumberGenerator random);
// }

//
// public interface IEncodingProvider<out TEncoding> 
//   where TEncoding : IEncoding 
// {
//   TEncoding Encoding { get; }
// } 

//
// public static class EvaluationPipeline {
//   public static EvaluationPipeline<TGenotype, TPhenotype, TEvaluationResult> Create<TGenotype, TPhenotype, TEvaluationResult>(
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

// public interface IEncodedProblem<TSolution, in TGenotype, out TSearchSpace> : IProblem<TSolution>, IOptimizable<TGenotype, TSearchSpace> 
//   where TSearchSpace : ISearchSpace<TGenotype> 
// {
//   TSolution Decode(TGenotype genotype);
//   //TSearchSpace SearchSpace { get; }
//   //IDecoder<TGenotype, TSolution> Decoder { get; }
// }

// public interface IProblem<TSolution, TGenotype, out TSearchSpace> : IProblem<TSolution>, //ISearchSpaceProvider<TGenotype, TSearchSpace>
//  where TSearchSpace : ISearchSpace<TGenotype> {
//   //TSolution Decode(TGenotype genotype);
// }

// public abstract class ProblemBase<TSolution> : IProblem<TSolution>
// {
//   // public IEvaluator<TSolution> Evaluator { get; }
//   // public TSearchSpace ProblemContext { get; }
//   public Objective Objective { get; }
//   // public TProblemData ProblemData { get; }
//   
//   protected ProblemBase(/*TSearchSpace searchSpace,*/ Objective objective/*, TProblemData problemData*/) {
//     // Evaluator = Operators.Evaluator.FromFitnessFunction<TSolution>(Evaluate);
//     // ProblemContext = searchSpace;
//     Objective = objective;
//     // ProblemData = problemData;
//   }
//   
//   public abstract ObjectiveVector Evaluate(TSolution solution);
// }

public abstract class Problem<TSolution>(Objective objective)
  : Problem<TSolution, IEncoding<TSolution>>(objective, AnyEncoding<TSolution>.Instance);

public abstract class Problem<TSolution, TEncoding>(Objective objective, TEncoding searchSpace) : IProblem<TSolution, TEncoding>
  where TEncoding : class, IEncoding<TSolution> {
  public Objective Objective { get; } = objective;
  public TEncoding SearchSpace { get; } = searchSpace;

  public abstract ObjectiveVector Evaluate(TSolution solution, IRandomNumberGenerator random);
  public virtual ObjectiveVector Evaluate(TSolution solution) => Evaluate(solution, NoRandomNumberGenerator.Instance);
}
