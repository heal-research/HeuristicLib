
using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Genotypes;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Problems.TravelingSalesman;

namespace HEAL.HeuristicLib._ProofOfConcept.CompatabilityUsingInstanceObjects;

// public interface ICrossover
// {
//   ICrossoverInstance CreateInstance(IEncoding encoding, IProblem problem, IAlgorithm algorithm);
// }

public interface ICrossover<TGenotype>
{
  ICrossoverInstance<TGenotype> CreateInstance(IEncoding encoding, IProblem problem, IAlgorithm algorithm);
}



// public interface ICrossoverInstance
// {
//   TGenotype Cross<TGenotype>(TGenotype parent1, TGenotype parent2);
// }

public interface ICrossoverInstance<TGenotype>
{
  TGenotype Cross(TGenotype parent1, TGenotype parent2);
}

// public record CrossoverAdapter<TGenotype>(ICrossover<TGenotype> Crossover) : Crossover<TGenotype>
// {
//   public override ICrossoverInstance<TGenotype> CreateInstance(IEncoding encoding, IProblem problem, IAlgorithm algorithm) {
//     return Crossover.CreateInstance(encoding, problem, algorithm);
//   }
// }


// public abstract record Crossover : ICrossover
// {
//   public abstract ICrossoverInstance CreateInstance(IEncoding encoding, IProblem problem, IAlgorithm algorithm);
// }

public abstract record Crossover<TGenotype> : ICrossover<TGenotype>
{
  public abstract ICrossoverInstance<TGenotype> CreateInstance(IEncoding encoding, IProblem problem, IAlgorithm algorithm);
}

public abstract record Crossover<TGenotype, TEncoding> : ICrossover<TGenotype>
  where TEncoding : IEncoding<TGenotype>
{
  ICrossoverInstance<TGenotype> ICrossover<TGenotype>.CreateInstance(IEncoding encoding, IProblem problem, IAlgorithm algorithm) {
    if (encoding is not TEncoding typedEncoding) throw new ArgumentException($"Expected encoding of type {typeof(TEncoding).Name}, but got {encoding.GetType().Name}.");
    return CreateInstance(typedEncoding, problem, algorithm);
  }
  public abstract ICrossoverInstance<TGenotype> CreateInstance(TEncoding encoding, IProblem problem, IAlgorithm algorithm);
}



public abstract record Crossover<TGenotype, TEncoding, TProblem> : ICrossover<TGenotype>
  where TEncoding : IEncoding<TGenotype>
  where TProblem : IProblem<TGenotype, TEncoding>
{
  ICrossoverInstance<TGenotype> ICrossover<TGenotype>.CreateInstance(IEncoding encoding, IProblem problem, IAlgorithm algorithm) {
    if (encoding is not TEncoding typedEncoding) throw new ArgumentException($"Expected encoding of type {typeof(TEncoding).Name}, but got {encoding.GetType().Name}.");
    if (problem is not TProblem typedProblem) throw new ArgumentException($"Expected problem of type {typeof(TProblem).Name}, but got {problem.GetType().Name}.");
    return CreateInstance(typedEncoding, typedProblem, algorithm);
  }
  public abstract ICrossoverInstance<TGenotype> CreateInstance(TEncoding encoding, TProblem problem, IAlgorithm algorithm);
}


public abstract class CrossoverInstance<TGenotype> : ICrossoverInstance<TGenotype>
{
  public abstract TGenotype Cross(TGenotype parent1, TGenotype parent2);
}

// public abstract class CrossoverInstance<TGenotype, TEncoding> : CrossoverInstance<TGenotype>
//   where TEncoding : IEncoding<TGenotype>
// {
//   public TEncoding Encoding { get; }
//   
//   protected CrossoverInstance(TEncoding encoding) {
//     Encoding = encoding;
//   }
// }
//
// public abstract class CrossoverInstance<TGenotype, TEncoding, TProblem> : CrossoverInstance<TGenotype, TEncoding>
//   where TEncoding : IEncoding<TGenotype>
//   where TProblem : IProblem<TGenotype, TEncoding>
// {
//   public TProblem Problem { get; }
//   
//   protected CrossoverInstance(TEncoding encoding, TProblem problem) : base(encoding) {
//     Problem = problem;
//   }
// }






