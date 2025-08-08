using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Genotypes;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Problems.TravelingSalesman;

namespace HEAL.HeuristicLib._ProofOfConcept.CompatabilityUsingConvenienceBaseClasses;


public interface ICrossover<TGenotype, in TEncoding, in TProblem>
  where TEncoding : IEncoding<TGenotype>
  where TProblem : IProblem<TGenotype, TEncoding>
{
  TGenotype Cross(TGenotype parent1, TGenotype parent2, TEncoding encoding, TProblem problem);
}

public abstract class Crossover<TGenotype, TEncoding, TProblem> : ICrossover<TGenotype, TEncoding, TProblem>
  where TEncoding : IEncoding<TGenotype>
  where TProblem : IProblem<TGenotype, TEncoding>
{
  public abstract TGenotype Cross(TGenotype parent1, TGenotype parent2, TEncoding encoding, TProblem problem);
}

public abstract class Crossover<TGenotype, TEncoding> : ICrossover<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>>
  where TEncoding : IEncoding<TGenotype>
{
  TGenotype ICrossover<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>>.Cross(TGenotype parent1, TGenotype parent2, TEncoding encoding, IProblem<TGenotype, TEncoding> problem) 
    => Cross(parent1, parent2, encoding);

  public abstract TGenotype Cross(TGenotype parent1, TGenotype parent2, TEncoding encoding);
}

public abstract class Crossover<TGenotype> : ICrossover<TGenotype, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>>
{
  TGenotype ICrossover<TGenotype, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>>.Cross(TGenotype parent1, TGenotype parent2, IEncoding<TGenotype> encoding, IProblem<TGenotype, IEncoding<TGenotype>> problem) 
    => Cross(parent1, parent2);

  public abstract TGenotype Cross(TGenotype parent1, TGenotype parent2);
}

public class IndependentCrossover<TGenotype> : Crossover<TGenotype>
{
  public override TGenotype Cross(TGenotype parent1, TGenotype parent2) => throw new NotImplementedException();
}

public class PermutationSpecificCrossover : Crossover<Permutation, PermutationEncoding>
{
  public override Permutation Cross(Permutation parent1, Permutation parent2, PermutationEncoding encoding) => throw new NotImplementedException();
}

public class TspSpecificCrossover : Crossover<Permutation, PermutationEncoding, TravelingSalesmanProblem>
{
  public override Permutation Cross(Permutation parent1, Permutation parent2, PermutationEncoding encoding, TravelingSalesmanProblem problem) => throw new NotImplementedException();
}

public class RealVectorSpecificCrossover : Crossover<RealVector, RealVectorEncoding>
{
  public override RealVector Cross(RealVector parent1, RealVector parent2, RealVectorEncoding encoding) => throw new NotImplementedException();
}

public class TestFunctionProblemSpecificCrossover : Crossover<RealVector, RealVectorEncoding, TestFunctionProblem>
{
  public override RealVector Cross(RealVector parent1, RealVector parent2, RealVectorEncoding encoding, TestFunctionProblem problem) => throw new NotImplementedException();
}

public record IterationState<TGenotype>
{
  public TGenotype[] Population { get; init; }
}


public interface IAlgorithm<TGenotype, in TEncoding, in TProblem> 
  where TEncoding : IEncoding<TGenotype>
  where TProblem : IProblem<TGenotype, TEncoding>
{
  IterationState<TGenotype> Run(TProblem problem, IterationState<TGenotype>? initialState = null);
}

public abstract class Algorithm<TGenotype, TEncoding, TProblem> : IAlgorithm<TGenotype, TEncoding, TProblem>
  where TEncoding : IEncoding<TGenotype>
  where TProblem : IProblem<TGenotype, TEncoding>
{
  public abstract IterationState<TGenotype> Run(TProblem problem, IterationState<TGenotype>? initialState = null);
}


public class IndependentAlgorithm<TGenotype, TEncoding, TProblem> : Algorithm<TGenotype, TEncoding, TProblem>
  where TEncoding : IEncoding<TGenotype>
  where TProblem : IProblem<TGenotype, TEncoding>
{
  public ICrossover<TGenotype, TEncoding, TProblem> Crossover { get; set; }
  public override IterationState<TGenotype> Run(TProblem problem, IterationState<TGenotype>? initialState = null) => throw new NotImplementedException();
}

