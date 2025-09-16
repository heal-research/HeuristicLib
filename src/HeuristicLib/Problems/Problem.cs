using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Genotypes;
using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Problems;

public interface IProblem {
  Objective Objective { get; }
}

public interface IProblem<in TGenotype> : IProblem {
  IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TGenotype> solution);

  //IEvaluator<TGenotype> GetEvaluator();
}

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

public interface IProblem<in TGenotype, out TEncoding> : IProblem<TGenotype>
  where TEncoding : class, IEncoding<TGenotype> {
  TEncoding SearchSpace { get; }
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

public abstract class Problem<TSolution> : IProblem<TSolution> {
  public Objective Objective { get; }

  protected Problem(Objective objective) {
    Objective = objective;
  }

  public abstract ObjectiveVector Evaluate(TSolution solution);

  public IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TSolution> solution) {
    var results = new ObjectiveVector[solution.Count];
    Parallel.For(0, solution.Count, i => {
      results[i] = Evaluate(solution[i]);
    });
    return results;
  }
}

public abstract class Problem<TSolution, TEncoding> : IProblem<TSolution, TEncoding>
  where TEncoding : class, IEncoding<TSolution> {
  public Objective Objective { get; }
  public TEncoding SearchSpace { get; }

  protected Problem(Objective objective, TEncoding searchSpace) {
    Objective = objective;
    SearchSpace = searchSpace;
  }

  public abstract ObjectiveVector Evaluate(TSolution solution);

  public IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TSolution> solution) {
    var results = new ObjectiveVector[solution.Count];
    Parallel.For(0, solution.Count, i => {
      results[i] = Evaluate(solution[i]);
    });
    return results;
  }
}

public abstract class PermutationProblem : Problem<Permutation, PermutationEncoding> {
  protected PermutationProblem(Objective objective, PermutationEncoding searchSpace) :
    base(objective, searchSpace) { }
}

public abstract class RealVectorProblem : Problem<RealVector, RealVectorEncoding> {
  protected RealVectorProblem(Objective objective, RealVectorEncoding searchSpace)
    : base(objective, searchSpace) { }
}

// public record class EncodedProblem<TSolution, TGenotype, TSearchSpace> : IEncodedProblem<TSolution, TGenotype, TSearchSpace>
//   where TSearchSpace : ISearchSpace<TGenotype>
// {
//   public required TSearchSpace SearchSpace { get; init; }
//   public required IDecoder<TGenotype, TSolution> Decoder { get; init; }
//   public required IEvaluator<TSolution> Evaluator { get; init; }
//   public required Objective Objective { get; init; }
// }

public class FuncProblem<TGenotype, TEncoding> : Problem<TGenotype, TEncoding> /*, IDeterministicProblem<TGenotype>*/
  where TEncoding : class, IEncoding<TGenotype> {
  public Func<TGenotype, double> EvaluateFunc { get; }
  // public IEvaluator<TGenotype> GetEvaluator() {
  //   return new DeterministicProblemEvaluator<TGenotype>(this);
  // }

  public FuncProblem(Func<TGenotype, double> evaluateFunc, TEncoding searchSpace, Objective objective)
    : base(objective, searchSpace) {
    EvaluateFunc = evaluateFunc;
  }

  public override ObjectiveVector Evaluate(TGenotype solution) {
    return EvaluateFunc(solution);
  }
}

public class FuncProblem {
  public static FuncProblem<TGenotype, TEncoding> Create<TGenotype, TEncoding>(
    Func<TGenotype, double> evaluateFunc,
    TEncoding encoding,
    Objective objective
  ) where TEncoding : class, IEncoding<TGenotype> {
    return new(evaluateFunc, encoding, objective);
  }

  private static void Test() {
    var f = new FuncProblem<RealVector, RealVectorEncoding>(
      x => (x * x).Sum(),
      new RealVectorEncoding(2, -10, 10),
      SingleObjective.Minimize
    );

    var fs = FuncProblem.Create(
      (RealVector r) => (r * r).Sum(),
      new RealVectorEncoding(2, -10, 10),
      SingleObjective.Minimize
    );

    // var mutator = new InversionMutator();
    // mutator.Mutate(new Permutation([0, 2, 3, 1]), new SystemRandomNumberGenerator(1), new PermutationEncoding(3));
    //
    // var creator = new RandomPermutationCreator();
    // creator.Create(new SystemRandomNumberGenerator(1), new PermutationEncoding(3));
  }
}