public record IndependentCrossover<TGenotype>(double Bias = 0.5) : Crossover<TGenotype> // e.g. Random Crossover
{
  public override ICrossoverInstance<TGenotype> CreateInstance(IEncoding encoding, IProblem problem, IAlgorithm algorithm) {
    return new IndependentCrossoverInstance<TGenotype>(this);
  }
}

public class IndependentCrossoverInstance<TGenotype>(IndependentCrossover<TGenotype> config) : CrossoverInstance<TGenotype>
{
  public override TGenotype Cross(TGenotype parent1, TGenotype parent2) => default!; 
}



public record PermutationGenotypeSpecificCrossover : Crossover<Permutation> // e.g. Order Crossover (does not require encoding info)
{
  public override ICrossoverInstance<Permutation> CreateInstance(IEncoding encoding, IProblem problem, IAlgorithm algorithm) {
    return new PermutationGenotypeSpecificCrossoverInstance();
  }
}

public class PermutationGenotypeSpecificCrossoverInstance : CrossoverInstance<Permutation>
{
  
  public override Permutation Cross(Permutation parent1, Permutation parent2) => default!;
}



public record PermutationEncodingSpecificCrossover : Crossover<Permutation, PermutationEncoding>
{
  public override ICrossoverInstance<Permutation> CreateInstance(PermutationEncoding encoding, IProblem problem, IAlgorithm algorithm) {
    return new PermutationSpecificCrossoverInstance(encoding);
  }
}

public class PermutationSpecificCrossoverInstance(PermutationEncoding encoding) : CrossoverInstance<Permutation>
{
  public override Permutation Cross(Permutation parent1, Permutation parent2) => default!;
}


public record TspSpecificCrossover : Crossover<Permutation, PermutationEncoding, TravelingSalesmanProblem>
{
  public override ICrossoverInstance<Permutation> CreateInstance(PermutationEncoding encoding, TravelingSalesmanProblem problem, IAlgorithm algorithm) {
    return new TspSpecificCrossoverInstance(encoding, problem);
  }
}

public class TspSpecificCrossoverInstance(PermutationEncoding encoding, TravelingSalesmanProblem problem) : CrossoverInstance<Permutation>
{
  
  public override Permutation Cross(Permutation parent1, Permutation parent2) => throw new NotImplementedException();
}





public record RealVectorGenotypeSpecificCrossover : Crossover<RealVector>
{
  public override ICrossoverInstance<RealVector> CreateInstance(IEncoding encoding, IProblem problem, IAlgorithm algorithm) {
    return new RealVectorGenotypeSpecificCrossoverInstance();
  }
}

public class RealVectorGenotypeSpecificCrossoverInstance : CrossoverInstance<RealVector>
{
  public override RealVector Cross(RealVector parent1, RealVector parent2) => default!;
}



public record RealVectorEncodingSpecificCrossover : Crossover<RealVector, RealVectorEncoding>
{
  public override ICrossoverInstance<RealVector> CreateInstance(RealVectorEncoding encoding, IProblem problem, IAlgorithm algorithm) {
    return new RealVectorEncodingSpecificCrossoverInstance(encoding);
  }
}

public class RealVectorEncodingSpecificCrossoverInstance(RealVectorEncoding encoding) : CrossoverInstance<RealVector>
{
  public override RealVector Cross(RealVector parent1, RealVector parent2) => default!;
}



