
using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Genotypes;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Problems.TravelingSalesman;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib._ProofOfConcept.CompatabilityUsingGenericOperators;

public class OperatorExecutionContext<TEncoding, TProblem>
  where TEncoding : IEncoding
  where TProblem : IProblem
{
  public required IRandomNumberGenerator Random { get; set; } 
  public required TEncoding Encoding { get; init; }
  public required TProblem Problem { get; init; }

  public OperatorExecutionContext<TEncoding, TProblem>[] ForkExecution(int count) => Enumerable.Repeat(this, count).ToArray();
}

public interface ICrossover<TGenotype, TEncoding, TProblem>
  where TEncoding : IEncoding<TGenotype>
  where TProblem : IProblem<TGenotype, TEncoding>
{
  TGenotype Cross(TGenotype parent1, TGenotype parent2, OperatorExecutionContext<TEncoding, TProblem> context);
}

public abstract class BaseCrossover<TGenotype, TEncoding, TProblem> : ICrossover<TGenotype, TEncoding, TProblem>
  where TEncoding : IEncoding<TGenotype>
  where TProblem : IProblem<TGenotype, TEncoding>
{

  public abstract TGenotype Cross(TGenotype parent1, TGenotype parent2, OperatorExecutionContext<TEncoding, TProblem> context);
}


public class IndependentCrossover<TGenotype, TEncoding, TProblem> : BaseCrossover<TGenotype, TEncoding, TProblem>
  where TEncoding : IEncoding<TGenotype>
  where TProblem : IProblem<TGenotype, TEncoding>
{
  public override TGenotype Cross(TGenotype parent1, TGenotype parent2, OperatorExecutionContext<TEncoding, TProblem> context) {
    throw new NotImplementedException();
  }
}

public class PermutationSpecificCrossover<TProblem> : BaseCrossover<Permutation, PermutationEncoding, TProblem>
  where TProblem : IProblem<Permutation, PermutationEncoding>
{

  public override Permutation Cross(Permutation parent1, Permutation parent2, OperatorExecutionContext<PermutationEncoding, TProblem> context) {
    throw new NotImplementedException();
  }
}

public class TspSpecificCrossover : BaseCrossover<Permutation, PermutationEncoding, TravelingSalesmanProblem>
{
  public override Permutation Cross(Permutation parent1, Permutation parent2, OperatorExecutionContext<PermutationEncoding, TravelingSalesmanProblem> context) {
    throw new NotImplementedException();
  }
}

public class RealVectorSpecificCrossover<TProblem> : BaseCrossover<RealVector, RealVectorEncoding, TProblem>
  where TProblem : IProblem<RealVector, RealVectorEncoding>
{
  public override RealVector Cross(RealVector parent1, RealVector parent2, OperatorExecutionContext<RealVectorEncoding, TProblem> context) {
    throw new NotImplementedException();
  }
}

public class TestFunctionProblemSpecificCrossover : BaseCrossover<RealVector, RealVectorEncoding, TestFunctionProblem>
{
  public override RealVector Cross(RealVector parent1, RealVector parent2, OperatorExecutionContext<RealVectorEncoding, TestFunctionProblem> context) {
    throw new NotImplementedException();
  }
}

public record IterationState<TGenotype>
{
  public required ImmutableList<TGenotype> Population { get; init; }
}

public interface IAlgorithm
{
}

public interface IAlgorithm<TGenotype, in TEncoding, in TProblem> 
  where TEncoding : IEncoding<TGenotype>
  where TProblem : IProblem<TGenotype, TEncoding>
{
  IterationState<TGenotype> Run(TProblem problem, IterationState<TGenotype>? initialState = null);
}

public class EncodingIndependentAlgorithm<TGenotype, TEncoding, TProblem> : IAlgorithm<TGenotype, TEncoding, TProblem>
  where TEncoding : IEncoding<TGenotype>
  where TProblem : IProblem<TGenotype, TEncoding>
{
  public ICrossover<TGenotype, TEncoding, TProblem> Crossover { get; set; }
  public IterationState<TGenotype> Run(TProblem problem, IterationState<TGenotype>? initialState = null) {
    throw new NotImplementedException();
  }
}