public class IndependentAlgorithm<TGenotype, TEncoding> : IndependentAlgorithm<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>>
  where TEncoding : IEncoding<TGenotype>
{
}

public class IndependentAlgorithm<TGenotype> : IndependentAlgorithm<TGenotype, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>>
{
}

public class PermutationEncodingSpecificAlgorithm<TProblem> : Algorithm<Permutation, PermutationEncoding, TProblem>
  where TProblem : IProblem<Permutation, PermutationEncoding>
{
  public ICrossover<Permutation, PermutationEncoding, TProblem> Crossover { get; set; }
  public override IterationState<Permutation> Run(TProblem problem, IterationState<Permutation>? initialState = null) => throw new NotImplementedException();
}

public class PermutationEncodingSpecificAlgorithm : PermutationEncodingSpecificAlgorithm<IProblem<Permutation, PermutationEncoding>>
{
}


public class TravelingSalesmanProblemSpecificAlgorithm : Algorithm<Permutation, PermutationEncoding, TravelingSalesmanProblem>
{
  public ICrossover<Permutation, PermutationEncoding, TravelingSalesmanProblem> Crossover { get; set; }
  public override IterationState<Permutation> Run(TravelingSalesmanProblem problem, IterationState<Permutation>? initialState = null) => throw new NotImplementedException();
}


public class RealVectorEncodingSpecificAlgorithm<TProblem> : Algorithm<RealVector, RealVectorEncoding, TProblem>
  where TProblem : IProblem<RealVector, RealVectorEncoding>
{
  public ICrossover<RealVector, RealVectorEncoding, TProblem> Crossover { get; set; }
  public override IterationState<RealVector> Run(TProblem problem, IterationState<RealVector>? initialState = null) => throw new NotImplementedException();
}

public class RealVectorEncodingSpecificAlgorithm : RealVectorEncodingSpecificAlgorithm<IProblem<RealVector, RealVectorEncoding>>
{
}


public class TestFunctionProblemSpecificAlgorithm : Algorithm<RealVector, RealVectorEncoding, TestFunctionProblem>
{
  public ICrossover<RealVector, RealVectorEncoding, TestFunctionProblem> Crossover { get; set; }
  public override IterationState<RealVector> Run(TestFunctionProblem problem, IterationState<RealVector>? initialState = null) => throw new NotImplementedException();
}

