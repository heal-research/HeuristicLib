// using HEAL.HeuristicLib.Operators;

using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Problems;

// public interface IOptimizationProblem { }

public interface IProblem<in TSolution, out TSearchSpace, out TProblemData> : IOptimizable<TSolution, TSearchSpace> 
  where TSearchSpace : ISearchSpace<TSolution>
/*: IProblem*/ {
  TProblemData ProblemData { get; }
  // IEvaluator<TSolution> Evaluator { get; }
  // TSearchSpace SearchSpace { get; }
  // Objective Objective { get; }
  // Fitness Evaluate(TSolution solution);
}

//
// public static class EvaluationPipeline {
//   public static EvaluationPipeline<TGenotype, TPhenotype, TEvaluationResult> Create<TGenotype, TPhenotype, TEvaluationResult>(
//     IDecoder<TGenotype, TPhenotype> decoder,
//     IEvaluator<TPhenotype, TEvaluationResult> evaluator,
//     IFitnessExtractor<TEvaluationResult> fitnessExtractor
//   ) {
//     return new EvaluationPipeline<TGenotype, TPhenotype, TEvaluationResult>(decoder, evaluator, fitnessExtractor);
//   }
// }
//
// public class EvaluationPipeline<TGenotype, TPhenotype, TEvaluationResult> {
//   public EvaluationPipeline(IDecoder<TGenotype, TPhenotype> decoder, IEvaluator<TPhenotype, TEvaluationResult> evaluator, IFitnessExtractor<TEvaluationResult> fitnessExtractor) {
//     Decoder = decoder;
//     Evaluator = evaluator;
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

public abstract class ProblemBase<TSolution, TSearchSpace, TProblemData> : IProblem<TSolution, TSearchSpace, TProblemData>
  where TSearchSpace : ISearchSpace<TSolution>
{
  // public IEvaluator<TSolution> Evaluator { get; }
  public TSearchSpace SearchSpace { get; }
  public Objective Objective { get; }
  public TProblemData ProblemData { get; }
  
  protected ProblemBase(TSearchSpace searchSpace, Objective objective, TProblemData problemData) {
    // Evaluator = Operators.Evaluator.FromFitnessFunction<TSolution>(Evaluate);
    SearchSpace = searchSpace;
    Objective = objective;
    ProblemData = problemData;
  }
  
  public abstract ObjectiveVector Evaluate(TSolution solution);
}


// public record class EncodedProblem<TSolution, TGenotype, TSearchSpace> : IEncodedProblem<TSolution, TGenotype, TSearchSpace>
//   where TSearchSpace : ISearchSpace<TGenotype>
// {
//   public required TSearchSpace SearchSpace { get; init; }
//   public required IDecoder<TGenotype, TSolution> Decoder { get; init; }
//   public required IEvaluator<TSolution> Evaluator { get; init; }
//   public required Objective Objective { get; init; }
// }