public record TestFunctionProblemSpecificCrossover : Crossover<RealVector, RealVectorEncoding, TestFunctionProblem>
{
  public override ICrossoverInstance<RealVector> CreateInstance(RealVectorEncoding encoding, TestFunctionProblem problem, IAlgorithm algorithm)
    => new TestFunctionProblemSpecificCrossoverInstance(encoding, problem);
}

public class TestFunctionProblemSpecificCrossoverInstance(RealVectorEncoding encoding, TestFunctionProblem problem) : CrossoverInstance<RealVector>
{
  public override RealVector Cross(RealVector parent1, RealVector parent2) => default!;
}



public record IterationResult<TGenotype>
{
  public required ImmutableList<TGenotype> Population { get; init; }
}

public interface IAlgorithm;

public interface IAlgorithm<TGenotype> : IAlgorithm
{
  public IAlgorithmInstance<TGenotype> CreateInstance<TProblem>(TProblem problem) where TProblem : IProblem<TGenotype, IEncoding<TGenotype>>;
}

public interface IAlgorithmInstance<TGenotype>
{
  IterationResult<TGenotype> Run(IterationResult<TGenotype>? initialState = null);
}


public abstract record Algorithm<TGenotype> : IAlgorithm<TGenotype>
{
  public abstract IAlgorithmInstance<TGenotype> CreateInstance<TProblem>(TProblem problem) where TProblem : IProblem<TGenotype, IEncoding<TGenotype>>;
}

public abstract record Algorithm<TGenotype, TEncoding> : IAlgorithm<TGenotype>
  where TEncoding : IEncoding<TGenotype>
{
  IAlgorithmInstance<TGenotype> IAlgorithm<TGenotype>.CreateInstance<TProblem>(TProblem problem)
  {
    if (problem is not IProblem<TGenotype, TEncoding> typedProblem) throw new ArgumentException($"Expected problem of type {typeof(IProblem<TGenotype, TEncoding>).Name}, but got {problem.GetType().Name}.");
    return CreateInstance(typedProblem);
  }
  public abstract IAlgorithmInstance<TGenotype> CreateInstance<TProblem>(TProblem problem) where TProblem : IProblem<TGenotype, TEncoding>;
}

public abstract record Algorithm<TGenotype, TEncoding, TProblem> : IAlgorithm<TGenotype>
  where TEncoding : IEncoding<TGenotype>
  where TProblem : IProblem<TGenotype, TEncoding>
{
  IAlgorithmInstance<TGenotype> IAlgorithm<TGenotype>.CreateInstance<TUntypedPoblem>(TUntypedPoblem problem)
  {
    if (problem is not TProblem typedProblem) throw new ArgumentException($"Expected problem of type {typeof(TProblem).Name}, but got {problem.GetType().Name}.");
    return CreateInstance(typedProblem);
  }
  
  public abstract IAlgorithmInstance<TGenotype> CreateInstance(TProblem problem);
}

//
//
// public interface IAlgorithm<TGenotype> 
// {
//   IterationState<TGenotype> Run<TProblem>(TProblem problem, IterationState<TGenotype>? initialState = null) where TProblem : IProblem<TGenotype, IEncoding<TGenotype>>;
// }
//
// public interface IAlgorithm<TGenotype, in TEncoding> 
//   where TEncoding : IEncoding<TGenotype>
// {
//   IterationState<TGenotype> Run<TProblem>(TProblem problem, IterationState<TGenotype>? initialState = null);
// }
//
// public interface IAlgorithm<TGenotype, in TEncoding, in TProblem> 
//   where TEncoding : IEncoding<TGenotype>
//   where TProblem : IProblem<TGenotype, TEncoding>
// {
//   IterationState<TGenotype> Run(TProblem problem, IterationState<TGenotype>? initialState = null);
// }

public record IndependentAlgorithm<TGenotype> : IAlgorithm<TGenotype>
{
  public ICrossover<TGenotype> Crossover { get; set; }

