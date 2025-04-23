using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Problems;

public interface IProblem { }

public interface IProblem<in TSolution> : IProblem {
  IEvaluator<TSolution> Evaluator { get; }
  Objective Objective { get; }
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

public interface IEncodedProblem<TSolution, in TGenotype, out TEncoding> : IProblem<TSolution> 
  where TEncoding : IEncoding<TGenotype>
{
  TEncoding Encoding { get; }
  IDecoder<TGenotype, TSolution> Decoder { get; }
}

// public interface IProblem<TSolution, TGenotype, out TEncoding> : IProblem<TSolution>, //IEncodingProvider<TGenotype, TEncoding>
//  where TEncoding : IEncoding<TGenotype> {
//   //TSolution Decode(TGenotype genotype);
// }

public abstract class ProblemBase<TSolution> : IProblem<TSolution> {
  public IEvaluator<TSolution> Evaluator { get; }
  public Objective Objective { get; }
  
  
  protected ProblemBase(Objective objective) {
    Evaluator = Operators.Evaluator.FromFitnessFunction<TSolution>(Evaluate);
    Objective = objective;
  }
  
  public abstract Fitness Evaluate(TSolution solution);
}

public abstract class EncodedProblemBase<TSolution, TGenotype, TEncoding> : ProblemBase<TSolution>, IEncodedProblem<TSolution, TGenotype, TEncoding>
  where TEncoding : IEncoding<TGenotype>
{
  public TEncoding Encoding { get; }
  public IDecoder<TGenotype, TSolution> Decoder { get; }
  
  protected EncodedProblemBase(Objective objective, TEncoding encoding) : base(objective) {
    Encoding = encoding;
    Decoder = Operators.Decoder.Create<TGenotype, TSolution>(Decode);
  }
  
  public abstract TSolution Decode(TGenotype genotype);
}

public record class EncodedProblem<TSolution, TGenotype, TEncoding> : IEncodedProblem<TSolution, TGenotype, TEncoding>
  where TEncoding : IEncoding<TGenotype>
{
  public required TEncoding Encoding { get; init; }
  public required IDecoder<TGenotype, TSolution> Decoder { get; init; }
  public required IEvaluator<TSolution> Evaluator { get; init; }
  public required Objective Objective { get; init; }
}