public class PermutationEncodingSpecificAlgorithm<TProblem> : IAlgorithm<Permutation, PermutationEncoding, TProblem>
  where TProblem : IProblem<Permutation, PermutationEncoding>
{
  public ICrossover<Permutation, PermutationEncoding, TProblem> Crossover { get; set; }
  public IterationState<Permutation> Run(TProblem problem, IterationState<Permutation>? initialState = null) {
    throw new NotImplementedException();
  }
}

public class TravelingSalesmanProblemSpecificAlgorithm : IAlgorithm<Permutation, PermutationEncoding, TravelingSalesmanProblem>
{
  public ICrossover<Permutation, PermutationEncoding, TravelingSalesmanProblem> Crossover { get; set; }
  public IterationState<Permutation> Run(TravelingSalesmanProblem problem, IterationState<Permutation>? initialState = null) {
    throw new NotImplementedException();
  }
}

public class RealVectorEncodingSpecificAlgorithm<TProblem> : IAlgorithm<RealVector, RealVectorEncoding, TProblem>
  where TProblem : IProblem<RealVector, RealVectorEncoding>
{
  public ICrossover<RealVector, RealVectorEncoding, TProblem> Crossover { get; set; }
  public IterationState<RealVector> Run(TProblem problem, IterationState<RealVector>? initialState = null) {
    throw new NotImplementedException();
  }
}

public class TestFunctionProblemSpecificAlgorithm : IAlgorithm<RealVector, RealVectorEncoding, TestFunctionProblem>
{
  public ICrossover<RealVector, RealVectorEncoding, TestFunctionProblem> Crossover { get; set; }
  public IterationState<RealVector> Run(TestFunctionProblem problem, IterationState<RealVector>? initialState = null) {
    throw new NotImplementedException();
  }
}

