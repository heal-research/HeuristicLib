
using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Genotypes;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib._ProofOfConcept.CompatabilityUsingImplicitConversions;

public interface IProblem
{
}

public interface IProblem<in TGenotype, out TEncoding> : IProblem
  where TEncoding : IEncoding<TGenotype>
{
  TEncoding Encoding { get; }
  double Evaluate(TGenotype genotype);
}

public class TravelingSalesmanProblem : IProblem<Permutation, PermutationEncoding>
{
  public PermutationEncoding Encoding { get; } = new PermutationEncoding(10);

  public double Evaluate(Permutation genotype) => throw new NotImplementedException();
}

public class TestFunctionProblem : IProblem<RealVector, RealVectorEncoding>
{
  public RealVectorEncoding Encoding { get; } = new RealVectorEncoding(10, new RealVector(10), new RealVector(10));

  public double Evaluate(RealVector genotype) => throw new NotImplementedException();
}

public interface ICrossover
{
  TGenotype Cross<TGenotype>(TGenotype parent1, TGenotype parent2);
}

public abstract class BaseCrossover : ICrossover
{
  public abstract TGenotype Cross<TGenotype>(TGenotype parent1, TGenotype parent2);
  
  public class Adapter<TGenotype, TEncoding>(ICrossover crossover) : BaseCrossover<TGenotype, TEncoding>
    where TEncoding : IEncoding<TGenotype>
  {
    public override TGenotype Cross(TGenotype parent1, TGenotype parent2, TEncoding encoding) => crossover.Cross(parent1, parent2);
  }

  public class Adapter<TGenotype, TEncoding, TProblem>(ICrossover crossover) : BaseCrossover<TGenotype, TEncoding, TProblem>
    where TEncoding : IEncoding<TGenotype> 
    where TProblem : IProblem<TGenotype, TEncoding>
  {
    public override TGenotype Cross(TGenotype parent1, TGenotype parent2, TEncoding encoding, TProblem problem) => crossover.Cross(parent1, parent2);
  }
}

public interface ICrossover<TGenotype, in TEncoding>
  where TEncoding : IEncoding<TGenotype>
{
  TGenotype Cross(TGenotype parent1, TGenotype parent2, TEncoding encoding);
}

public abstract class BaseCrossover<TGenotype, TEncoding> : ICrossover<TGenotype, TEncoding>
  where TEncoding : IEncoding<TGenotype>
{
  public abstract TGenotype Cross(TGenotype parent1, TGenotype parent2, TEncoding encoding);
  
  [return: NotNullIfNotNull(nameof(crossover))]
  public static implicit operator BaseCrossover<TGenotype, TEncoding>?(BaseCrossover? crossover) {
    return crossover is null ? null : new BaseCrossover.Adapter<TGenotype, TEncoding>(crossover);
  }
  
  public class Adapter<TProblem>(ICrossover<TGenotype, TEncoding> crossover) : BaseCrossover<TGenotype, TEncoding, TProblem>
    where TProblem : IProblem<TGenotype, TEncoding>
  {
    public override TGenotype Cross(TGenotype parent1, TGenotype parent2, TEncoding encoding, TProblem problem) => crossover.Cross(parent1, parent2, encoding);
  }
}

public interface ICrossover<TGenotype, in TEncoding, in TProblem>
  where TEncoding : IEncoding<TGenotype>
  where TProblem : IProblem<TGenotype, TEncoding>
{
  TGenotype Cross(TGenotype parent1, TGenotype parent2, TEncoding encoding, TProblem problem);
}

public abstract class BaseCrossover<TGenotype, TEncoding, TProblem> : ICrossover<TGenotype, TEncoding, TProblem>
  where TEncoding : IEncoding<TGenotype>
  where TProblem : IProblem<TGenotype, TEncoding>
{
  public abstract TGenotype Cross(TGenotype parent1, TGenotype parent2, TEncoding encoding, TProblem problem);
  
  [return: NotNullIfNotNull(nameof(crossover))]
  public static implicit operator BaseCrossover<TGenotype, TEncoding, TProblem>?(BaseCrossover? crossover) {
    return crossover is null ? null : new BaseCrossover.Adapter<TGenotype, TEncoding, TProblem>(crossover);
  }
  
  [return: NotNullIfNotNull(nameof(crossover))]
  public static implicit operator BaseCrossover<TGenotype, TEncoding, TProblem>?(BaseCrossover<TGenotype, TEncoding>? crossover) {
    return crossover is null ? null : new BaseCrossover<TGenotype, TEncoding>.Adapter<TProblem>(crossover);
  }
}