public class Test
{
  static void TestCompatability() {

    var independentAlgorithmForPermutationWithoutEncoding = new IndependentAlgorithm<Permutation>();
    independentAlgorithmForPermutationWithoutEncoding.Run(new TravelingSalesmanProblem()); // ok
    independentAlgorithmForPermutationWithoutEncoding.Run(new TestFunctionProblem()); // error: incompatible problem (wrong encoding)
    independentAlgorithmForPermutationWithoutEncoding.Crossover = new IndependentCrossover<Permutation>(); // ok
    independentAlgorithmForPermutationWithoutEncoding.Crossover = new IndependentCrossover<RealVector>(); // wrong encoding
    independentAlgorithmForPermutationWithoutEncoding.Crossover = new PermutationSpecificCrossover(); // wrong encoding
    independentAlgorithmForPermutationWithoutEncoding.Crossover = new TspSpecificCrossover(); // error: incompatible problem & incompatible encoding
    independentAlgorithmForPermutationWithoutEncoding.Crossover = new RealVectorSpecificCrossover(); // error: incompatible encoding
    independentAlgorithmForPermutationWithoutEncoding.Crossover = new TestFunctionProblemSpecificCrossover(); // error: incompatible encoding & incompatible problem
    
    var independentAlgorithmForRealVectorWithoutEncoding = new IndependentAlgorithm<RealVector>();
    independentAlgorithmForRealVectorWithoutEncoding.Run(new TravelingSalesmanProblem()); // error: incompatible problem (wrong encoding)
    independentAlgorithmForRealVectorWithoutEncoding.Run(new TestFunctionProblem()); // ok
    independentAlgorithmForRealVectorWithoutEncoding.Crossover = new IndependentCrossover<Permutation>(); // wrong encoding
    independentAlgorithmForRealVectorWithoutEncoding.Crossover = new IndependentCrossover<RealVector>(); // ok
    independentAlgorithmForRealVectorWithoutEncoding.Crossover = new PermutationSpecificCrossover(); // error: incompatible encoding
    independentAlgorithmForRealVectorWithoutEncoding.Crossover = new TspSpecificCrossover(); // error: incompatible encoding & incompatible problem
    independentAlgorithmForRealVectorWithoutEncoding.Crossover = new RealVectorSpecificCrossover(); // error: incompatible encoding
    independentAlgorithmForRealVectorWithoutEncoding.Crossover = new TestFunctionProblemSpecificCrossover(); // error: incompatible encoding & incompatible problem
    
    // INDEPENDENT ALGORITHMS
    
    var independentAlgorithmForPermutationWithoutProblem = new IndependentAlgorithm<Permutation, PermutationEncoding>();
    independentAlgorithmForPermutationWithoutProblem.Run(new TravelingSalesmanProblem()); // ok
    independentAlgorithmForPermutationWithoutProblem.Run(new TestFunctionProblem()); // error: incompatible problem (wrong encoding)
    independentAlgorithmForPermutationWithoutProblem.Crossover = new IndependentCrossover<Permutation>(); // ok
    independentAlgorithmForPermutationWithoutProblem.Crossover = new IndependentCrossover<RealVector>(); // wrong encoding
    independentAlgorithmForPermutationWithoutProblem.Crossover = new PermutationSpecificCrossover(); // ok
    independentAlgorithmForPermutationWithoutProblem.Crossover = new TspSpecificCrossover(); // error: incompatible problem
    independentAlgorithmForPermutationWithoutProblem.Crossover = new RealVectorSpecificCrossover(); // error: incompatible encoding
    independentAlgorithmForPermutationWithoutProblem.Crossover = new TestFunctionProblemSpecificCrossover(); // error: incompatible encoding & incompatible problem
    
    var independentAlgorithmForPermutationWithPermutationProblem = new IndependentAlgorithm<Permutation, PermutationEncoding, PermutationProblem>();
    independentAlgorithmForPermutationWithPermutationProblem.Run(new TravelingSalesmanProblem()); // ok
    independentAlgorithmForPermutationWithPermutationProblem.Run(new TestFunctionProblem()); // error: incompatible problem (wrong encoding)
    independentAlgorithmForPermutationWithPermutationProblem.Crossover = new IndependentCrossover<Permutation>(); // ok
    independentAlgorithmForPermutationWithPermutationProblem.Crossover = new IndependentCrossover<RealVector>(); // wrong encoding
    independentAlgorithmForPermutationWithPermutationProblem.Crossover = new PermutationSpecificCrossover(); // ok
    independentAlgorithmForPermutationWithPermutationProblem.Crossover = new TspSpecificCrossover(); // error: incompatible problem
    independentAlgorithmForPermutationWithPermutationProblem.Crossover = new RealVectorSpecificCrossover(); // error: incompatible encoding
    independentAlgorithmForPermutationWithPermutationProblem.Crossover = new TestFunctionProblemSpecificCrossover(); // error: incompatible encoding & incompatible problem
    
    var independentAlgorithmForPermutationWithTravelingSalesmanProblem = new IndependentAlgorithm<Permutation, PermutationEncoding, TravelingSalesmanProblem>();
    independentAlgorithmForPermutationWithTravelingSalesmanProblem.Run(new TravelingSalesmanProblem()); // ok
    independentAlgorithmForPermutationWithTravelingSalesmanProblem.Run(new TestFunctionProblem()); // error: incompatible problem (wrong encoding)
    independentAlgorithmForPermutationWithTravelingSalesmanProblem.Crossover = new IndependentCrossover<Permutation>(); // ok
    independentAlgorithmForPermutationWithTravelingSalesmanProblem.Crossover = new IndependentCrossover<RealVector>(); // wrong encoding
    independentAlgorithmForPermutationWithTravelingSalesmanProblem.Crossover = new PermutationSpecificCrossover(); // ok
    independentAlgorithmForPermutationWithTravelingSalesmanProblem.Crossover = new TspSpecificCrossover(); // ok
    independentAlgorithmForPermutationWithTravelingSalesmanProblem.Crossover = new RealVectorSpecificCrossover(); // error: incompatible encoding
    independentAlgorithmForPermutationWithTravelingSalesmanProblem.Crossover = new TestFunctionProblemSpecificCrossover(); // error: incompatible encoding & incompatible problem
    
    
    var independentAlgorithmForRealVectorWithoutProblem = new IndependentAlgorithm<RealVector, RealVectorEncoding>();
    independentAlgorithmForRealVectorWithoutProblem.Run(new TravelingSalesmanProblem()); // error: incompatible problem (wrong encoding)
    independentAlgorithmForRealVectorWithoutProblem.Run(new TestFunctionProblem()); // ok
    independentAlgorithmForRealVectorWithoutProblem.Crossover = new IndependentCrossover<Permutation>(); // wrong encoding
    independentAlgorithmForRealVectorWithoutProblem.Crossover = new IndependentCrossover<RealVector>(); // ok
    independentAlgorithmForRealVectorWithoutProblem.Crossover = new PermutationSpecificCrossover(); // error: incompatible encoding
    independentAlgorithmForRealVectorWithoutProblem.Crossover = new TspSpecificCrossover(); // error: incompatible encoding & incompatible problem
    independentAlgorithmForRealVectorWithoutProblem.Crossover = new RealVectorSpecificCrossover(); // ok
    independentAlgorithmForRealVectorWithoutProblem.Crossover = new TestFunctionProblemSpecificCrossover(); // error: wrong problem
    
    var independentAlgorithmForRealVectorWithRealVectorProblem = new IndependentAlgorithm<RealVector, RealVectorEncoding, RealVectorProblem>();
    independentAlgorithmForRealVectorWithRealVectorProblem.Run(new TravelingSalesmanProblem()); // error: incompatible problem (wrong encoding)
    independentAlgorithmForRealVectorWithRealVectorProblem.Run(new TestFunctionProblem()); // ok
    independentAlgorithmForRealVectorWithRealVectorProblem.Crossover = new IndependentCrossover<Permutation>(); // wrong encoding
    independentAlgorithmForRealVectorWithRealVectorProblem.Crossover = new IndependentCrossover<RealVector>(); // ok
    independentAlgorithmForRealVectorWithRealVectorProblem.Crossover = new PermutationSpecificCrossover(); // error: incompatible encoding
    independentAlgorithmForRealVectorWithRealVectorProblem.Crossover = new TspSpecificCrossover(); // error: incompatible encoding & incompatible problem
    independentAlgorithmForRealVectorWithRealVectorProblem.Crossover = new RealVectorSpecificCrossover(); // ok
    independentAlgorithmForRealVectorWithRealVectorProblem.Crossover = new TestFunctionProblemSpecificCrossover(); // error: wrong problem
    
    var independentAlgorithmForRealVectorWithTestFunctionProblem = new IndependentAlgorithm<RealVector, RealVectorEncoding, TestFunctionProblem>();
    independentAlgorithmForRealVectorWithTestFunctionProblem.Run(new TravelingSalesmanProblem()); // error: incompatible problem (wrong encoding)
    independentAlgorithmForRealVectorWithTestFunctionProblem.Run(new TestFunctionProblem()); // ok
    independentAlgorithmForRealVectorWithTestFunctionProblem.Crossover = new IndependentCrossover<Permutation>(); // wrong encoding
    independentAlgorithmForRealVectorWithTestFunctionProblem.Crossover = new IndependentCrossover<RealVector>(); // ok
    independentAlgorithmForRealVectorWithTestFunctionProblem.Crossover = new PermutationSpecificCrossover(); // error: incompatible encoding
    independentAlgorithmForRealVectorWithTestFunctionProblem.Crossover = new TspSpecificCrossover(); // error: incompatible encoding & incompatible problem
    independentAlgorithmForRealVectorWithTestFunctionProblem.Crossover = new RealVectorSpecificCrossover(); // ok
    independentAlgorithmForRealVectorWithTestFunctionProblem.Crossover = new TestFunctionProblemSpecificCrossover(); // ok
    
    // ENCODING SPECIFIC ALGORITHMS

    var permutationAlgorithmWithoutProblem = new PermutationEncodingSpecificAlgorithm();
    permutationAlgorithmWithoutProblem.Run(new TravelingSalesmanProblem()); // ok
    permutationAlgorithmWithoutProblem.Run(new TestFunctionProblem()); // error: incompatible problem (wrong encoding)
    permutationAlgorithmWithoutProblem.Crossover = new IndependentCrossover<Permutation>(); // ok
    permutationAlgorithmWithoutProblem.Crossover = new IndependentCrossover<RealVector>(); // error: incompatible encoding
    permutationAlgorithmWithoutProblem.Crossover = new PermutationSpecificCrossover(); // ok
    permutationAlgorithmWithoutProblem.Crossover = new TspSpecificCrossover(); // error: incompatible problem
    permutationAlgorithmWithoutProblem.Crossover = new RealVectorSpecificCrossover(); // error: incompatible encoding
    permutationAlgorithmWithoutProblem.Crossover = new TestFunctionProblemSpecificCrossover(); // error: incompatible encoding & incompatible problem
    
    var permutationAlgorithmWithPermutationProblem = new PermutationEncodingSpecificAlgorithm<PermutationProblem>();
    permutationAlgorithmWithPermutationProblem.Run(new TravelingSalesmanProblem()); // ok
    permutationAlgorithmWithPermutationProblem.Run(new TestFunctionProblem()); // error: incompatible problem (wrong encoding)
    permutationAlgorithmWithPermutationProblem.Crossover = new IndependentCrossover<Permutation>(); // ok
    permutationAlgorithmWithPermutationProblem.Crossover = new IndependentCrossover<RealVector>(); // error: incompatible encoding
    permutationAlgorithmWithPermutationProblem.Crossover = new PermutationSpecificCrossover(); // ok
    permutationAlgorithmWithPermutationProblem.Crossover = new TspSpecificCrossover(); // error: incompatible problem
    permutationAlgorithmWithPermutationProblem.Crossover = new RealVectorSpecificCrossover(); // error: incompatible encoding
    permutationAlgorithmWithPermutationProblem.Crossover = new TestFunctionProblemSpecificCrossover(); // error: incompatible encoding & incompatible problem
    
    var permutationAlgorithmWithTravelingSalesmanProblem = new PermutationEncodingSpecificAlgorithm<TravelingSalesmanProblem>();
    permutationAlgorithmWithTravelingSalesmanProblem.Run(new TravelingSalesmanProblem()); // ok
    permutationAlgorithmWithTravelingSalesmanProblem.Run(new TestFunctionProblem()); // error: incompatible problem (wrong encoding)
    permutationAlgorithmWithTravelingSalesmanProblem.Crossover = new IndependentCrossover<Permutation>(); // ok
    permutationAlgorithmWithTravelingSalesmanProblem.Crossover = new IndependentCrossover<RealVector>(); // error: incompatible encoding
    permutationAlgorithmWithTravelingSalesmanProblem.Crossover = new PermutationSpecificCrossover(); // ok
    permutationAlgorithmWithTravelingSalesmanProblem.Crossover = new TspSpecificCrossover(); // ok
    permutationAlgorithmWithTravelingSalesmanProblem.Crossover = new RealVectorSpecificCrossover(); // error: incompatible encoding
    permutationAlgorithmWithTravelingSalesmanProblem.Crossover = new TestFunctionProblemSpecificCrossover(); // error: incompatible encoding & incompatible problem
    
    
    var realVectorAlgorithmWithoutProblem = new RealVectorEncodingSpecificAlgorithm();
    realVectorAlgorithmWithoutProblem.Run(new TravelingSalesmanProblem()); // error: incompatible problem (wrong encoding)
    realVectorAlgorithmWithoutProblem.Run(new TestFunctionProblem()); // ok
    realVectorAlgorithmWithoutProblem.Crossover = new IndependentCrossover<Permutation>(); // error: incompatible encoding
    realVectorAlgorithmWithoutProblem.Crossover = new IndependentCrossover<RealVector>(); // ok
    realVectorAlgorithmWithoutProblem.Crossover = new PermutationSpecificCrossover(); // error: incompatible encoding
    realVectorAlgorithmWithoutProblem.Crossover = new TspSpecificCrossover(); // error: incompatible encoding & incompatible problem
    realVectorAlgorithmWithoutProblem.Crossover = new RealVectorSpecificCrossover(); // ok
    realVectorAlgorithmWithoutProblem.Crossover = new TestFunctionProblemSpecificCrossover(); // error: incompatible problem
    
    var realVectorAlgorithmWithRealVectorProblem = new RealVectorEncodingSpecificAlgorithm<RealVectorProblem>();
    realVectorAlgorithmWithRealVectorProblem.Run(new TravelingSalesmanProblem()); // error: incompatible problem (wrong encoding)
    realVectorAlgorithmWithRealVectorProblem.Run(new TestFunctionProblem()); // ok
    realVectorAlgorithmWithRealVectorProblem.Crossover = new IndependentCrossover<Permutation>(); // error: incompatible encoding
    realVectorAlgorithmWithRealVectorProblem.Crossover = new IndependentCrossover<RealVector>(); // ok
    realVectorAlgorithmWithRealVectorProblem.Crossover = new PermutationSpecificCrossover(); // error: incompatible encoding
    realVectorAlgorithmWithRealVectorProblem.Crossover = new TspSpecificCrossover(); // error: incompatible encoding & incompatible problem
    realVectorAlgorithmWithRealVectorProblem.Crossover = new RealVectorSpecificCrossover(); // ok
    realVectorAlgorithmWithRealVectorProblem.Crossover = new TestFunctionProblemSpecificCrossover(); // error: incompatible problem
    
    var realVectorAlgorithmWithTestFunctionProblem = new RealVectorEncodingSpecificAlgorithm<TestFunctionProblem>();
    realVectorAlgorithmWithTestFunctionProblem.Run(new TravelingSalesmanProblem()); // error: incompatible problem (wrong encoding)
    realVectorAlgorithmWithTestFunctionProblem.Run(new TestFunctionProblem()); // ok
    realVectorAlgorithmWithTestFunctionProblem.Crossover = new IndependentCrossover<Permutation>(); // error: incompatible encoding
    realVectorAlgorithmWithTestFunctionProblem.Crossover = new IndependentCrossover<RealVector>(); //
    realVectorAlgorithmWithTestFunctionProblem.Crossover = new PermutationSpecificCrossover(); // error: incompatible encoding
    realVectorAlgorithmWithTestFunctionProblem.Crossover = new TspSpecificCrossover(); // error: incompatible encoding & incompatible problem
    realVectorAlgorithmWithTestFunctionProblem.Crossover = new RealVectorSpecificCrossover(); // ok
    realVectorAlgorithmWithTestFunctionProblem.Crossover = new TestFunctionProblemSpecificCrossover(); // ok
    
    
    // PROBLEM SPECIFIC ALGORITHMS
    
    var tspSpecificAlgorithm = new TravelingSalesmanProblemSpecificAlgorithm();
    tspSpecificAlgorithm.Run(new TravelingSalesmanProblem()); // ok
    tspSpecificAlgorithm.Run(new TestFunctionProblem()); // error: incompatible problem (wrong encoding)
    tspSpecificAlgorithm.Crossover = new IndependentCrossover<Permutation>(); // ok
    tspSpecificAlgorithm.Crossover = new IndependentCrossover<RealVector>(); // error: incompatible encoding
    tspSpecificAlgorithm.Crossover = new PermutationSpecificCrossover(); // ok
    tspSpecificAlgorithm.Crossover = new TspSpecificCrossover(); // ok
    tspSpecificAlgorithm.Crossover = new RealVectorSpecificCrossover(); // error: incompatible encoding
    tspSpecificAlgorithm.Crossover = new TestFunctionProblemSpecificCrossover(); // error: incompatible encoding & incompatible problem
    
    var testFunctionProblemSpecificAlgorithm = new TestFunctionProblemSpecificAlgorithm();
    testFunctionProblemSpecificAlgorithm.Run(new TravelingSalesmanProblem()); // error: incompatible problem (wrong encoding)
    testFunctionProblemSpecificAlgorithm.Run(new TestFunctionProblem()); // ok
    testFunctionProblemSpecificAlgorithm.Crossover = new IndependentCrossover<Permutation>(); // error: incompatible encoding
    testFunctionProblemSpecificAlgorithm.Crossover = new IndependentCrossover<RealVector>(); // ok
    testFunctionProblemSpecificAlgorithm.Crossover = new PermutationSpecificCrossover(); // error: incompatible encoding
    testFunctionProblemSpecificAlgorithm.Crossover = new TspSpecificCrossover(); // error: incompatible encoding & incompatible problem
    testFunctionProblemSpecificAlgorithm.Crossover = new RealVectorSpecificCrossover(); // ok
    testFunctionProblemSpecificAlgorithm.Crossover = new TestFunctionProblemSpecificCrossover(); // ok
    
    
    
    
    
  }
}