  public IAlgorithmInstance<TGenotype> CreateInstance<TProblem>(TProblem problem) where TProblem : IProblem<TGenotype, IEncoding<TGenotype>> {
    return new IndependentAlgorithmInstance<TGenotype>(this, problem);
  }
}

public class IndependentAlgorithmInstance<TGenotype>(IndependentAlgorithm<TGenotype> config, IProblem problem) : IAlgorithmInstance<TGenotype>
{
  private ICrossoverInstance<TGenotype> crossover = config.Crossover.CreateInstance(problem.Encoding, problem, config);
  
  public IterationResult<TGenotype> Run(IterationResult<TGenotype>? initialState = null) => default!;
}



public record EncodingIndependentAlgorithm<TGenotype, TEncoding> : Algorithm<TGenotype>
  where TEncoding : IEncoding<TGenotype>
{
  public ICrossover<TGenotype> Crossover { get; set; }

  // public IAlgorithmInstance<TGenotype> CreateInstance<TProblem>(TProblem problem) where TProblem : IProblem<TGenotype, IEncoding<TGenotype>> {
  //   return new EncodingIndependentAlgorithmInstance<TGenotype, TEncoding>(this, problem);
  // }
  public override IAlgorithmInstance<TGenotype> CreateInstance<TProblem>(TProblem problem) {
    return new EncodingIndependentAlgorithmInstance<TGenotype, TEncoding>(this, problem);
  }
}

public class EncodingIndependentAlgorithmInstance<TGenotype, TEncoding>(EncodingIndependentAlgorithm<TGenotype, TEncoding> config, IProblem problem) : IAlgorithmInstance<TGenotype>
  where TEncoding : IEncoding<TGenotype>
{
  private ICrossoverInstance<TGenotype> crossover = config.Crossover.CreateInstance(problem.Encoding, problem, config);
  
  public IterationResult<TGenotype> Run(IterationResult<TGenotype>? initialState = null) => default!;
}


public record PermutationEncodingSpecificAlgorithm : Algorithm<Permutation, PermutationEncoding>
{
  public ICrossover<Permutation> Crossover { get; set; }

  public override IAlgorithmInstance<Permutation> CreateInstance<TProblem>(TProblem problem) {
    return new PermutationEncodingSpecificAlgorithmInstance(this, problem);
  }
}

public class PermutationEncodingSpecificAlgorithmInstance(PermutationEncodingSpecificAlgorithm config, IProblem problem): IAlgorithmInstance<Permutation>
{
  private ICrossoverInstance<Permutation> crossover = config.Crossover.CreateInstance(problem.Encoding, problem, config);
  
  public IterationResult<Permutation> Run(IterationResult<Permutation>? initialState = null) => throw new NotImplementedException();
}



public record TravelingSalesmanProblemSpecificAlgorithm : Algorithm<Permutation, PermutationEncoding, TravelingSalesmanProblem>
{
  public ICrossover<Permutation> Crossover { get; set; }

  public override IAlgorithmInstance<Permutation> CreateInstance(TravelingSalesmanProblem problem) {
    return new TravelingSalesmanProblemSpecificAlgorithmInstance(this, problem);
  }
}

public class TravelingSalesmanProblemSpecificAlgorithmInstance(TravelingSalesmanProblemSpecificAlgorithm config, TravelingSalesmanProblem problem) : IAlgorithmInstance<Permutation>
{
  private ICrossoverInstance<Permutation> crossover = config.Crossover.CreateInstance(problem.Encoding, problem, config);
  
  public IterationResult<Permutation> Run(IterationResult<Permutation>? initialState = null) => throw new NotImplementedException();
}


public record RealVectorEncodingSpecificAlgorithm : Algorithm<RealVector, RealVectorEncoding>
{
  public ICrossover<RealVector> Crossover { get; set; }

  public override IAlgorithmInstance<RealVector> CreateInstance<TProblem>(TProblem problem) {
    return new RealVectorEncodingSpecificAlgorithmInstance(this, problem);
  }
}

