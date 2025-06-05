using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Problems.Special;


// This is an example problem that do not use any of the standard search spaces and needs to define its own operators

public record SpecialGenotype(int Value);

public class SpecialProblem : IProblem<SpecialGenotype> {
  public Objective Objective => SingleObjective.Minimize;
  public double Data { get; }

  public SpecialProblem(double data) {
    Data = data;
  }
  
  public ObjectiveVector Evaluate(SpecialGenotype solution) {
    return Data + solution.Value;
  }
}


public record class SpecialGenotypeCreator : Creator<SpecialGenotype, SpecialProblem> {
  public required int Parameter { get; init; }

  public override ICreatorExecution<SpecialGenotype> CreateExecution(SpecialProblem problem) {
    return new SpecialGenotypeCreatorExecution(this, problem);
  }
}

public class SpecialGenotypeCreatorExecution : CreatorExecution<SpecialGenotype, SpecialProblem, SpecialGenotypeCreator> {
  public SpecialGenotypeCreatorExecution(SpecialGenotypeCreator parameters, SpecialProblem problem) : base(parameters, problem) { }

  public override SpecialGenotype Create(IRandomNumberGenerator random) {
    return new SpecialGenotype(random.Integer(0, Parameters.Parameter));
  }
}


public record class SpecialGenotypeCrossover : Operator<SpecialGenotype, SpecialProblem> {
  public override ICrossoverExecution<SpecialGenotype> CreateExecution(SpecialProblem problem) {
    return new SpecialGenotypeCrossoverExecution(this, problem);
  }
}

public class SpecialGenotypeCrossoverExecution : CrossoverExecution<SpecialGenotype, SpecialProblem, SpecialGenotypeCrossover> {
  public SpecialGenotypeCrossoverExecution(SpecialGenotypeCrossover parameters, SpecialProblem problem) : base(parameters, problem) { }

  public override SpecialGenotype Cross(SpecialGenotype parent1, SpecialGenotype parent2, IRandomNumberGenerator random) {
    return new SpecialGenotype(random.Random() < 0.5 ? parent1.Value : parent2.Value);
  }
}


public record class SpecialGenotypeMutator : Mutator<SpecialGenotype, SpecialProblem> {
  public override IMutatorExecution<SpecialGenotype> CreateExecution(SpecialProblem problem) {
    return new SpecialGenotypeMutatorExecution(this, problem);
  }
}

public class SpecialGenotypeMutatorExecution : MutatorExecution<SpecialGenotype, SpecialProblem, SpecialGenotypeMutator> {
  public SpecialGenotypeMutatorExecution(SpecialGenotypeMutator parameters, SpecialProblem problem) : base(parameters, problem) { }

  public SpecialGenotype Mutate(IRandomNumberGenerator random, SpecialGenotype genotype) {
    int strength = (int)SearchSpace.Data;
    int offset = random.Integer(-strength, strength + 1);
    return new SpecialGenotype(genotype.Value + offset);
  }
}