public class IndependentCrossover : BaseCrossover
{
  public override TGenotype Cross<TGenotype>(TGenotype parent1, TGenotype parent2) => throw new NotImplementedException();
}

public class PermutationSpecificCrossover : BaseCrossover<Permutation, PermutationEncoding>
{
  public override Permutation Cross(Permutation parent1, Permutation parent2, PermutationEncoding encoding) => throw new NotImplementedException();
}

public class TspSpecificCrossover : BaseCrossover<Permutation, PermutationEncoding, TravelingSalesmanProblem>
{
  public override Permutation Cross(Permutation parent1, Permutation parent2, PermutationEncoding encoding, TravelingSalesmanProblem problem) => throw new NotImplementedException();
}

public class RealVectorSpecificCrossover : BaseCrossover<RealVector, RealVectorEncoding>
{
  public override RealVector Cross(RealVector parent1, RealVector parent2, RealVectorEncoding encoding) => throw new NotImplementedException();
}

public class TestFunctionProblemSpecificCrossover : BaseCrossover<RealVector, RealVectorEncoding, TestFunctionProblem>
{
  public override RealVector Cross(RealVector parent1, RealVector parent2, RealVectorEncoding encoding, TestFunctionProblem problem) => throw new NotImplementedException();
}

public record IterationState<TGenotype>
{
  public required ImmutableList<TGenotype> Population { get; init; }
}

public interface IAlgorithm
{
}

public interface IAlgorithm<TGenotype, in TEncoding> 
  where TEncoding : IEncoding<TGenotype>
{
  IterationState<TGenotype> Run<TProblem>(TProblem problem, IterationState<TGenotype>? initialState = null) where TProblem : IProblem<TGenotype, TEncoding>;
}

public interface IAlgorithm<TGenotype, in TEncoding, in TProblem> 
  where TEncoding : IEncoding<TGenotype>
  where TProblem : IProblem<TGenotype, TEncoding>
{
  IterationState<TGenotype> Run(TProblem problem, IterationState<TGenotype>? initialState = null);
}

public class EncodingIndependentAlgorithm<TGenotype, TEncoding> : IAlgorithm<TGenotype, TEncoding>
  where TEncoding : IEncoding<TGenotype>
{
  public ICrossover<TGenotype, TEncoding> Crossover { get; set; }
  public IterationState<TGenotype> Run<TProblem>(TProblem problem, IterationState<TGenotype>? initialState = null)
    where TProblem : IProblem<TGenotype, TEncoding> => throw new NotImplementedException();
}

public class PermutationEncodingSpecificAlgorithm : IAlgorithm<Permutation, PermutationEncoding>
{
  public ICrossover<Permutation, PermutationEncoding> Crossover { get; set; }
  public IterationState<Permutation> Run<TProblem>(TProblem problem, IterationState<Permutation>? initialState = null) where TProblem : IProblem<Permutation, PermutationEncoding> => throw new NotImplementedException();
}

public class TravelingSalesmanProblemSpecificAlgorithm : IAlgorithm<Permutation, PermutationEncoding, TravelingSalesmanProblem>
{
  public ICrossover<Permutation, PermutationEncoding, TravelingSalesmanProblem> Crossover { get; set; }
  public IterationState<Permutation> Run(TravelingSalesmanProblem problem, IterationState<Permutation>? initialState = null) => throw new NotImplementedException();
}

public class RealVectorEncodingSpecificAlgorithm : IAlgorithm<RealVector, RealVectorEncoding>
{
  public ICrossover<RealVector, RealVectorEncoding> Crossover { get; set; }
  public IterationState<RealVector> Run<TProblem>(TProblem problem, IterationState<RealVector>? initialState = null) where TProblem : IProblem<RealVector, RealVectorEncoding> => throw new NotImplementedException();
}