public class RealVectorEncodingSpecificAlgorithmInstance(RealVectorEncodingSpecificAlgorithm config, IProblem problem) : IAlgorithmInstance<RealVector>
{
  private ICrossoverInstance<RealVector> crossover = config.Crossover.CreateInstance(problem.Encoding, problem, config);

  public IterationResult<RealVector> Run(IterationResult<RealVector>? initialState = null) => throw new NotImplementedException();
}


public record TestFunctionProblemSpecificAlgorithm : Algorithm<RealVector, RealVectorEncoding, TestFunctionProblem>
{
  public ICrossover<RealVector> Crossover { get; set; }
 
  public override IAlgorithmInstance<RealVector> CreateInstance(TestFunctionProblem problem) {
    return new TestFunctionProblemSpecificAlgorithmInstance(this, problem);
  }
}

public class TestFunctionProblemSpecificAlgorithmInstance(TestFunctionProblemSpecificAlgorithm config, TestFunctionProblem problem) : IAlgorithmInstance<RealVector>
{
  private ICrossoverInstance<RealVector> crossover = config.Crossover.CreateInstance(problem.Encoding, problem, config);

  public IterationResult<RealVector> Run(IterationResult<RealVector>? initialState = null) => throw new NotImplementedException();
}




public class Test
{
  static void TestCompatability() {

    var independentAlgorithmWithPermutation = new EncodingIndependentAlgorithm<Permutation, PermutationEncoding>();
    independentAlgorithmWithPermutation.CreateInstance(new TravelingSalesmanProblem()); //ok
    independentAlgorithmWithPermutation.CreateInstance(new TestFunctionProblem()); // error: problem
    independentAlgorithmWithPermutation.Crossover = new IndependentCrossover<Permutation>(); // ok
    independentAlgorithmWithPermutation.Crossover = new PermutationGenotypeSpecificCrossover(); // ok
    independentAlgorithmWithPermutation.Crossover = new PermutationEncodingSpecificCrossover(); // ok
    independentAlgorithmWithPermutation.Crossover = new TspSpecificCrossover(); // error: problem independent algorithm
    independentAlgorithmWithPermutation.Crossover = new RealVectorGenotypeSpecificCrossover(); // error:  genotype
    independentAlgorithmWithPermutation.Crossover = new RealVectorEncodingSpecificCrossover(); // error: incompatible genotype, incompatible encoding
    independentAlgorithmWithPermutation.Crossover = new TestFunctionProblemSpecificCrossover(); // error: incompatible genotype, incompatible encoding, incompatible problem
    
    var independentAlgorithmWithRealVector = new EncodingIndependentAlgorithm<RealVector, RealVectorEncoding>();
    independentAlgorithmWithRealVector.CreateInstance(new TravelingSalesmanProblem()); // error: incompatible encoding
    independentAlgorithmWithRealVector.CreateInstance(new TestFunctionProblem()); // ok
    independentAlgorithmWithRealVector.Crossover = new IndependentCrossover<RealVector>(); // ok
    independentAlgorithmWithRealVector.Crossover = new PermutationGenotypeSpecificCrossover(); // error: incompatible encoding
    independentAlgorithmWithRealVector.Crossover = new PermutationEncodingSpecificCrossover(); // error: incompatible encoding
    independentAlgorithmWithRealVector.Crossover = new TspSpecificCrossover(); // error: incompatible encoding, problem independent algorithm
    independentAlgorithmWithRealVector.Crossover = new RealVectorGenotypeSpecificCrossover(); // ok
    independentAlgorithmWithRealVector.Crossover = new RealVectorEncodingSpecificCrossover(); // ok
    independentAlgorithmWithRealVector.Crossover = new TestFunctionProblemSpecificCrossover(); // error: problem independent algorithm
    
    var permutationSpecificAlgorithm = new PermutationEncodingSpecificAlgorithm();
    permutationSpecificAlgorithm.CreateInstance(new TravelingSalesmanProblem()); // ok
    permutationSpecificAlgorithm.CreateInstance(new TestFunctionProblem()); // error: incompatible encoding
    permutationSpecificAlgorithm.Crossover = new IndependentCrossover(); // ok
    permutationSpecificAlgorithm.Crossover = new PermutationSpecificCrossover(); // ok
    permutationSpecificAlgorithm.Crossover = new TspSpecificCrossover(); // error: problem independent algorithm
    permutationSpecificAlgorithm.Crossover = new RealVectorSpecificCrossover(); // error: incompatible encoding
    permutationSpecificAlgorithm.Crossover = new TestFunctionProblemSpecificCrossover(); // error: incompatible encoding, problem independent algorithm
    
    var tspSpecificAlgorithm = new TravelingSalesmanProblemSpecificAlgorithm();
    tspSpecificAlgorithm.CreateInstance(new TravelingSalesmanProblem()); // ok
    tspSpecificAlgorithm.CreateInstance(new TestFunctionProblem()); // error: incompatible encoding, incompatible problem
    tspSpecificAlgorithm.Crossover = new IndependentCrossover(); // ok
    tspSpecificAlgorithm.Crossover = new BaseCrossover.Adapter<Permutation, PermutationEncoding, TravelingSalesmanProblem>(new IndependentCrossover()); // ok
    tspSpecificAlgorithm.Crossover = new BaseCrossover<Permutation, PermutationEncoding>.Adapter<TravelingSalesmanProblem>(new IndependentCrossover()); // ok
    tspSpecificAlgorithm.Crossover = new PermutationSpecificCrossover(); // ok
    tspSpecificAlgorithm.Crossover = new TspSpecificCrossover(); // ok
    tspSpecificAlgorithm.Crossover = new RealVectorSpecificCrossover(); // error: incompatible encoding
    tspSpecificAlgorithm.Crossover = new TestFunctionProblemSpecificCrossover(); // error: incompatible encoding, incompatible problem
    
    var realVectorSpecificAlgorithm = new RealVectorEncodingSpecificAlgorithm();
    realVectorSpecificAlgorithm.CreateInstance(new TravelingSalesmanProblem()); // error: incompatible encoding
    realVectorSpecificAlgorithm.CreateInstance(new TestFunctionProblem()); // ok
    realVectorSpecificAlgorithm.Crossover = new IndependentCrossover(); // ok
    realVectorSpecificAlgorithm.Crossover = new PermutationSpecificCrossover(); // error: incompatible encoding
    realVectorSpecificAlgorithm.Crossover = new TspSpecificCrossover(); // error: incompatible encoding, problem independent algorithm
    realVectorSpecificAlgorithm.Crossover = new RealVectorSpecificCrossover(); // ok
    realVectorSpecificAlgorithm.Crossover = new TestFunctionProblemSpecificCrossover(); // error: problem independent algorithm
    
    var testFunctionProblemSpecificAlgorithm = new TestFunctionProblemSpecificAlgorithm();
    testFunctionProblemSpecificAlgorithm.CreateInstance(new TravelingSalesmanProblem()); // error: incompatible encoding, incompatible problem
    testFunctionProblemSpecificAlgorithm.CreateInstance(new TestFunctionProblem()); // ok
    testFunctionProblemSpecificAlgorithm.Crossover = new IndependentCrossover(); // ok
    testFunctionProblemSpecificAlgorithm.Crossover = new PermutationSpecificCrossover(); // error: incompatible encoding
    testFunctionProblemSpecificAlgorithm.Crossover = new TspSpecificCrossover(); // error: incompatible encoding, incompatible problem
    testFunctionProblemSpecificAlgorithm.Crossover = new RealVectorSpecificCrossover(); // ok
    testFunctionProblemSpecificAlgorithm.Crossover = new TestFunctionProblemSpecificCrossover(); // ok
    
  }
}
