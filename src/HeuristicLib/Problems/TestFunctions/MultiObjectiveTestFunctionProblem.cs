using HEAL.HeuristicLib.Genotypes;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.SearchSpaces;
using LanguageExt;

namespace HEAL.HeuristicLib.Problems.TestFunctions;

public class MultiObjectiveTestFunctionProblem : ProblemBase<RealVector, RealVectorSearchSpace, Unit> {
  private readonly IMultiObjectiveTestFunction testFunction;

  public MultiObjectiveTestFunctionProblem(IMultiObjectiveTestFunction testFunction) 
    : base(GetSearchSpace(testFunction), testFunction.Objective, Unit.Default) 
  {
    this.testFunction = testFunction;
  }
  
  public override ObjectiveVector Evaluate(RealVector solution) {
    return new ObjectiveVector(testFunction.Evaluate(solution));
  }
  
  public static RealVectorSearchSpace GetSearchSpace(IMultiObjectiveTestFunction testFunction) => new RealVectorSearchSpace(testFunction.Dimension, testFunction.Min, testFunction.Max);

  // public override RealVector Decode(RealVector genotype) => genotype;
  
  // return new RealVectorSearchSpace<RealVector>(Decoder.Identity<RealVector>()) {
  //   Creator = new UniformDistributedCreator(minimum: null, maximum: null), 
  //   Crossover = new AlphaBetaBlendCrossover(alpha: 0.7, beta: 0.3),
  //   Mutator = new GaussianMutator(mutationRate: 0.1, mutationStrength: 0.1)
  // };
}


public interface IMultiObjectiveTestFunction {
  public int Dimension { get; }
  double Min { get; }
  double Max { get; }
  public Objective Objective { get; }
  RealVector Evaluate(RealVector solution);
}

public class ZDT1 : IMultiObjectiveTestFunction {
  public int Dimension { get; }
  public double Min => 0;
  public double Max => 1;
  public Objective Objective => MultiObjective.Create([ObjectiveDirection.Minimize, ObjectiveDirection.Minimize]);
  
  public ZDT1(int dimension) {
    Dimension = dimension;
  }
  
  public RealVector Evaluate(RealVector solution) {
    double g = 0;
    for (int i = 1; i < solution.Count; i++) g += solution[i];
    g = 1.0 + 9.0 * g / (solution.Count - 1);
    double f0 = solution[0];
    double f1 = g * (1.0 - Math.Sqrt(solution[0] / g));
    return new RealVector(f0, f1);
  }
}

public class ZDT2 : IMultiObjectiveTestFunction {
  public int Dimension { get; }
  public double Min => 0;
  public double Max => 1;
  public Objective Objective => MultiObjective.Create([ObjectiveDirection.Minimize, ObjectiveDirection.Minimize]);
  
  public ZDT2(int dimension) {
    Dimension = dimension;
  }
  
  public RealVector Evaluate(RealVector solution) {
    double g = 0;
    for (int i = 1; i < solution.Count; i++) g += solution[i];
    g = 1.0 + 9.0 * g / (solution.Count - 1);
    double d = solution[0] / g;
    double f0 = solution[0];
    double f1 = g * (1.0 - d * d);
    return new RealVector(f0, f1);
  }
}