public class Test
{
  static void TestCompatability() {

    var independentAlgorithmWithPermutationProblem = new EncodingIndependentAlgorithm<Permutation, PermutationEncoding, PermutationProblem>();
    independentAlgorithmWithPermutationProblem.Run(new TravelingSalesmanProblem()); //ok
    independentAlgorithmWithPermutationProblem.Run(new TestFunctionProblem()); // error: incompatible encoding
    independentAlgorithmWithPermutationProblem.Crossover = new IndependentCrossover<Permutation, PermutationEncoding, PermutationProblem>(); // ok
    independentAlgorithmWithPermutationProblem.Crossover = new IndependentCrossover<Permutation, PermutationEncoding, TravelingSalesmanProblem>(); // ok
    independentAlgorithmWithPermutationProblem.Crossover = new PermutationSpecificCrossover<PermutationProblem>(); // ok
    independentAlgorithmWithPermutationProblem.Crossover = new PermutationSpecificCrossover<TravelingSalesmanProblem>(); // ok
    independentAlgorithmWithPermutationProblem.Crossover = new TspSpecificCrossover(); // error: problem independent algorithm
    independentAlgorithmWithPermutationProblem.Crossover = new RealVectorSpecificCrossover<RealVectorProblem>(); // error: incompatible encoding
    independentAlgorithmWithPermutationProblem.Crossover = new TestFunctionProblemSpecificCrossover(); // error: incompatible encoding, problem independent algorithm
    
    var independentAlgorithmWithRealVectorProblem = new EncodingIndependentAlgorithm<RealVector, RealVectorEncoding, RealVectorProblem>();
    independentAlgorithmWithRealVectorProblem.Run(new TravelingSalesmanProblem()); // error: incompatible encoding
    independentAlgorithmWithRealVectorProblem.Run(new TestFunctionProblem()); // ok
    independentAlgorithmWithRealVectorProblem.Crossover = new IndependentCrossover<RealVector, RealVectorEncoding, RealVectorProblem>(); // ok
    independentAlgorithmWithRealVectorProblem.Crossover = new IndependentCrossover<RealVector, RealVectorEncoding, TestFunctionProblem>(); // ok
    independentAlgorithmWithRealVectorProblem.Crossover = new PermutationSpecificCrossover<PermutationProblem>(); // error: incompatible encoding
    independentAlgorithmWithRealVectorProblem.Crossover = new TspSpecificCrossover(); // error: incompatible encoding, problem independent algorithm
    independentAlgorithmWithRealVectorProblem.Crossover = new RealVectorSpecificCrossover<RealVectorProblem>(); // ok
    independentAlgorithmWithRealVectorProblem.Crossover = new RealVectorSpecificCrossover<TestFunctionProblem>(); // ok
    independentAlgorithmWithRealVectorProblem.Crossover = new TestFunctionProblemSpecificCrossover(); // error: problem independent algorithm
    
    var independentAlgorithmWithTsp = new EncodingIndependentAlgorithm<Permutation, PermutationEncoding, TravelingSalesmanProblem>();
    independentAlgorithmWithTsp.Run(new TravelingSalesmanProblem()); //ok
    independentAlgorithmWithTsp.Run(new TestFunctionProblem()); // error: incompatible encoding
    independentAlgorithmWithTsp.Crossover = new IndependentCrossover<Permutation, PermutationEncoding, TravelingSalesmanProblem>(); // ok
    independentAlgorithmWithTsp.Crossover = new PermutationSpecificCrossover<TravelingSalesmanProblem>(); // ok
    independentAlgorithmWithTsp.Crossover = new TspSpecificCrossover(); // ok
    independentAlgorithmWithTsp.Crossover = new RealVectorSpecificCrossover<TestFunctionProblem>(); // error: incompatible encoding
    independentAlgorithmWithTsp.Crossover = new TestFunctionProblemSpecificCrossover(); // error: incompatible encoding, problem independent algorithm
    
    var independentAlgorithmWithTestFunction = new EncodingIndependentAlgorithm<RealVector, RealVectorEncoding, TestFunctionProblem>();
    independentAlgorithmWithTestFunction.Run(new TravelingSalesmanProblem()); // error: incompatible encoding
    independentAlgorithmWithTestFunction.Run(new TestFunctionProblem()); // ok
    independentAlgorithmWithTestFunction.Crossover = new IndependentCrossover<RealVector, RealVectorEncoding, TestFunctionProblem>(); // ok
    independentAlgorithmWithTestFunction.Crossover = new PermutationSpecificCrossover<PermutationProblem>(); // error: incompatible encoding
    independentAlgorithmWithTestFunction.Crossover = new TspSpecificCrossover(); // error: incompatible encoding, problem independent algorithm
    independentAlgorithmWithTestFunction.Crossover = new RealVectorSpecificCrossover<TestFunctionProblem>(); // ok
    independentAlgorithmWithTestFunction.Crossover = new TestFunctionProblemSpecificCrossover(); // ok
    
    var permutationSpecificAlgorithmWithPermutationProblem = new PermutationEncodingSpecificAlgorithm<PermutationProblem>();
    permutationSpecificAlgorithmWithPermutationProblem.Run(new TravelingSalesmanProblem()); // ok
    permutationSpecificAlgorithmWithPermutationProblem.Run(new TestFunctionProblem()); // error: incompatible encoding
    permutationSpecificAlgorithmWithPermutationProblem.Crossover = new IndependentCrossover<Permutation, PermutationEncoding, PermutationProblem>(); // ok
    permutationSpecificAlgorithmWithPermutationProblem.Crossover = new IndependentCrossover<Permutation, PermutationEncoding, TravelingSalesmanProblem>(); // ok
    permutationSpecificAlgorithmWithPermutationProblem.Crossover = new PermutationSpecificCrossover<PermutationProblem>(); // ok
    permutationSpecificAlgorithmWithPermutationProblem.Crossover = new PermutationSpecificCrossover<TravelingSalesmanProblem>(); // ok
    permutationSpecificAlgorithmWithPermutationProblem.Crossover = new TspSpecificCrossover(); // error: problem independent algorithm
    permutationSpecificAlgorithmWithPermutationProblem.Crossover = new RealVectorSpecificCrossover(); // error: incompatible encoding
    permutationSpecificAlgorithmWithPermutationProblem.Crossover = new TestFunctionProblemSpecificCrossover(); // error: incompatible encoding, problem independent algorithm
    
    var permutationSpecificAlgorithmWithTsp = new PermutationEncodingSpecificAlgorithm<TravelingSalesmanProblem>();
    permutationSpecificAlgorithmWithTsp.Run(new TravelingSalesmanProblem()); // ok
    permutationSpecificAlgorithmWithTsp.Run(new TestFunctionProblem()); // error: incompatible encoding
    permutationSpecificAlgorithmWithTsp.Crossover = new IndependentCrossover<Permutation, PermutationEncoding, PermutationProblem>(); // ok
    permutationSpecificAlgorithmWithTsp.Crossover = new IndependentCrossover<Permutation, PermutationEncoding, TravelingSalesmanProblem>(); // ok
    permutationSpecificAlgorithmWithTsp.Crossover = new PermutationSpecificCrossover<PermutationProblem>(); // ok
    permutationSpecificAlgorithmWithTsp.Crossover = new PermutationSpecificCrossover<TravelingSalesmanProblem>(); // ok
    permutationSpecificAlgorithmWithTsp.Crossover = new TspSpecificCrossover(); // error: problem independent algorithm
    permutationSpecificAlgorithmWithTsp.Crossover = new RealVectorSpecificCrossover<RealVectorProblem>(); // error: incompatible encoding
    permutationSpecificAlgorithmWithTsp.Crossover = new TestFunctionProblemSpecificCrossover(); // error: incompatible encoding, problem independent algorithm
   
    var tspSpecificAlgorithm = new TravelingSalesmanProblemSpecificAlgorithm();
    tspSpecificAlgorithm.Run(new TravelingSalesmanProblem()); // ok
    tspSpecificAlgorithm.Run(new TestFunctionProblem()); // error: incompatible encoding, incompatible problem
    tspSpecificAlgorithm.Crossover = new IndependentCrossover<Permutation, PermutationEncoding, TravelingSalesmanProblem>(); // ok
    tspSpecificAlgorithm.Crossover = new IndependentCrossover<Permutation, PermutationEncoding, PermutationProblem>(); // ok
    tspSpecificAlgorithm.Crossover = new PermutationSpecificCrossover<TravelingSalesmanProblem>(); // ok
    tspSpecificAlgorithm.Crossover = new PermutationSpecificCrossover<PermutationProblem>(); // ok
    tspSpecificAlgorithm.Crossover = new TspSpecificCrossover(); // ok
    tspSpecificAlgorithm.Crossover = new RealVectorSpecificCrossover<RealVectorProblem>(); // error: incompatible encoding
    tspSpecificAlgorithm.Crossover = new TestFunctionProblemSpecificCrossover(); // error: incompatible encoding, incompatible problem
    
    var realVectorSpecificAlgorithm = new RealVectorEncodingSpecificAlgorithm();
    realVectorSpecificAlgorithm.Run(new TravelingSalesmanProblem()); // error: incompatible encoding
    realVectorSpecificAlgorithm.Run(new TestFunctionProblem()); // ok
    realVectorSpecificAlgorithm.Crossover = new IndependentCrossover(); // ok
    realVectorSpecificAlgorithm.Crossover = new PermutationSpecificCrossover(); // error: incompatible encoding
    realVectorSpecificAlgorithm.Crossover = new TspSpecificCrossover(); // error: incompatible encoding, problem independent algorithm
    realVectorSpecificAlgorithm.Crossover = new RealVectorSpecificCrossover(); // ok
    realVectorSpecificAlgorithm.Crossover = new TestFunctionProblemSpecificCrossover(); // error: problem independent algorithm
    
    var testFunctionProblemSpecificAlgorithm = new TestFunctionProblemSpecificAlgorithm();
    testFunctionProblemSpecificAlgorithm.Run(new TravelingSalesmanProblem()); // error: incompatible encoding, incompatible problem
    testFunctionProblemSpecificAlgorithm.Run(new TestFunctionProblem()); // ok
    testFunctionProblemSpecificAlgorithm.Crossover = new IndependentCrossover(); // ok
    testFunctionProblemSpecificAlgorithm.Crossover = new PermutationSpecificCrossover(); // error: incompatible encoding
    testFunctionProblemSpecificAlgorithm.Crossover = new TspSpecificCrossover(); // error: incompatible encoding, incompatible problem
    testFunctionProblemSpecificAlgorithm.Crossover = new RealVectorSpecificCrossover(); // ok
    testFunctionProblemSpecificAlgorithm.Crossover = new TestFunctionProblemSpecificCrossover(); // ok
    
  }
}
