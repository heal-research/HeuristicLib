using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Problems.Special;


// This is an example problem that do not use any of the standard search spaces and needs to define its own operators

public record SpecialGenotype(int Value);

public record class SpecialEncoding : Encoding<SpecialGenotype> {
  public override bool Contains(SpecialGenotype genotype) {
    return true;
  }
}

public class SpecialProblem : Problem<SpecialGenotype, SpecialEncoding> {
  public double Data { get; set; }

  public SpecialProblem(double data) : base (SingleObjective.Maximize, GetEncoding()) {
    Data = data;
  }
  
  public override ObjectiveVector Evaluate(SpecialGenotype solution) {
    return Data + solution.Value;
  }

  private static SpecialEncoding GetEncoding() {
    return new SpecialEncoding();
  }
}


public class SpecialGenotypeCreator : Creator<SpecialGenotype, SpecialEncoding, SpecialProblem> {
  public int Parameter { get; set; }
  public SpecialGenotypeCreator(int parameter) {
    Parameter = parameter;
  }
  public override SpecialGenotype Create(IExecutionContext<SpecialEncoding, SpecialProblem> context) {
    var random = context.Random;
    return new SpecialGenotype(random.Integer(0, Parameter));
  }
}


public class SpecialGenotypeCrossover : Crossover<SpecialGenotype, SpecialEncoding, SpecialProblem> {
  public override SpecialGenotype Cross((SpecialGenotype, SpecialGenotype) parents, IRandomNumberGenerator random, SpecialEncoding encoding, SpecialProblem problem) {
    var (parent1, parent2) = parents;
    return new SpecialGenotype(random.Random() < 0.5 ? parent1.Value : parent2.Value);
  }
}


public class SpecialGenotypeMutator : Mutator<SpecialGenotype, SpecialEncoding, SpecialProblem> {
  public override SpecialGenotype Mutate(SpecialGenotype genotype, IRandomNumberGenerator random, SpecialEncoding encoding, SpecialProblem problem) {
    int strength = (int)Math.Round(problem.Data);
    int offset = random.Integer(-strength, strength + 1);
    return new SpecialGenotype(genotype.Value + offset);
  }
}
