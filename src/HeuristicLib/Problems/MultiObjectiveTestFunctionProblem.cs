using HEAL.HeuristicLib.Core;
using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Problems;

public class MultiObjectiveTestFunctionProblem : ProblemBase<RealVector, MultiObjectiveTestFunctionInstance>, IEncodableProblem<RealVector, RealVector, RealVectorEncoding<RealVector>> {
  
  public override Fitness Evaluate(RealVector solution, MultiObjectiveTestFunctionInstance instance) {
    return new Fitness(instance.Evaluate(solution));
  }
  
  public RealVectorEncoding<RealVector> GetEncoding() {
    return new RealVectorEncoding<RealVector>(Decoder.Identity<RealVector>()) {
      Creator = new UniformDistributedCreator(minimum: null, maximum: null), 
      Crossover = new AlphaBetaBlendCrossover(alpha: 0.7, beta: 0.3),
      Mutator = new GaussianMutator(mutationRate: 0.1, mutationStrength: 0.1)
    };
  }
}


public class MultiObjectiveTestFunctionInstance : IBindableProblemInstance<RealVectorEncodingParameter, RealVector> {
  private readonly IMultiObjectiveTestFunction testFunction;
  
  public MultiObjectiveTestFunctionInstanceInformation? Information { get; init; }

  public MultiObjectiveTestFunctionInstance(IMultiObjectiveTestFunction testFunction, MultiObjectiveTestFunctionInstanceInformation? information = null) {
    this.testFunction = testFunction;
    Information = information;
  }

  public RealVector Evaluate(RealVector solution) {
    return testFunction.Evaluate(solution);
  }
  
  public Objective GetObjective() => MultiObjective.Create(Enumerable.Repeat(ObjectiveDirection.Minimize, testFunction.Dimension).ToArray());
  
  public RealVectorEncodingParameter GetEncodingParameter() {
    return new RealVectorEncodingParameter(length: testFunction.Dimension, minimum: testFunction.Min, maximum: testFunction.Max);
  }
}

public class MultiObjectiveTestFunctionInstanceInformation {
  public required string Name { get; init; }
  public string? Description { get; init; }
  public string? Publication { get; init; }
  public RealVector? BestKnownQuality { get; init; }
  public RealVector? BestKnownSolution { get; init; }
}

public interface IMultiObjectiveTestFunction {
  public int Dimension { get; }
  double Min { get; }
  double Max { get; }
  RealVector Evaluate(RealVector solution);
}

public class ZDT1 : IMultiObjectiveTestFunction {
  public int Dimension { get; }
  public double Min => 0;
  public double Max => 1;
  
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