public class TestFunctionProblemSpecificAlgorithm : IAlgorithm<RealVector, RealVectorEncoding, TestFunctionProblem>
{
  public ICrossover<RealVector, RealVectorEncoding, TestFunctionProblem> Crossover { get; set; }
  public IterationState<RealVector> Run(TestFunctionProblem problem, IterationState<RealVector>? initialState = null) => throw new NotImplementedException();
}

public class Test
{
  static void TestCompatability() {

    var independentAlgorithmWithPermutation = new EncodingIndependentAlgorithm<Permutation, PermutationEncoding>();
    independentAlgorithmWithPermutation.Run(new TravelingSalesmanProblem()); //ok
    independentAlgorithmWithPermutation.Run(new TestFunctionProblem()); // error: incompatible encoding
    independentAlgorithmWithPermutation.Crossover = new IndependentCrossover(); // ok
    independentAlgorithmWithPermutation.Crossover = new BaseCrossover.Adapter<Permutation, PermutationEncoding>(new IndependentCrossover()); // ok
    independentAlgorithmWithPermutation.Crossover = new PermutationSpecificCrossover(); // ok
    independentAlgorithmWithPermutation.Crossover = new TspSpecificCrossover(); // error: problem independent algorithm
    independentAlgorithmWithPermutation.Crossover = new RealVectorSpecificCrossover(); // error: incompatible encoding
    independentAlgorithmWithPermutation.Crossover = new TestFunctionProblemSpecificCrossover(); // error: incompatible encoding, problem independent algorithm
    
    var independentAlgorithmWithRealVector = new EncodingIndependentAlgorithm<RealVector, RealVectorEncoding>();
    independentAlgorithmWithRealVector.Run(new TravelingSalesmanProblem()); // error: incompatible encoding
    independentAlgorithmWithRealVector.Run(new TestFunctionProblem()); // ok
    independentAlgorithmWithRealVector.Crossover = new IndependentCrossover(); // ok
    independentAlgorithmWithRealVector.Crossover = new PermutationSpecificCrossover(); // error: incompatible encoding
    independentAlgorithmWithRealVector.Crossover = new TspSpecificCrossover(); // error: incompatible encoding, problem independent algorithm
    independentAlgorithmWithRealVector.Crossover = new RealVectorSpecificCrossover(); // ok
    independentAlgorithmWithRealVector.Crossover = new TestFunctionProblemSpecificCrossover(); // error: problem independent algorithm
    
    var permutationSpecificAlgorithm = new PermutationEncodingSpecificAlgorithm();
    permutationSpecificAlgorithm.Run(new TravelingSalesmanProblem()); // ok
    permutationSpecificAlgorithm.Run(new TestFunctionProblem()); // error: incompatible encoding
    permutationSpecificAlgorithm.Crossover = new IndependentCrossover(); // ok
    permutationSpecificAlgorithm.Crossover = new PermutationSpecificCrossover(); // ok
    permutationSpecificAlgorithm.Crossover = new TspSpecificCrossover(); // error: problem independent algorithm
    permutationSpecificAlgorithm.Crossover = new RealVectorSpecificCrossover(); // error: incompatible encoding
    permutationSpecificAlgorithm.Crossover = new TestFunctionProblemSpecificCrossover(); // error: incompatible encoding, problem independent algorithm
    
    var tspSpecificAlgorithm = new TravelingSalesmanProblemSpecificAlgorithm();
    tspSpecificAlgorithm.Run(new TravelingSalesmanProblem()); // ok
    tspSpecificAlgorithm.Run(new TestFunctionProblem()); // error: incompatible encoding, incompatible problem
    tspSpecificAlgorithm.Crossover = new IndependentCrossover(); // ok
    tspSpecificAlgorithm.Crossover = new BaseCrossover.Adapter<Permutation, PermutationEncoding, TravelingSalesmanProblem>(new IndependentCrossover()); // ok
    tspSpecificAlgorithm.Crossover = new BaseCrossover<Permutation, PermutationEncoding>.Adapter<TravelingSalesmanProblem>(new IndependentCrossover()); // ok
    tspSpecificAlgorithm.Crossover = new PermutationSpecificCrossover(); // ok
    tspSpecificAlgorithm.Crossover = new TspSpecificCrossover(); // ok
    tspSpecificAlgorithm.Crossover = new RealVectorSpecificCrossover(); // error: incompatible encoding
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
