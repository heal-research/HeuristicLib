#if false
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib._ProofOfConcept;

// Estimator
public interface IOperatorTemplate<out TOperator, in TParameters>
  where TOperator : IOperator {
  TOperator Parameterize(TParameters parameters);
} 

// Predictor
public interface IOperator {
} 



public interface IMutatorTemplate<out TMutator, TGenotype, in TParams> 
  : IOperatorTemplate<TMutator, TParams>
  where TMutator : IMutator<TGenotype> {
}

public interface IMutator<TGenotype> : IOperator {
  TGenotype Mutate(TGenotype genotype);
}

public class UniformOnePositionMutator : IMutator<RealVector> {
  public UniformOnePositionMutator(RealVector minimum, RealVector maximum, IRandomNumberGenerator random) {
    Minimum = minimum;
    Maximum = maximum;
    Random = random;
  }
  public RealVector Minimum { get; }
  public RealVector Maximum { get; }
  public IRandomNumberGenerator Random { get; }
  public RealVector Mutate(RealVector genotype) {
    int index = Random.Integer(genotype.Count);
    double min = Minimum[index % Minimum.Count];
    double max = Maximum[index % Maximum.Count];
    double value = min + Random.Random() * (max - min);
    double[] newGenotype = genotype.ToArray();
    newGenotype[index] = value;
    return new RealVector(newGenotype);
  }

  public record Parameter(RealVector Minimum, RealVector Maximum, IRandomNumberGenerator Random);
  
  public class Template : IMutatorTemplate<UniformOnePositionMutator, RealVector, Parameter> {
    public UniformOnePositionMutator Parameterize(Parameter parameter) {
      return new UniformOnePositionMutator(parameter.Minimum, parameter.Maximum, parameter.Random);
    }
  }
}

#endif
