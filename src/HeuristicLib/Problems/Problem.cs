// using HEAL.HeuristicLib.Operators;

using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Genotypes;
using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Problems;

// public interface IOptimizationProblem { }

public interface IProblem
{
  // IEncoding Encoding { get; }
  Objective Objective { get; }
}

public interface IProblem<in TGenotype> : IProblem
{
  IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TGenotype> solution);
  //ObjectiveVector Evaluate(TGenotype solution);
  
  // IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TGenotype> solution);
  
  // IEncoding<TGenotype> Encoding { get; }
}

public interface IProblem<in TGenotype, out TEncoding> : IProblem<TGenotype>
  where TEncoding : class, IEncoding<TGenotype>
{
  TEncoding Encoding { get; }
}

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

public abstract class Problem<TSolution, TEncoding> : IProblem<TSolution, TEncoding>
  where TEncoding : class, IEncoding<TSolution>
{
  public Objective Objective { get; }
  public TEncoding Encoding { get; }
  
  protected Problem(Objective objective) {
    Objective = objective;
    Encoding = GetEncoding();
  }
  
  public abstract ObjectiveVector Evaluate(TSolution solution);

  public IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TSolution> solution) {
    var results = new ObjectiveVector[solution.Count];
    Parallel.For(0, solution.Count, i => {
      results[i] = Evaluate(solution[i]);
    });
    return results;
  }

  public abstract TEncoding GetEncoding();
}

public abstract class PermutationProblem : Problem<Permutation, PermutationEncoding> {
  protected PermutationProblem(Objective objective) : base(objective) {}
}

public abstract class RealVectorProblem : Problem<RealVector, RealVectorEncoding> {
  protected RealVectorProblem(Objective objective) : base(objective) {
  }
}

// public record class EncodedProblem<TSolution, TGenotype, TSearchSpace> : IEncodedProblem<TSolution, TGenotype, TSearchSpace>
//   where TSearchSpace : ISearchSpace<TGenotype>
// {
//   public required TSearchSpace SearchSpace { get; init; }
//   public required IDecoder<TGenotype, TSolution> Decoder { get; init; }
//   public required IEvaluator<TSolution> Evaluator { get; init; }
//   public required Objective Objective { get; init; }
// }
